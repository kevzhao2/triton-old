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
using Triton.Native;
using static Triton.Native.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua thread.
    /// </summary>
    public sealed unsafe class LuaThread : LuaObject
    {
        internal LuaThread(LuaEnvironment environment, int reference, lua_State* state) :
            base(environment, reference, state)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the thread can be started.
        /// </summary>
        /// <value><see langword="true"/> if the thread can be started; otherwise, <see langword="false"/>.</value>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public bool CanStart
        {
            get
            {
                _environment.ThrowIfDisposed();
                return lua_status(_state) == LuaStatus.Ok;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the thread can be resumed.
        /// </summary>
        /// <value><see langword="true"/> if the thread can be resumed; otherwise, <see langword="false"/>.</value>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public bool CanResume
        {
            get
            {
                _environment.ThrowIfDisposed();
                return lua_status(_state) == LuaStatus.Yield;
            }
        }

        // TODO: consider optimization by adding generic overloads

        /// <summary>
        /// Starts the thread with the given <paramref name="function"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The thread cannot be started.</exception>
        /// <exception cref="LuaEvaluationException">A Lua error occurred when starting the thread.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object?[] Start(LuaFunction function, params object[] args)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _environment.ThrowIfDisposed();
            _environment.ThrowIfNotEnoughLuaStack(_state, 1 + args.Length);  // (1 + numArgs) stack slots required
            ThrowIfCannotStart();

            _environment.PushObject(_state, function);
            var stackDelta = 1;

            try
            {
                foreach (var arg in args)
                {
                    _environment.PushObject(_state, arg);
                    ++stackDelta;
                }
            }
            catch
            {
                lua_pop(_state, stackDelta);
                throw;
            }

            return ResumeInternal(args.Length);
        }

        /// <summary>
        /// Resumes the thread with the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The thread cannot be resumed.</exception>
        /// <exception cref="LuaEvaluationException">A Lua error occurred when evaluating the thread.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object?[] Resume(params object[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _environment.ThrowIfDisposed();
            _environment.ThrowIfNotEnoughLuaStack(_state, args.Length);  // numArgs stack slots required
            ThrowIfCannotResume();

            var stackDelta = 0;

            try
            {
                foreach (var arg in args)
                {
                    _environment.PushObject(_state, arg);
                    ++stackDelta;
                }
            }
            catch
            {
                lua_pop(_state, stackDelta);
                throw;
            }

            return ResumeInternal(args.Length);
        }

        private object?[] ResumeInternal(int numArgs)
        {
            Debug.Assert(numArgs >= 0);

            int numResults;
            var status = lua_resume(_state, null, numArgs, &numResults);
            if (status != LuaStatus.Ok && status != LuaStatus.Yield)
            {
                throw _environment.CreateExceptionFromLuaStack<LuaEvaluationException>(_state);
            }

            return _environment.MarshalResults(_state, numResults);
        }

        // Throws an `InvalidOperationException` if the thread cannot be started.
        private void ThrowIfCannotStart()
        {
            if (!CanStart)
            {
                throw new InvalidOperationException("Lua thread cannot be started");
            }
        }

        // Throws an `InvalidOperationException` if the thread cannot be resumed.
        private void ThrowIfCannotResume()
        {
            if (!CanResume)
            {
                throw new InvalidOperationException("Lua thread cannot be resumed");
            }
        }
    }
}
