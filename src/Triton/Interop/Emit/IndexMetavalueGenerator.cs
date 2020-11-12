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
    /// Generates the <c>__index</c> metavalue for CLR entities.
    /// </summary>
    internal sealed class IndexMetavalueGenerator : DynamicMetavalueGenerator
    {
        /// <inheritdoc/>
        public override string Name => "__index";

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
                    .Concat(type.GetPublicProperties(isStatic))
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
                            throw new NotImplementedException();
                        }
                    });

                return;

                void EmitFieldAccess(ILGenerator ilg, FieldInfo field)
                {
                    EmitHelpers.LuaPush(
                        ilg, field.FieldType,
                        ilg =>
                        {
                            EmitHelpers.MaybeLoadTarget(ilg, target);
                            ilg.Emit(target is null ? Ldsfld : Ldfld, field);
                        });

                    ilg.Emit(Ldc_I4_1);
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

                        var elementType = propertyType.GetElementType()!;

                        EmitHelpers.LuaPush(
                            ilg, elementType,
                            ilg =>
                            {
                                EmitHelpers.MaybeLoadTarget(ilg, target);
                                ilg.Emit(target is null ? Call : Callvirt, property.GetMethod);
                                ilg.EmitLdind(elementType);
                            });
                    }
                    else
                    {
                        if (property.GetMethod is not { IsPublic: true })
                        {
                            EmitHelpers.LuaError(
                                ilg, $"attempt to get non-readable property '{type.Name}.{property.Name}'");
                            ilg.Emit(Ret);
                            return;
                        }

                        EmitHelpers.LuaPush(
                            ilg, propertyType,
                            ilg =>
                            {
                                EmitHelpers.MaybeLoadTarget(ilg, target);
                                ilg.Emit(target is null ? Call : Callvirt, property.GetMethod);
                            });
                    }

                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Ret);
                }
            }
        
            void EmitSzArrayAccess(ILGenerator ilg)
            {
                var elementType = type.GetElementType()!;

                var index = EmitHelpers.DeclareSzArrayIndex(ilg);

                EmitHelpers.LuaPush(
                    ilg, elementType,
                    ilg =>
                    {
                        ilg.Emit(Ldloc, target!);
                        ilg.Emit(Ldloc, index);
                        ilg.EmitLdelem(elementType);
                    });

                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Ret);
            }

            void EmitNdArrayAccess(ILGenerator ilg)
            {
                var rank = type.GetArrayRank();

                var indices = EmitHelpers.DeclareNdArrayIndices(ilg, rank);

                EmitHelpers.LuaPush(
                    ilg, type.GetElementType()!,
                    ilg =>
                    {
                        ilg.Emit(Ldloc, target!);
                        EmitHelpers.LoadNdArrayIndices(ilg, rank, indices);
                        ilg.Emit(Callvirt, type.GetMethod("Get")!);
                    });

                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Ret);
            }
        }

        /*/// <inheritdoc/>
        public override void Push(lua_State* state, object entity, bool isTypes)
        {
            var type = isTypes ?
                ((IReadOnlyList<Type>)entity).SingleOrDefault(t => !t.IsGenericTypeDefinition) :
                entity.GetType();

            // The metavalue is a table consisting of cacheable members with a metamethod with an `__index` metavalue to
            // handle non-cacheable members. This greatly improves cacheable members while only providing a minor hit to
            // non-cacheable members.

            lua_newtable(state);
            if (type is not null)
            {
                PopulateCacheableMembers();
            }

            SetupMetatable();
            return;

            void PopulateCacheableMembers()
            {
                // TODO: const fields
                // TODO: events
                // TODO: methods
                // TODO: nested types
            }

            void SetupMetatable()
            {
                lua_createtable(state, 0, 1);
                base.Push(state, entity, isTypes);
                lua_setfield(state, -2, Name);
                lua_setmetatable(state, -2);
            }
        }*/

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
            if (types.SingleOrDefault(t => !t.IsGenericTypeDefinition) is { } type)
            {
                EmitTypeAccess(state, ilg, type, isStatic: true);
            }

            EmitHelpers.LuaError(
                ilg, "attempt to index CLR types with an invalid key");
            ilg.Emit(Ret);
        }
    }
}
