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
using System.Runtime.InteropServices;
using Triton.Interop.CodeGeneration;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Manages Lua metatables for CLR types and objects.
    /// </summary>
    internal class ClrMetatableManager
    {
        private static readonly lua_CFunction _gcCallback = GcCallback;
        private static readonly lua_CFunction _toStringCallback = ProtectedCall(ToStringCallback);

        // To store the metatables for the CLR types and objects, we will use two Lua tables with integer keys and
        // metatable values.
        private readonly int _typeTableReference;
        private readonly int _objectTableReference;
        private readonly Dictionary<Type, int> _typeTableKeys = new Dictionary<Type, int>();
        private readonly Dictionary<Type, int> _objectTableKeys = new Dictionary<Type, int>();

        internal ClrMetatableManager(IntPtr state, LuaEnvironment environment)
        {
            Debug.Assert(state != IntPtr.Zero);
            Debug.Assert(environment != null);

            // Create the CLR type and object metatable tables.
            lua_newtable(state);
            _typeTableReference = luaL_ref(state, LUA_REGISTRYINDEX);
            lua_newtable(state);
            _objectTableReference = luaL_ref(state, LUA_REGISTRYINDEX);
        }

        // Performs a "protected" call of a `lua_CFunction`, raising uncaught CLR exceptions as Lua errors. This is
        // required since exceptions should _NOT_ be thrown in reverse P/Invokes.
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

        private static int GcCallback(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            handle.Free();
            return 0;
        }

        private static int ToStringCallback(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            lua_pushstring(state, handle.Target.ToString());
            return 1;
        }

        /// <summary>
        /// Pushes the metatable for the specified CLR <paramref name="type"/> onto the stack of the given Lua
        /// <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        public void PushClrTypeMetatable(IntPtr state, Type type) =>
            PushClrTypeOrObjectMetatable(state, type, isType: true);

        /// <summary>
        /// Pushes the metatable for the specified CLR <paramref name="obj"/> onto the stack of the given Lua
        /// <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The CLR object.</param>
        public void PushClrObjectMetatable(IntPtr state, object obj) =>
            PushClrTypeOrObjectMetatable(state, obj.GetType(), isType: false);

        private void PushClrTypeOrObjectMetatable(IntPtr state, Type typeOrObjType, bool isType)
        {
            Debug.Assert(state != IntPtr.Zero);
            Debug.Assert(typeOrObjType != null);

            var tableReference = isType ? _typeTableReference : _objectTableReference;
            var tableKeys = isType ? _typeTableKeys : _objectTableKeys;

            // If the metatable is cached, then retrieve it. Otherwise, generate it and then cache it.
            if (tableKeys.TryGetValue(typeOrObjType, out var key))
            {
                lua_rawgeti(state, LUA_REGISTRYINDEX, tableReference);
                lua_rawgeti(state, -1, key);
                lua_remove(state, -2);  // Remove the metatable table from the stack
            }
            else
            {
                if (isType)
                {
                    GenerateClrTypeMetatable(state, typeOrObjType);
                }
                else
                {
                    GenerateClrObjectMetatable(state, typeOrObjType);
                }

                key = tableKeys.Count + 1;  // Sequentially generate the keys to reduce memory usage
                lua_rawgeti(state, LUA_REGISTRYINDEX, tableReference);
                lua_pushvalue(state, -2);
                lua_rawseti(state, -2, key);
                lua_pop(state, 1);  // Pop the metatable table from the stack

                tableKeys[typeOrObjType] = key;
            }
        }

        private void GenerateClrTypeMetatable(IntPtr state, Type type)
        {
            lua_newtable(state);

            lua_pushcfunction(state, _gcCallback);
            lua_setfield(state, -2, "__gc");

            lua_pushcfunction(state, _toStringCallback);
            lua_setfield(state, -2, "__tostring");

            lua_pushcfunction(state, ProtectedCall(ClrTypeIndexGenerator.Generate(type)));
            lua_setfield(state, -2, "__index");
        }

        private void GenerateClrObjectMetatable(IntPtr state, Type objType)
        {
            throw new NotImplementedException();
        }
    }
}
