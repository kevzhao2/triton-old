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
    /// Represents a Lua thread.
    /// </summary>
    public sealed class LuaThread : LuaReference {
        private readonly IntPtr _thread;

        internal LuaThread(IntPtr state, int reference, IntPtr thread) : base(state, reference) {
            _thread = thread;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="LuaThread"/> can be resumed.
        /// </summary>
        /// <value>A value indicating whether the <see cref="LuaThread"/> can be resumed.</value>
        public bool CanResume {
            get {
                if (IsDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                return CanResumeInternal();
            }
        }
        
        /// <summary>
        /// Resumes the <see cref="LuaThread"/> with the given arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="LuaThread"/> cannot be resumed.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaThread"/> is disposed.</exception>
        public object[] Resume(params object[] args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (!CanResumeInternal()) {
                throw new InvalidOperationException("Thread cannot be resumed.");
            }
            
            // Ensure that we have enough stack space on the thread for the arguments.
            var numArgs = args.Length;
            if (!LuaApi.CheckStack(_thread, numArgs)) {
                throw new LuaException("Not enough stack space on thread for arguments.");
            }

            var oldThreadTop = LuaApi.GetTop(_thread);
            try {
                foreach (var arg in args) {
                    LuaApi.PushObject(_thread, arg);
                }

                var status = LuaApi.Resume(_thread, State, numArgs);
                if (status != LuaStatus.Ok && status != LuaStatus.Yield) {
                    var errorMessage = LuaApi.ToString(_thread, -1);
                    throw new LuaException(errorMessage);
                }

                // Ensure that we have enough stack space on the thread for ToObjects.
                var numResults = LuaApi.GetTop(_thread);
                if (!LuaApi.CheckStack(_thread, 1)) {
                    throw new LuaException("Not enough scratch stack space on thread.");
                }
                
                return LuaApi.ToObjects(_thread, 1, numResults);
            } finally {
                LuaApi.SetTop(_thread, 0);
            }
        }

        private bool CanResumeInternal() {
            var status = LuaApi.Status(_thread);
            if (status == LuaStatus.Yield) {
                return true;
            }

            var debug = new LuaDebug();
            return status == LuaStatus.Ok && LuaApi.GetStack(_thread, 0, ref debug) == 0 && LuaApi.GetTop(_thread) > 0;
        }
    }
}
