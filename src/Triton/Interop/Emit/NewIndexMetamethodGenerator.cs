// Copyright (c) 2020 Kevin Zhao
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Triton.Interop.Emit.Extensions;
using Triton.Interop.Emit.Helpers;
using static System.Reflection.Emit.OpCodes;
using static Triton.Lua;
using static Triton.Lua.LuaType;

namespace Triton.Interop.Emit
{
    /// <summary>
    /// Generates the <c>__newindex</c> metavalue for CLR entities.
    /// </summary>
    internal sealed unsafe class NewIndexMetamethodGenerator : DynamicMetavalueGenerator
    {
        /// <inheritdoc/>
        public override string Name => "__newindex";

        /// <inheritdoc/>
        public override bool IsApplicable(object entity, bool isTypes) =>
            !isTypes || ((IReadOnlyList<Type>)entity).Any(t => !t.IsGenericTypeDefinition);

        /// <inheritdoc/>
        protected override void GenerateImpl(lua_State* state, ILGenerator ilg, object obj) =>
            GenerateImpl(state, ilg, obj.GetType(), isStatic: false);

        /// <inheritdoc/>
        protected override void GenerateImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types) =>
            GenerateImpl(state, ilg, types.First(t => !t.IsGenericTypeDefinition), isStatic: true);

        private static void GenerateImpl(lua_State* state, ILGenerator ilg, Type type, bool isStatic)
        {
            var isStruct = type.IsClrStruct();
            var target = isStatic ? null : EmitDeclareTarget(ilg, type);
            var keyType = EmitDeclareKeyType(ilg);

            EmitAccess(ilg);

            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldstr, $"attempt to index CLR {(isStatic ? "types" : "object")} with invalid key");
            ilg.Emit(Call, _luaL_error);
            ilg.Emit(Ret);
            return;

            void EmitAccess(ILGenerator ilg)
            {
                EmitMemberAccess(ilg);

                if (!isStatic)
                {
                    if (type.IsSZArray)
                    {
                        EmitSzArrayAccess(ilg);
                    }
                    else if (type.IsVariableBoundArray)
                    {
                        EmitNdArrayAccess(ilg);
                    }
                }
            }

            void EmitMemberAccess(ILGenerator ilg)
            {
                var isNotString = ilg.DefineLabel();

                ilg.Emit(Ldloc, keyType);
                ilg.Emit(Ldc_I4, (int)LUA_TSTRING);
                ilg.Emit(Bne_Un, isNotString);  // Not short form
                {
                    var members = Array.Empty<MemberInfo>()
                        .Concat(type.GetPublicFields(isStatic))
                        .Concat(type.GetPublicEvents(isStatic))
                        .Concat(type.GetPublicProperties(isStatic))
                        .Concat(type.GetPublicMethods(isStatic).GroupBy(m => m.Name).Select(g => g.First()))
                        .Concat(isStatic ? type.GetPublicNestedTypes() : Type.EmptyTypes)
                        .ToList();

                    var ptrs = InternMemberNames(state, members);
                    var labels = ilg.DefineLabels(members.Count);

                    var keyPtr = EmitDeclareKeyPtr(ilg);
                    EmitSwitchKeyPtr(ilg, keyPtr, ptrs.Zip(labels));

                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldstr, $"attempt to set invalid member `{type.Name}.{{0}}`");
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldc_I4_2);  // Key
                    ilg.Emit(Call, _lua_tostring);
                    ilg.Emit(Call, _stringFormat);
                    ilg.Emit(Call, _luaL_error);
                    ilg.Emit(Ret);

                    for (var i = 0; i < members.Count; ++i)
                    {
                        ilg.MarkLabel(labels[i]);

                        var member = members[i];
                        if (member is FieldInfo field)
                        {
                            EmitFieldAccess(ilg, field);
                        }
                        else if (member is PropertyInfo property)
                        {
                            EmitPropertyAccess(ilg, property);
                        }
                        else
                        {
                            ilg.Emit(Ldarg_0);  // Lua state
                            ilg.Emit(Ldstr, @$"attempt to set {member.MemberType switch
                            {
                                MemberTypes.Event      => "event",
                                MemberTypes.Method     => "method",
                                MemberTypes.NestedType => "nested type",
                                _                      => throw new InvalidOperationException()
                            }} `{type.Name}.{member.Name}`");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Ret);
                        }
                    }
                }

                ilg.MarkLabel(isNotString);
                return;

                void EmitFieldAccess(ILGenerator ilg, FieldInfo field)
                {
                    var fieldType = field.FieldType;

                    if (field.IsLiteral || field.IsInitOnly)
                    {
                        ilg.Emit(Ldarg_0);  // Lua state
                        ilg.Emit(Ldstr, $"attempt to set non-assignable field `{type.Name}.{field.Name}`");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);
                        return;
                    }

                    var isValid = ilg.DefineLabel();

                    // As a small optimization, pass the field by-ref.

                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldc_I4_3);  // Value
                    if (!isStatic)
                    {
                        ilg.Emit(isStruct ? Ldloca : Ldloc, target!);
                    }
                    ilg.Emit(isStatic ? Ldsflda : Ldflda, field);
                    ilg.Emit(Call, LuaTryLoadHelpers.Get(fieldType));
                    ilg.Emit(Brtrue_S, isValid);
                    {
                        ilg.Emit(Ldarg_0);  // Lua state
                        ilg.Emit(Ldstr, $"attempt to set field `{type.Name}.{field.Name}` with invalid value");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);
                    }

                    ilg.MarkLabel(isValid);
                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                }

                void EmitPropertyAccess(ILGenerator ilg, PropertyInfo property)
                {
                    var propertyType = property.PropertyType;

                    if (propertyType.IsByRef)
                    {
                        // The by-ref property is guaranteed to have a public getter because we filtered for public
                        // properties and by-ref properties can only have getters.

                        Debug.Assert(property.GetMethod is { IsPublic: true });

                        var isValid = ilg.DefineLabel();

                        // As a small optimization, pass the property by-ref.

                        ilg.Emit(Ldarg_0);  // Lua state
                        ilg.Emit(Ldc_I4_3);  // Value
                        if (!isStatic)
                        {
                            ilg.Emit(isStruct ? Ldloca : Ldloc, target!);
                        }
                        ilg.Emit(isStatic ? Call : Callvirt, property.GetMethod);
                        ilg.Emit(Call, LuaTryLoadHelpers.Get(propertyType.GetElementType()!));
                        ilg.Emit(Brtrue_S, isValid);
                        {
                            ilg.Emit(Ldarg_0);  // Lua state
                            ilg.Emit(Ldstr, $"attempt to set property `{type.Name}.{property.Name}` with invalid value");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Ret);
                        }

                        ilg.MarkLabel(isValid);
                    }
                    else
                    {
                        if (property.SetMethod is not { IsPublic: true })
                        {
                            ilg.Emit(Ldarg_0);  // Lua state
                            ilg.Emit(Ldstr, $"attempt to set non-assignable property `{type.Name}.{property.Name}`");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Ret);
                            return;
                        }

                        var isValid = ilg.DefineLabel();

                        // Use a temporary to simulate passing the property by-ref.

                        using var temp = ilg.DeclareReusableLocal(propertyType);
                        ilg.Emit(Ldarg_0);  // Lua state
                        ilg.Emit(Ldc_I4_3);  // Value
                        ilg.Emit(Ldloca, temp);
                        ilg.Emit(Call, LuaTryLoadHelpers.Get(propertyType));
                        ilg.Emit(Brtrue_S, isValid);
                        {
                            ilg.Emit(Ldarg_0);  // Lua state
                            ilg.Emit(Ldstr, $"attempt to set property `{type.Name}.{property.Name}` with invalid value");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Ret);
                        }

                        ilg.MarkLabel(isValid);

                        if (!isStatic)
                        {
                            ilg.Emit(isStruct ? Ldloca : Ldloc, target!);
                        }
                        ilg.Emit(Ldloc, temp);
                        ilg.Emit(isStatic ? Call : Callvirt, property.SetMethod);
                    }

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                }
            }
        
            void EmitSzArrayAccess(ILGenerator ilg)
            {
                var elementType = type.GetElementType()!;

                var isValidKey = ilg.DefineLabel();

                var index = ilg.DeclareLocal(typeof(int));
                ilg.Emit(Ldarg_0);  // Lua state
                ilg.Emit(Ldc_I4_2);  // Key
                ilg.Emit(Ldloca, index);
                ilg.Emit(Call, LuaTryLoadHelpers.Get(typeof(int)));
                ilg.Emit(Brtrue_S, isValidKey);
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldstr, "attempt to set array with invalid index");
                    ilg.Emit(Call, _luaL_error);
                    ilg.Emit(Ret);
                }

                ilg.MarkLabel(isValidKey);

                var isValidValue = ilg.DefineLabel();

                // As a small optimization, pass the array element by-ref.

                ilg.Emit(Ldarg_0);  // Lua state
                ilg.Emit(Ldc_I4_3);  // Value
                ilg.Emit(Ldloc, target!);
                ilg.Emit(Ldloc, index);
                ilg.Emit(Ldelema, elementType);
                ilg.Emit(Call, LuaTryLoadHelpers.Get(elementType));
                ilg.Emit(Brtrue_S, isValidValue);
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldstr, "attempt to set array with invalid value");
                    ilg.Emit(Call, _luaL_error);
                    ilg.Emit(Ret);
                }

                ilg.MarkLabel(isValidValue);

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            }

            void EmitNdArrayAccess(ILGenerator ilg)
            {
                var elementType = type.GetElementType()!;
                var rank = type.GetArrayRank();

                var indices = ilg.DeclareLocal(typeof(Span<int>));
                ilg.Emit(Ldc_I4, 4 * rank);
                ilg.Emit(Conv_U);
                ilg.Emit(Localloc);
                ilg.Emit(Ldc_I4, rank);
                ilg.Emit(Newobj, _spanIntCtor);
                ilg.Emit(Stloc, indices);

                var isValidKey = ilg.DefineLabel();

                ilg.Emit(Ldarg_0);  // Lua state
                ilg.Emit(Ldc_I4_2);  // Key
                ilg.Emit(Ldloc, indices);
                ilg.Emit(Call, ArrayHelpers._getNdArrayIndices);
                ilg.Emit(Brtrue_S, isValidKey);
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldstr, "attempt to set multi-dimensional array with invalid indices");
                    ilg.Emit(Call, _luaL_error);
                    ilg.Emit(Ret);
                }

                ilg.MarkLabel(isValidKey);

                var isValidValue = ilg.DefineLabel();

                // As a small optimization, pass the array element by-ref.

                ilg.Emit(Ldarg_0);  // Lua state
                ilg.Emit(Ldc_I4_3);  // Value
                ilg.Emit(Ldloc, target!);
                for (var i = 0; i < rank; ++i)
                {
                    ilg.Emit(Ldloca, indices);
                    ilg.Emit(Ldc_I4, i);
                    ilg.Emit(Call, _spanIntItemGet);
                    ilg.Emit(Ldind_I4);
                }
                ilg.Emit(Callvirt, type.GetMethod("Address")!);
                ilg.Emit(Call, LuaTryLoadHelpers.Get(elementType));
                ilg.Emit(Brtrue_S, isValidValue);
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Ldstr, "attempt to set multi-dimensional array with invalid value");
                    ilg.Emit(Call, _luaL_error);
                    ilg.Emit(Ret);
                }

                ilg.MarkLabel(isValidValue);

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            }
        }
    }
}
