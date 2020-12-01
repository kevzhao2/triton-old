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
using System.Diagnostics.CodeAnalysis;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua thread, an object representing a thread of execution which can be resumed with arguments to
    /// receive results.
    /// </summary>
    /// <remarks>
    /// Instances of this class store the threads in the Lua registry in order to reference them. This means that an
    /// instance is associated with its environment, and <i>must</i> be disposed of to prevent a Lua memory leak.
    /// </remarks>
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

            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                ThrowHelper.ThrowInvalidOperationException("Thread is not ready to execute a function");

            lua_settop(state, 0);  // ensure that the function will be at index 1

            function.Push(state);
            _hasFunction = true;  // mark the thread as having a function to reduce the number of P/Invokes
        }

        #region Resume overloads

        /// <summary>
        /// Resumes the thread with no arguments.
        /// </summary>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume()
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            return lua_resume(state, null, 0);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            return lua_resume(state, null, 1);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            return lua_resume(state, null, 2);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            argument3.Push(state);
            return lua_resume(state, null, 3);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            argument3.Push(state);
            argument4.Push(state);
            return lua_resume(state, null, 4);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            argument3.Push(state);
            argument4.Push(state);
            argument5.Push(state);
            return lua_resume(state, null, 5);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            argument3.Push(state);
            argument4.Push(state);
            argument5.Push(state);
            argument6.Push(state);
            return lua_resume(state, null, 6);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            argument3.Push(state);
            argument4.Push(state);
            argument5.Push(state);
            argument6.Push(state);
            argument7.Push(state);
            return lua_resume(state, null, 7);
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
            ThrowIfDisposed();

            var state = _state;  // local optimization

            if (lua_status(state) == LUA_YIELD)
                lua_settop(state, 0);  // ensure that the results will begin at index 1
            else
                ThrowIfNoFunction();

            argument.Push(state);
            argument2.Push(state);
            argument3.Push(state);
            argument4.Push(state);
            argument5.Push(state);
            argument6.Push(state);
            argument7.Push(state);
            argument8.Push(state);
            return lua_resume(state, null, 8);
        }

        #endregion

        [ExcludeFromCodeCoverage]
        internal void Push(lua_State* state)
        {
            // Checking pointer equality is sufficient for checking environment equality since only one handle is
            // allocated per environment.
            //
            if (*(IntPtr*)lua_getextraspace(state) != *(IntPtr*)lua_getextraspace(_state))
                ThrowHelper.ThrowInvalidOperationException("Thread is not associated with this environment");
            
            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
        }

        [ExcludeFromCodeCoverage]
        internal string ToDebugString() => $"thread: 0x{(long)_state:x)}";

        [DebuggerStepThrough]
        [ExcludeFromCodeCoverage]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaThread));
        }

        [DebuggerStepThrough]
        [ExcludeFromCodeCoverage]
        private void ThrowIfNoFunction()
        {
            if (!_hasFunction)
                ThrowHelper.ThrowInvalidOperationException("Thread has no function to execute");

            _hasFunction = false;  // reset the mark since the function will immediately be used
        }
    }
}
