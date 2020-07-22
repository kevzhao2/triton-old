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
using System.Reflection;
using System.Reflection.Emit;
using Triton.Interop.Utils;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop.CodeGeneration
{
    /// <summary>
    /// Generates the <c>__index</c> metamethod for a CLR type.
    /// </summary>
    internal static class ClrTypeIndexGenerator
    {
        public static lua_CFunction Generate(Type type)
        {
            var members = new Dictionary<string, MemberInfo>();
            var memberNames = new List<string>();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                members[field.Name] = field;
                memberNames.Add(field.Name);
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var property in properties)
            {
                members[property.Name] = property;
                memberNames.Add(property.Name);
            }

            var matcher = new StringMatcher(memberNames);
            var method = new DynamicMethod("__index", typeof(int), new[] { typeof(StringMatcher), typeof(IntPtr) });

            var ilg = method.GetILGenerator();

            var labels = new Label[memberNames.Count];
            for (var i = 0; i < memberNames.Count; ++i)
            {
                labels[i] = ilg.DefineLabel();
            }

            ilg.Emit(Ldarg_0);
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_2);
            ilg.Emit(Call, typeof(NativeMethods).GetMethod("lua_tostring"));
            ilg.Emit(Call, typeof(StringMatcher).GetMethod("Match"));
            ilg.Emit(Switch, labels);

            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldstr, "attempt to index invalid member");
            ilg.Emit(Call, typeof(NativeMethods).GetMethod("luaL_error"));
            ilg.Emit(Ret);

            for (var i = 0; i < memberNames.Count; ++i)
            {
                ilg.MarkLabel(labels[i]);

                var member = members[memberNames[i]];
                if (member is FieldInfo field)
                {
                    ilg.Emit(Ldarg_1);
                    ilg.Emit(Ldsfld, field);
                    ilg.EmitLuaPush(field.FieldType);
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Ret);
                }
                else if (member is PropertyInfo property)
                {
                    ilg.Emit(Ldarg_1);
                    ilg.Emit(Call, property.GetMethod);
                    ilg.EmitLuaPush(property.PropertyType);
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Ret);
                }
            }

            return (lua_CFunction)method.CreateDelegate(typeof(lua_CFunction), matcher);
        }
    }
}
