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

namespace Triton.Interop.Emit
{
    /// <summary>
    /// Generates the <c>__newindex</c> metavalue for CLR entities.
    /// </summary>
    internal sealed class NewIndexMetamethodGenerator : DynamicMetavalueGenerator
    {
        /// <inheritdoc/>
        public override string Name => "__newindex";

        private static unsafe void EmitTypeAccess(lua_State* state, ILGenerator ilg, Type type, bool isStatic)
        {
            var target = isStatic ? null : EmitHelpers.DeclareTarget(ilg, type);

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

            return;

            void EmitMemberAccess(ILGenerator ilg)
            {
                var members = Array.Empty<MemberInfo>()
                    .Concat(type.GetPublicFields(isStatic))
                    .Concat(type.GetPublicEvents(isStatic))
                    .Concat(type.GetPublicProperties(isStatic))
                    .Concat(type.GetPublicMethods(isStatic).GroupBy(m => m.Name).Select(g => g.First()))
                    .Concat(isStatic ? type.GetPublicNestedTypes() : Type.EmptyTypes)
                    .ToList();

                EmitHelpers.MemberAccesses(state, ilg, type, members,
                    (ilg, member) =>
                    {
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
                            EmitHelpers.LuaError(
                                ilg, $@"attempt to set {member.MemberType switch
                                {
                                    MemberTypes.Event      => "event",
                                    MemberTypes.Method     => "method",
                                    MemberTypes.NestedType => "nested type",
                                    _                      => throw new InvalidOperationException()
                                }} '{type.Name}.{member.Name}'");
                            ilg.Emit(Ret);
                        }
                    });

                return;

                void EmitFieldAccess(ILGenerator ilg, FieldInfo field)
                {
                    if (field.IsLiteral || field.IsInitOnly)
                    {
                        EmitHelpers.LuaError(
                            ilg, $"attempt to set non-assignable field '{type.Name}.{field.Name}'");
                        ilg.Emit(Ret);
                        return;
                    }

                    EmitHelpers.LuaTryLoad(
                        ilg, field.FieldType,
                        ilg => ilg.Emit(Ldc_I4_3),  // value
                        ilg =>
                        {
                            EmitHelpers.MaybeLoadTarget(ilg, target);
                            ilg.Emit(target is null ? Ldsflda : Ldflda, field);
                        },
                        ilg =>
                        {
                            EmitHelpers.LuaError(
                                ilg, $"attempt to set field '{type.Name}.{field.Name}' with an invalid value");
                            ilg.Emit(Ret);
                        });

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                }

                void EmitPropertyAccess(ILGenerator ilg, PropertyInfo property)
                {
                    var propertyType = property.PropertyType;

                    if (propertyType.IsByRef)
                    {
                        // The by-ref property is guaranteed to have a public getter because we filtered for public
                        // properties, and by-ref properties can only have getters.

                        Debug.Assert(property.GetMethod is { IsPublic: true });

                        EmitHelpers.LuaTryLoad(
                            ilg, propertyType.GetElementType()!,
                            ilg => ilg.Emit(Ldc_I4_3),  // value
                            ilg =>
                            {
                                EmitHelpers.MaybeLoadTarget(ilg, target);
                                ilg.Emit(target is null ? Call : Callvirt, property.GetMethod);
                            },
                            ilg =>
                            {
                                EmitHelpers.LuaError(
                                    ilg, $"attempt to set property '{type.Name}.{property.Name}' with an invalid value");
                                ilg.Emit(Ret);
                            });
                    }
                    else
                    {
                        if (property.SetMethod is not { IsPublic: true })
                        {
                            EmitHelpers.LuaError(
                                ilg, $"attempt to set non-assignable property '{type.Name}.{property.Name}'");
                            ilg.Emit(Ret);
                            return;
                        }

                        using var temp = ilg.DeclareReusableLocal(propertyType);
                        EmitHelpers.LuaTryLoad(
                            ilg, propertyType,
                            ilg => ilg.Emit(Ldc_I4_3),  // value
                            ilg => ilg.Emit(Ldloca, temp),
                            ilg =>
                            {
                                EmitHelpers.LuaError(
                                    ilg, $"attempt to set property '{type.Name}.{property.Name}' with an invalid value");
                                ilg.Emit(Ret);
                            });

                        EmitHelpers.MaybeLoadTarget(ilg, target);
                        ilg.Emit(Ldloc, temp);
                        ilg.Emit(target is null ? Call : Callvirt, property.SetMethod);
                    }

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                }
            }

            void EmitSzArrayAccess(ILGenerator ilg)
            {
                var elementType = type.GetElementType()!;

                var index = EmitHelpers.DeclareSzArrayIndex(ilg);

                EmitHelpers.LuaTryLoad(
                    ilg, elementType,
                    ilg => ilg.Emit(Ldc_I4_3),  // value
                    ilg =>
                    {
                        ilg.Emit(Ldloc, target!);
                        ilg.Emit(Ldloc, index);
                        ilg.Emit(Ldelema, elementType);
                    },
                    ilg =>
                    {
                        EmitHelpers.LuaError(
                            ilg, "attempt to set an array with an invalid value");
                        ilg.Emit(Ret);
                    });

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            }

            void EmitNdArrayAccess(ILGenerator ilg)
            {
                var rank = type.GetArrayRank();

                var indices = EmitHelpers.DeclareNdArrayIndices(ilg, rank);

                EmitHelpers.LuaTryLoad(
                    ilg, type.GetElementType()!,
                    ilg => ilg.Emit(Ldc_I4_3),  // value
                    ilg =>
                    {
                        ilg.Emit(Ldloc, target!);
                        EmitHelpers.LoadNdArrayIndices(ilg, rank, indices);
                        ilg.Emit(Callvirt, type.GetMethod("Address")!);
                    },
                    ilg =>
                    {
                        EmitHelpers.LuaError(
                            ilg, "attempt to set a multi-dimensional array with an invalid value");
                        ilg.Emit(Ret);
                    });

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            }
        }

        /// <inheritdoc/>
        public override bool IsApplicable(object entity, bool isTypes) =>
            !isTypes || ((IReadOnlyList<Type>)entity).Any(t => !t.IsGenericTypeDefinition);

        /// <inheritdoc/>
        protected override unsafe void EmitMetamethodImpl(lua_State* state, ILGenerator ilg, object obj)
        {
            EmitTypeAccess(state, ilg, obj.GetType(), isStatic: false);

            EmitHelpers.LuaError(
                ilg, "attempt to index a CLR object with an invalid key");
            ilg.Emit(Ret);
        }

        /// <inheritdoc/>
        protected override unsafe void EmitMetamethodImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types)
        {
            EmitTypeAccess(state, ilg, types.Single(t => !t.IsGenericTypeDefinition), isStatic: true);

            EmitHelpers.LuaError(
                ilg, "attempt to index CLR types with an invalid key");
            ilg.Emit(Ret);
        }
    }
}
