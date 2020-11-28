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
    /// Represents a Lua thread, an object representing a thread of execution which can be resumed with arguments to
    /// receive results.
    /// </summary>
    public sealed unsafe class LuaThread : IDisposable
    {
        private readonly lua_State* _state;
        private readonly int _ref;

        private bool _hasFunction;
        private bool _isDisposed;

        internal LuaThread(lua_State* state, int @ref)
        {
            _state = state;
            _ref = @ref;
        }

        /// <summary>
        /// Gets a value indicating whether the thread is ready to execute a function.
        /// </summary>
        public bool IsReady
        {
            get
            {
                ThrowIfDisposed();

                return lua_status(_state) != LUA_YIELD;
            }
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
        /// Sets the function to be executed on the thread.
        /// </summary>
        /// <param name="function">The function to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The thread is not ready to execute a function.</exception>
        public void SetFunction(LuaFunction function)
        {
            if (function is null)
                ThrowHelper.ThrowArgumentNullException(nameof(function));
            if (!IsReady)  // performs disposed validation
                ThrowHelper.ThrowInvalidOperationException("Thread is not ready to execute a function");

            // Reset the stack so that the results start at index 1.
            //
            lua_settop(_state, 0);

            function.Push(_state);
            _hasFunction = true;
        }

        /// <summary>
        /// Resumes the thread with no arguments.
        /// </summary>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume()
        {
            ResumePrologue();  // performs validation

            return lua_resume(_state, null, 0);
        }

        /// <summary>
        /// Resumes the thread with one argument.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            return lua_resume(_state, null, 1);
        }

        /// <summary>
        /// Resumes the thread with two arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            return lua_resume(_state, null, 2);
        }

        /// <summary>
        /// Resumes the thread with three arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            return lua_resume(_state, null, 3);
        }

        /// <summary>
        /// Resumes the thread with four arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            return lua_resume(_state, null, 4);
        }

        /// <summary>
        /// Resumes the thread with five arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            return lua_resume(_state, null, 5);
        }

        /// <summary>
        /// Resumes the thread with six arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <param name="argument6">The sixth argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5,
            in LuaArgument argument6)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            argument6.Push(_state);
            return lua_resume(_state, null, 6);
        }

        /// <summary>
        /// Resumes the thread with seven arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <param name="argument6">The sixth argument.</param>
        /// <param name="argument7">The seventh argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5,
            in LuaArgument argument6,
            in LuaArgument argument7)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            argument6.Push(_state);
            argument7.Push(_state);
            return lua_resume(_state, null, 7);
        }

        /// <summary>
        /// Resumes the thread with eight arguments.
        /// </summary>
        /// <param name="argument">The first argument.</param>
        /// <param name="argument2">The second argument.</param>
        /// <param name="argument3">The third argument.</param>
        /// <param name="argument4">The fourth argument.</param>
        /// <param name="argument5">The fifth argument.</param>
        /// <param name="argument6">The sixth argument.</param>
        /// <param name="argument7">The seventh argument.</param>
        /// <param name="argument8">The eighth argument.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(
            in LuaArgument argument,
            in LuaArgument argument2,
            in LuaArgument argument3,
            in LuaArgument argument4,
            in LuaArgument argument5,
            in LuaArgument argument6,
            in LuaArgument argument7,
            in LuaArgument argument8)
        {
            ResumePrologue();  // performs validation

            argument.Push(_state);
            argument2.Push(_state);
            argument3.Push(_state);
            argument4.Push(_state);
            argument5.Push(_state);
            argument6.Push(_state);
            argument7.Push(_state);
            argument8.Push(_state);
            return lua_resume(_state, null, 8);
        }

        internal void Push(lua_State* state)
        {
            if (*(GCHandle*)lua_getextraspace(state) != *(GCHandle*)lua_getextraspace(_state))
                ThrowHelper.ThrowInvalidOperationException("Thread is not associated with the given environment");

            var type = lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
            Debug.Assert(type == LUA_TTHREAD);
        }

        private void ResumePrologue()
        {
            if (!IsReady)  // performs disposed validation
            {
                // Reset the stack so that the results start at index 1.
                //
                lua_settop(_state, 0);
            }
            else
            {
                if (!_hasFunction)
                    ThrowHelper.ThrowInvalidOperationException("Thread has no function to execute");

                _hasFunction = false;
            }
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaThread));
        }
    }
}
