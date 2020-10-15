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
using static Triton.Lua.LuaStatus;

namespace Triton
{
    /// <summary>
    /// Represents a Lua thread. A thread is a Lua object which represents an independent thread of execution.
    /// </summary>
    public sealed unsafe class LuaThread : LuaObject
    {
        internal LuaThread(lua_State* state, LuaEnvironment environment, int @ref) : base(state, environment, @ref)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the thread is ready to start executing a function.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the thread is ready to start executing a function; otherwise,
        /// <see langword="false"/>.
        /// </value>
        public bool IsReady
        {
            get
            {
                _environment.ThrowIfDisposed();

                return lua_status(_state) == LUA_OK;
            }
        }

        /// <summary>
        /// Resumes the thread with no arguments.
        /// </summary>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume()
        {
            CheckSelf();  // Performs validation

            return lua_resume(_state, null, 0);
        }

        /// <summary>
        /// Resumes the thread with the given argument.
        /// </summary>
        /// <param name="arg">The argument to resume the thread with.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(in LuaValue arg)
        {
            CheckSelf();  // Performs validation

            arg.Push(_state);
            return lua_resume(_state, null, 1);
        }

        /// <summary>
        /// Resumes the thread with the given arguments.
        /// </summary>
        /// <param name="arg">The first argument to resume the thread with.</param>
        /// <param name="arg2">The second argument to resume the thread with.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(in LuaValue arg, in LuaValue arg2)
        {
            CheckSelf();  // Performs validation

            arg.Push(_state);
            arg2.Push(_state);
            return lua_resume(_state, null, 2);
        }

        /// <summary>
        /// Resumes the thread with the given arguments.
        /// </summary>
        /// <param name="arg">The first argument to resume the thread with.</param>
        /// <param name="arg2">The second argument to resume the thread with.</param>
        /// <param name="arg3">The third argument to resume the thread with.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(in LuaValue arg, in LuaValue arg2, in LuaValue arg3)
        {
            CheckSelf();  // Performs validation

            arg.Push(_state);
            arg2.Push(_state);
            arg3.Push(_state);
            return lua_resume(_state, null, 3);
        }

        /// <summary>
        /// Resumes the thread with the given arguments.
        /// </summary>
        /// <param name="args">The arguments to resume the thread with.</param>
        /// <returns>The results of the thread resumption.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The thread has no function to execute.</exception>
        /// <exception cref="LuaRuntimeException">The thread resumption results in a Lua runtime error.</exception>
        public LuaResults Resume(params LuaValue[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            CheckSelf();  // Performs validation

            for (var i = 0; i < args.Length; ++i)
            {
                args[i].Push(_state);
            }
            return lua_resume(_state, null, args.Length);
        }

        /// <summary>
        /// Sets the function that thread should execute.
        /// </summary>
        /// <param name="function">The function to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The thread is not ready to start executing a function.</exception>
        public void SetFunction(LuaFunction function)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (!IsReady)  // Performs validation
            {
                throw new InvalidOperationException("Thread is not ready to start executing a function");
            }

            lua_settop(_state, 0);  // Reset stack

            function.Push(_state);
        }

        private void CheckSelf()
        {
            if (!IsReady)  // Performs validation
            {
                lua_settop(_state, 0);  // Reset stack
            }
            else if (lua_gettop(_state) != 1)
            {
                throw new InvalidOperationException("Thread has no function to execute");
            }
        }
    }
}
