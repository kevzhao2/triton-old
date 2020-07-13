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
        /// <exception cref="LuaExecutionException">A Lua error occurred during execution.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        public object?[] Call()
        {
            _environment.ThrowIfNotEnoughLuaStack(_state, 1);

            var stackDelta = 0;

            try
            {
                lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                ++stackDelta;

                return CallInternal(0);
            }
            finally
            {
                lua_pop(_state, stackDelta);
            }
        }

        /// <summary>
        /// Calls the function with the specified argument.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="arg">The argument.</param>
        /// <returns>The function results.</returns>
        /// <exception cref="LuaExecutionException">A Lua error occurred during execution.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        public object?[] Call<T>(T arg)
        {
            _environment.ThrowIfNotEnoughLuaStack(_state, 2);

            var stackDelta = 0;

            try
            {
                lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                ++stackDelta;

                _environment.Push(_state, arg);
                ++stackDelta;

                return CallInternal(1);
            }
            finally
            {
                lua_pop(_state, stackDelta);
            }
        }

        /// <summary>
        /// Calls the function with the specified arguments.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The function results.</returns>
        /// <exception cref="LuaExecutionException">A Lua error occurred during execution.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        public object?[] Call<T1, T2>(T1 arg1, T2 arg2)
        {
            _environment.ThrowIfNotEnoughLuaStack(_state, 3);

            var stackDelta = 0;

            try
            {
                lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                ++stackDelta;

                _environment.Push(_state, arg1);
                ++stackDelta;

                _environment.Push(_state, arg2);
                ++stackDelta;

                return CallInternal(2);
            }
            finally
            {
                lua_pop(_state, stackDelta);
            }
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
        /// <exception cref="LuaExecutionException">A Lua error occurred during execution.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        public object?[] Call<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            _environment.ThrowIfNotEnoughLuaStack(_state, 4);

            var stackDelta = 0;

            try
            {
                lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                ++stackDelta;

                _environment.Push(_state, arg1);
                ++stackDelta;

                _environment.Push(_state, arg2);
                ++stackDelta;

                _environment.Push(_state, arg3);
                ++stackDelta;

                return CallInternal(3);
            }
            finally
            {
                lua_pop(_state, stackDelta);
            }
        }

        private object?[] CallInternal(int numArgs)
        {
            Debug.Assert(numArgs >= 0);

            var oldTop = lua_gettop(_state) - numArgs - 1;
            var status = lua_pcall(_state, numArgs, -1, 0);
            if (status != Native.LuaStatus.Ok)
            {
                try
                {
                    var errorMessage = _environment.ToString(_state, -1);
                    throw new LuaExecutionException(errorMessage);
                }
                finally
                {
                    lua_pop(_state, 1);
                }
            }

            var numResults = lua_gettop(_state) - oldTop;
            if (numResults == 0)
            {
                return Array.Empty<object?>();
            }

            _environment.ThrowIfNotEnoughLuaStack(_state, 1);  // 1 stack slot required (due to LuaObject)

            var results = new object?[numResults];
            for (var i = 0; i < numResults; ++i)
            {
                results[i] = _environment.ToObject(_state, oldTop + i + 1);
            }

            return results;
        }
    }
}
