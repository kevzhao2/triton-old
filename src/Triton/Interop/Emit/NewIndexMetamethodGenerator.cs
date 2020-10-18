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
using System.Text;
using System.Threading.Tasks;
using Triton.Interop.Emit.Extensions;
using Triton.Interop.Emit.Helpers;
using static System.Reflection.Emit.OpCodes;
using static Triton.Lua;

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
            var target = isStatic ? null : EmitDeclareTarget(ilg, type);
            var keyType = EmitDeclareKeyType(ilg);

            EmitTypeAccess();

            ilg.Emit(Ldc_I4_0);
            ilg.Emit(Ret);
            return;

            void EmitTypeAccess()
            {
                var members = Enumerable.Empty<MemberInfo>()
                    .Concat(type.GetPublicFields(isStatic))
                    //.Concat(type.GetPublicEvents(isStatic))
                    //.Concat(type.GetPublicProperties(isStatic))
                    //.Concat(type.GetPublicMethods(isStatic).GroupBy(m => m.Name).Select(g => g.First()))
                    //.Concat(isStatic ? type.GetPublicNestedTypes() : Array.Empty<Type>())
                    .ToList();
                EmitMemberAccess(ilg, keyType, state, members,
                    (ilg, member) =>
                    {
                        var type = member.GetUnderlyingType();
                        using var value = ilg.DeclareReusableLocal(type);

                        var isValid = ilg.DefineLabel();

                        ilg.Emit(Ldarg_0);  // `state`
                        ilg.Emit(Ldc_I4_3);  // Value
                        ilg.Emit(Ldloca, value);
                        ilg.Emit(Call, LuaTryLoadHelpers.Get(type));
                        ilg.Emit(Brtrue_S, isValid);
                        {

                        }

                        ilg.MarkLabel(isValid);

                        if (!isStatic)
                        {
                            ilg.Emit(Ldloc, target!);
                        }

                        ilg.Emit(Ldloc, value);
                        if (member is FieldInfo field)
                        {
                            ilg.Emit(isStatic ? Stsfld : Stfld, field);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        ilg.Emit(Ldc_I4_0);
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
        }
    }
}
