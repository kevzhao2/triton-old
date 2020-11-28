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
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua function, an object which can be called with arguments to receive results.
    /// </summary>
    public sealed unsafe class LuaFunction : IDisposable
    {
        private readonly lua_State* _state;
        private readonly int _ref;

        private bool _isDisposed;

        internal LuaFunction(lua_State* state, int @ref)
        {
            _state = state;
            _ref = @ref;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                luaL_unref(_state, LUA_REGISTRYINDEX, _ref);

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Calls the function with no arguments.
        /// </summary>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call()
        {
            CallPrologue();  // performs validation

            return lua_pcall(_state, 0, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with a single argument.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            return lua_pcall(_state, 1, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with two arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            return lua_pcall(_state, 2, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with three arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            return lua_pcall(_state, 3, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with four arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            return lua_pcall(_state, 4, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with five arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            return lua_pcall(_state, 5, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with six arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <param name="argument6">The sixth argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5,
            in LuaArgument argument6)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            argument6.Push(_state);
            return lua_pcall(_state, 6, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with seven arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <param name="argument6">The sixth argument.</param>
        /// <param name="argument7">The seventh argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5,
            in LuaArgument argument6,
            in LuaArgument argument7)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            argument6.Push(_state);
            argument7.Push(_state);
            return lua_pcall(_state, 7, LUA_MULTRET);
        }

        /// <summary>
        /// Calls the function with eight arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <param name="argument6">The sixth argument.</param>
        /// <param name="argument7">The seventh argument.</param>
        /// <param name="argument8">The eighth argument.</param>
        /// <returns>The results of the function call.</returns>
        /// <exception cref="LuaRuntimeException">The function call results in a Lua runtime error.</exception>
        public LuaResults Call(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5,
            in LuaArgument argument6,
            in LuaArgument argument7,
            in LuaArgument argument8)
        {
            CallPrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            argument6.Push(_state);
            argument7.Push(_state);
            argument8.Push(_state);
            return lua_pcall(_state, 8, LUA_MULTRET);
        }

        internal void Push(lua_State* state)
        {
            if (*(GCHandle*)lua_getextraspace(state) != *(GCHandle*)lua_getextraspace(_state))
                ThrowHelper.ThrowInvalidOperationException("Function is not associated with the given environment");

            var type = lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
            Debug.Assert(type == LUA_TFUNCTION);
        }

        private void CallPrologue()
        {
            ThrowIfDisposed();

            // Reset the stack so that the results start at index 1.
            //
            lua_settop(_state, 0);

            var type = lua_rawgeti(_state, LUA_REGISTRYINDEX, _ref);
            Debug.Assert(type == LUA_TFUNCTION);
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaFunction));
        }
    }
}
