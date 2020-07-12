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
using System.Diagnostics;
using static Triton.Native.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua function.
    /// </summary>
    public sealed unsafe class LuaFunction : LuaObject
    {
        internal LuaFunction(LuaEnvironment environment, int reference, lua_State* state) :
            base(environment, reference, state)
        {
        }

        /// <summary>
        /// Calls the function with no arguments.
        /// </summary>
        /// <returns>The function results.</returns>
        /// <exception cref="LuaException">A Lua error occurred.</exception>
        public object?[] Call()
        {
            _environment.CheckStack(_state, 1);

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);

            return CallInternal(0);
        }

        /// <summary>
        /// Calls the function with the specified argument.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="arg">The argument.</param>
        /// <returns>The function results.</returns>
        /// <exception cref="LuaException">A Lua error occurred.</exception>
        public object?[] Call<T>(T arg)
        {
            _environment.CheckStack(_state, 2);

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
            _environment.Push(_state, arg);

            return CallInternal(1);
        }

        /// <summary>
        /// Calls the function with the specified arguments.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The function results.</returns>
        /// <exception cref="LuaException">A Lua error occurred.</exception>
        public object?[] Call<T1, T2>(T1 arg1, T2 arg2)
        {
            _environment.CheckStack(_state, 3);

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
            _environment.Push(_state, arg1);
            _environment.Push(_state, arg2);

            return CallInternal(2);
        }

        /// <summary>
        /// Calls the function with the specified arguments.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The function results.</returns>
        /// <exception cref="LuaException">A Lua error occurred.</exception>
        public object?[] Call<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            _environment.CheckStack(_state, 4);

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
            _environment.Push(_state, arg1);
            _environment.Push(_state, arg2);
            _environment.Push(_state, arg3);

            return CallInternal(3);
        }

        private object?[] CallInternal(int numArgs)
        {
            Debug.Assert(numArgs >= 0);

            var top = lua_gettop(_state) - numArgs - 1;
            var status = lua_pcall(_state, numArgs, -1, 0);
            if (status != Native.LuaStatus.Ok)
            {
                throw _environment.ToLuaException(_state);
            }

            var numResults = lua_gettop(_state) - top;
            if (numResults == 0)
            {
                return Array.Empty<object?>();
            }

            // We potentially need one extra slot on the stack, in case one of the results is a Lua object and it needs
            // to be pushed in order for `luaL_ref` to be called on it.
            _environment.CheckStack(_state, 1);

            var results = new object?[numResults];
            for (var i = 0; i < numResults; ++i)
            {
                results[i] = _environment.ToObject(_state, top + i + 1);
            }

            return results;
        }
    }
}
