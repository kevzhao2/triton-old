// Copyright (c) 2018 Kevin Zhao
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
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua thread which may be resumed.
    /// </summary>
    public sealed class LuaThread : LuaReference {
        private readonly IntPtr _threadState;

        internal LuaThread(Lua lua, int referenceId, IntPtr threadState) : base(lua, referenceId) {
            _threadState = threadState;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="LuaThread"/> can be resumed.
        /// </summary>
        /// <value>A value indicating whether the <see cref="LuaThread"/> can be resumed.</value>
        public bool CanResume {
            get {
                // The thread can be resumed if it yielded...
                var status = LuaApi.Status(_threadState);
                if (status == LuaStatus.Yield) {
                    return true;
                }

                // or if it was just created, meaning it has no stack frames and has something in its stack.
                if (status != LuaStatus.Ok) {
                    return false;
                }

                var debug = new LuaDebug();
                return LuaApi.GetStack(_threadState, 0, ref debug) == 0 && LuaApi.GetTop(_threadState) > 0;
            }
        }

        /// <summary>
        /// Resumes the <see cref="LuaThread"/> with the given arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentException">
        /// One of the supplied arguments is a <see cref="LuaReference"/> which is tied to a different <see cref="Lua"/> environment.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="LuaThread"/> cannot be resumed.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        public object[] Resume(params object[] args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (!CanResume) {
                throw new InvalidOperationException("Thread cannot be resumed.");
            }

            return Lua.Call(args, _threadState, true);
        }
    }
}
