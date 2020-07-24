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
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua thread.
    /// </summary>
    public sealed class LuaThread : LuaObject
    {
        internal LuaThread(IntPtr state, LuaEnvironment environment, int reference) :
            base(state, environment, reference)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the Lua thread can be started.
        /// </summary>
        /// <value><see langword="true"/> if the Lua thread can be started; otherwise, <see langword="false"/>.</value>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public bool CanStart
        {
            get
            {
                ThrowIfDisposed();
                return lua_status(_state) == LuaStatus.Ok;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Lua thread can be resumed.
        /// </summary>
        /// <value><see langword="true"/> if the Lua thread can be resumed; otherwise, <see langword="false"/>.</value>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public bool CanResume
        {
            get
            {
                ThrowIfDisposed();
                return lua_status(_state) == LuaStatus.Yield;
            }
        }

        /// <summary>
        /// Starts the Lua thread, running the given <paramref name="function"/> with no arguments.
        /// </summary>
        /// <param name="function">The function to run on the Lua thread.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be started.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Start(LuaFunction function)
        {
            StartPrologue(function);  // Performs validation
            return StartOrResumeShared(0);
        }

        /// <summary>
        /// Starts the Lua thread, running the given <paramref name="function"/> with a single argument.
        /// </summary>
        /// <param name="function">The function to run on the Lua thread.</param>
        /// <param name="arg">The argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be started.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Start(LuaFunction function, in LuaValue arg)
        {
            StartPrologue(function);  // Performs validation
            _environment.PushValue(_state, arg);
            return StartOrResumeShared(1);
        }

        /// <summary>
        /// Starts the Lua thread, running the given <paramref name="function"/> with two arguments.
        /// </summary>
        /// <param name="function">The function to run on the Lua thread.</param>
        /// <param name="arg">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be started.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Start(LuaFunction function, in LuaValue arg, in LuaValue arg2)
        {
            StartPrologue(function);  // Performs validation
            _environment.PushValue(_state, arg);
            _environment.PushValue(_state, arg2);
            return StartOrResumeShared(2);
        }

        /// <summary>
        /// Starts the Lua thread, running the given <paramref name="function"/> with three arguments.
        /// </summary>
        /// <param name="function">The function to run on the Lua thread.</param>
        /// <param name="arg">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be started.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Start(LuaFunction function, in LuaValue arg, in LuaValue arg2, in LuaValue arg3)
        {
            StartPrologue(function);  // Performs validation
            _environment.PushValue(_state, arg);
            _environment.PushValue(_state, arg2);
            _environment.PushValue(_state, arg3);
            return StartOrResumeShared(3);
        }

        /// <summary>
        /// Starts the Lua thread, running the given <paramref name="function"/> with the given <paramref name="args"/>.
        /// </summary>
        /// <param name="function">The function to run on the Lua thread.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="function"/> or <paramref name="args"/> are <see langword="null"/>.
        /// </exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Start(LuaFunction function, params LuaValue[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            StartPrologue(function);  // Performs validation
            for (var i = 0; i < args.Length; ++i)
            {
                _environment.PushValue(_state, args[i]);
            }
            return StartOrResumeShared(args.Length);
        }

        /// <summary>
        /// Resumes the Lua thread with no arguments.
        /// </summary>
        /// <returns>The results.</returns>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be resumed.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Resume()
        {
            ResumePrologue();  // Performs validation
            return StartOrResumeShared(0);
        }

        /// <summary>
        /// Resumes the Lua thread with a single argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be resumed.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Resume(in LuaValue arg)
        {
            ResumePrologue();  // Performs validation
            _environment.PushValue(_state, arg);
            return StartOrResumeShared(1);
        }

        /// <summary>
        /// Resumes the Lua thread with two arguments.
        /// </summary>
        /// <param name="arg">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be resumed.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Resume(in LuaValue arg, in LuaValue arg2)
        {
            ResumePrologue();  // Performs validation
            _environment.PushValue(_state, arg);
            _environment.PushValue(_state, arg2);
            return StartOrResumeShared(2);
        }

        /// <summary>
        /// Resumes the Lua thread with three arguments.
        /// </summary>
        /// <param name="arg">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be resumed.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Resume(in LuaValue arg, in LuaValue arg2, in LuaValue arg3)
        {
            ResumePrologue();  // Performs validation
            _environment.PushValue(_state, arg);
            _environment.PushValue(_state, arg2);
            _environment.PushValue(_state, arg3);
            return StartOrResumeShared(3);
        }

        /// <summary>
        /// Resumes the Lua thread with the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The Lua thread cannot be resumed.</exception>
        /// <exception cref="LuaEvalException">A Lua error occured when evaluating the thread.</exception>
        /// <exception cref="ObjectDisposedException">The Lua thread is disposed.</exception>
        public LuaResults Resume(params LuaValue[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            ResumePrologue();  // Performs validation
            for (var i = 0; i < args.Length; ++i)
            {
                _environment.PushValue(_state, args[i]);
            }
            return StartOrResumeShared(args.Length);
        }

        private void StartPrologue(LuaFunction function)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (!CanStart)  // Checks for disposed
            {
                throw new InvalidOperationException("Lua thread cannot be started");
            }

            lua_settop(_state, 0);  // Reset stack

            _environment.PushLuaObject(_state, function);
        }

        private void ResumePrologue()
        {
            if (!CanResume)  // Checks for disposed
            {
                throw new InvalidOperationException("Lua thread cannot be resumed");
            }

            lua_settop(_state, 0);  // Reset stack
        }

        private LuaResults StartOrResumeShared(int numArgs)
        {
            Debug.Assert(numArgs >= 0);

            var status = lua_resume(_state, IntPtr.Zero, numArgs, out _);
            if (status != LuaStatus.Ok && status != LuaStatus.Yield)
            {
                throw _environment.CreateExceptionFromStack<LuaEvalException>(_state);
            }

            return new LuaResults(_state, _environment);
        }
    }
}
