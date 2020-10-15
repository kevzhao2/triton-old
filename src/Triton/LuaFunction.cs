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
using static Triton.Lua;

namespace Triton
{
    /// <summary>
    /// Represents a Lua function. A function is a Lua object which can be called to receive results.
    /// </summary>
    public sealed unsafe class LuaFunction : LuaObject
    {
        internal LuaFunction(lua_State* state, LuaEnvironment environment, int @ref) : base(state, environment, @ref)
        {
        }

        /// <summary>
        /// Calls the function with no arguments.
        /// </summary>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call()
        {
            PushSelf();  // Performs validation

            return lua_pcall(_state, 0, -1, 0);
        }

        /// <summary>
        /// Calls the function with the given argument.
        /// </summary>
        /// <param name="arg">The argument to call the function with.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(in LuaValue arg)
        {
            PushSelf();  // Performs validation

            arg.Push(_state);
            return lua_pcall(_state, 1, -1, 0);
        }

        /// <summary>
        /// Calls the function with the given arguments.
        /// </summary>
        /// <param name="arg">The first argument to call the function with.</param>
        /// <param name="arg2">The second argument to call the function with.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(in LuaValue arg, in LuaValue arg2)
        {
            PushSelf();  // Performs validation

            arg.Push(_state);
            arg2.Push(_state);
            return lua_pcall(_state, 2, -1, 0);
        }

        /// <summary>
        /// Calls the function with the given arguments.
        /// </summary>
        /// <param name="arg">The first argument to call the function with.</param>
        /// <param name="arg2">The second argument to call the function with.</param>
        /// <param name="arg3">The third argument to call the function with.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(in LuaValue arg, in LuaValue arg2, in LuaValue arg3)
        {
            PushSelf();  // Performs validation

            arg.Push(_state);
            arg2.Push(_state);
            arg3.Push(_state);
            return lua_pcall(_state, 3, -1, 0);
        }

        /// <summary>
        /// Calls the function with the given arguments.
        /// </summary>
        /// <param name="args">The arguments to call the function with.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(params LuaValue[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            PushSelf();  // Performs validation

            for (var i = 0; i < args.Length; ++i)
            {
                args[i].Push(_state);
            }
            return lua_pcall(_state, args.Length, -1, 0);
        }

        private void PushSelf()
        {
            _environment.ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _ref);
        }
    }
}
