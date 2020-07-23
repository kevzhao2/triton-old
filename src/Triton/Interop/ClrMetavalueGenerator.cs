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
using System.Runtime.InteropServices;
using Triton.Interop.Utils;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;
using Debug = System.Diagnostics.Debug;

namespace Triton.Interop
{
    /// <summary>
    /// Generates Lua metavalues for CLR types and objects.
    /// </summary>
    internal sealed class ClrMetavalueGenerator
    {
        private static readonly lua_CFunction _gcMetamethod = GcMetamethod;
        private static readonly lua_CFunction _tostringMetamethod = ProtectedCall(ToStringMetamethod);

        private readonly LuaEnvironment _environment;

        private readonly Dictionary<Type, lua_CFunction> _typeIndex = new Dictionary<Type, lua_CFunction>();

        public ClrMetavalueGenerator(LuaEnvironment environment)
        {
            _environment = environment;
        }

        // Creates a wrapper `lua_CFunction` that performs a "protected" call of a `lua_CFunction`, raising uncaught CLR
        // exceptions as Lua errors. This is required since exceptions should _NOT_ be thrown in reverse P/Invokes.
        //
        private static lua_CFunction ProtectedCall(lua_CFunction callback) =>
            state =>
            {
                try
                {
                    return callback(state);
                }
                catch (Exception ex)
                {
                    return luaL_error(state, $"unhandled CLR exception:\n{ex}");
                }
            };

        private static int GcMetamethod(IntPtr state)
        {
            Debug.Assert(lua_type(state, 1) == LuaType.Userdata);

            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            handle.Free();
            return 0;
        }

        private static int ToStringMetamethod(IntPtr state)
        {
            Debug.Assert(lua_type(state, 1) == LuaType.Userdata);

            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            lua_pushstring(state, handle.Target.ToString());
            return 1;
        }

        /// <summary>
        /// Gets the <c>__gc</c> metamethod for CLR types and objects.
        /// </summary>
        /// <value>The <c>__gc</c> metamethod for CLR types and objects.</value>
        public lua_CFunction Gc => _gcMetamethod;

        /// <summary>
        /// Gets the <c>__tostring</c> metamethod for CLR types and objects.
        /// </summary>
        /// <value>The <c>__tostring</c> metamethod for CLR types and objects.</value>
        public new lua_CFunction ToString => _tostringMetamethod;

        /// <summary>
        /// Pushes the <c>__index</c> metavalue for the given CLR <paramref name="type"/> onto the stack of the Lua
        /// <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        public void PushTypeIndex(IntPtr state, Type type)
        {
            // The metavalue is a table with entries for `const` fields, events, methods, and nested types. This table
            // then has an `__index` metamethod which resolves non-`const` fields and properties.
            //
            // Essentially, we are caching all cacheable members, which greatly improves performance as there are fewer
            // unmanaged <-> managed transitions.

            lua_newtable(state);

            foreach (var constField in type.GetAllConstFields())
            {
            }

            foreach (var nestedType in type.GetAllNestedTypes())
            {
                _environment.PushClrType(state, nestedType);
                lua_setfield(state, -2, nestedType.Name);
            }
        }

        /// <summary>
        /// Generates the <c>__index</c> metamethod for the given CLR <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The <c>__index</c> metamethod.</returns>
        public lua_CFunction TypeIndex(Type type)
        {
            if (_typeIndex.TryGetValue(type, out var result))
            {
                return result;
            }

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
