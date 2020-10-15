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
    /// Generates the <c>__index</c> metamethod for CLR entities.
    /// </summary>
    internal sealed unsafe class IndexMetamethodGenerator : DynamicMetamethodGenerator
    {
        private static readonly MethodInfo _lua_tolstring = typeof(Lua).GetMethod(nameof(lua_tolstring))!;

        public override string Name => "__index";

        public override unsafe void PushMetamethod(lua_State* state, object entity, bool isTypes)
        {
            base.PushMetamethod(state, entity, isTypes);
        }

        protected override void GenerateMetamethodImpl(lua_State* state, ILGenerator ilg, object obj) =>
            GenerateMetamethodImpl(state, ilg, new[] { obj.GetType() }, isStatic: false);

        protected override void GenerateMetamethodImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types) =>
            GenerateMetamethodImpl(state, ilg, types, isStatic: true);

        private void GenerateMetamethodImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types, bool isStatic)
        {
            var type = types.SingleOrDefault(t => !t.IsGenericTypeDefinition);
            var genericTypes = types.Where(t => t.IsGenericTypeDefinition).ToList();

            if (type is null)
            {
                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
                return;
            }


            var target = isStatic ? null : EmitDeclareTarget(ilg, type);
            var keyType = EmitDeclareKeyType(ilg);

            if (type is not null)
            {
                var members = Enumerable.Empty<MemberInfo>()
                    .Concat(type.GetPublicFields(isStatic).Where(f => !f.IsLiteral))
                    .Concat(type.GetPublicProperties(isStatic))
                    .ToList();
            }

            var ptr = ilg.DeclareLocal(typeof(nint));
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldc_I4_0);
            ilg.Emit(Conv_I);
            ilg.Emit(Call, _lua_tolstring);
            ilg.Emit(Stloc, ptr);



            var ptrs = InternMemberNames(state, members);

            for (var i = 0; i < members.Count; ++i)
            {
                var skip = ilg.DefineLabel();

                ilg.Emit(Ldloc, ptr);
                ilg.Emit(Ldc_I8, ptrs[i]);
                ilg.Emit(Conv_I);
                ilg.Emit(Bne_Un_S, skip);

                var member = members[i];
                if (member is FieldInfo field)
                {
                    var value = ilg.DeclareLocal(field.FieldType);
                    ilg.Emit(Ldsfld, field);
                    ilg.Emit(Stloc, value);

                    EmitLuaPush(ilg, value);
                }
                else if (member is PropertyInfo property)
                {
                    var value = ilg.DeclareLocal(property.PropertyType);
                    ilg.Emit(Call, property.GetMethod!);
                    ilg.Emit(Stloc, value);

                    EmitLuaPush(ilg, value);
                }

                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Ret);

                ilg.MarkLabel(skip);
            }

            ilg.Emit(Ldc_I4_0);
            ilg.Emit(Ret);
        }
    }
}
