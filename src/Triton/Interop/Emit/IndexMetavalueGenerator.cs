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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Triton.Interop.Emit.Extensions;
using static System.Reflection.Emit.OpCodes;
using static Triton.Lua;

namespace Triton.Interop.Emit
{
    /// <summary>
    /// Generates the <c>__index</c> metavalue for CLR entities.
    /// </summary>
    internal sealed unsafe class IndexMetavalueGenerator : DynamicMetavalueGenerator
    {
        public override string Name => "__index";

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
        }

        protected override void GenerateImpl(lua_State* state, ILGenerator ilg, object obj) =>
            GenerateImpl(state, ilg, new[] { obj.GetType() }, isStatic: false);

        protected override void GenerateImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types) =>
            GenerateImpl(state, ilg, types, isStatic: true);

        private static void GenerateImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types, bool isStatic)
        {
            var type = types.SingleOrDefault(t => !t.IsGenericTypeDefinition);
            var genericTypes = types.Where(t => t.IsGenericTypeDefinition).ToList();

            var target = isStatic ? null : EmitDeclareTarget(ilg, type!);
            var keyType = EmitDeclareKeyType(ilg);

            if (type is not null)
            {
                EmitTypeAccess();
            }

            if (genericTypes.Count > 0)
            {
                EmitGenericTypesAccess();
            }

            ilg.Emit(Ldc_I4_0);
            ilg.Emit(Ret);
            return;

            void EmitTypeAccess()
            {
                var members = Enumerable.Empty<MemberInfo>()
                    .Concat(type.GetPublicFields(isStatic).Where(f => !f.IsLiteral))
                    .Concat(type.GetPublicProperties(isStatic))
                    .ToList();
                EmitMemberAccess(ilg, keyType, state, members,
                    (ilg, member) =>
                    {
                        if (member is PropertyInfo { GetMethod: null or { IsPublic: false } })
                        {
                            EmitLuaError(ilg, "attempt to get non-readable property");
                            ilg.Emit(Ret);
                            return;
                        }

                        var type = member.GetUnderlyingType();
                        if (type.IsByRefLike)
                        {
                            EmitLuaError(ilg, "attempt to get byref-like member");
                            ilg.Emit(Ret);
                            return;
                        }

                        var isByRefType = type.IsByRef;
                        var nonByRefType = isByRefType ? type.GetElementType()! : type;
                        using var value = ilg.DeclareReusableLocal(nonByRefType);

                        if (!isStatic)
                        {
                            ilg.Emit(Ldloc, target!);
                        }
                        
                        if (member is FieldInfo field)
                        {
                            ilg.Emit(isStatic ? Ldsfld : Ldfld, field);
                        }
                        else if (member is PropertyInfo property)
                        {
                            ilg.Emit(isStatic ? Call : Callvirt, property.GetMethod!);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        if (isByRefType)
                        {
                            ilg.EmitLdind(nonByRefType);
                        }
                        ilg.Emit(Stloc, value);
                        EmitLuaPush(ilg, value);

                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Ret);
                    },
                    ilg =>
                    {
                        ilg.Emit(Ldc_I4_0);
                        ilg.Emit(Ret);
                    });

                if (isStatic)
                {
                    return;
                }
            }
        
            void EmitGenericTypesAccess()
            {
                // TODO: implement this
            }
        }
    }
}
