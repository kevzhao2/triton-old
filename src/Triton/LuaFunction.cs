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
#if NETSTANDARD || NET40
using System.Dynamic;
using System.Linq;
#endif
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua function.
    /// </summary>
    public class LuaFunction : LuaReference {
        internal LuaFunction(IntPtr state, int reference) : base(state, reference) {
        }

        /// <summary>
        /// Calls the function with given arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaFunction"/> is disposed.</exception>
        public object[] Call(params object[] args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return CallInternal(args);
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaFunction"/> is disposed.</exception>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            // Since we can only return one result, let's just try returning the first.
            result = CallInternal(args).FirstOrDefault();
            return true;
        }
#endif

        private object[] CallInternal(object[] args) {
            // Ensure that we have enough stack space for debug.traceback, the function, and its arguments.
            var numArgs = args.Length;
            if (!LuaApi.CheckStack(State, numArgs + 3)) {
                throw new LuaException("Not enough stack space for traceback, function, and arguments.");
            }

            var oldTop = LuaApi.GetTop(State);
            try {
                LuaApi.GetGlobal(State, "debug");
                LuaApi.GetField(State, -1, "traceback");

                LuaApi.RawGetI(State, LuaApi.RegistryIndex, Reference);
                foreach (var arg in args) {
                    LuaApi.PushObject(State, arg);
                }

                if (LuaApi.PCallK(State, numArgs, LuaApi.MultRet, oldTop + 2) != LuaStatus.Ok) {
                    var errorMessage = LuaApi.ToString(State, -1);
                    throw new LuaException(errorMessage);
                }

                // Ensure that we have enough stack space for ToObjects, which can require up to 1 slot.
                var top = LuaApi.GetTop(State);
                if (!LuaApi.CheckStack(State, 1)) {
                    throw new LuaException("Not enough scratch stack space.");
                }

                return LuaApi.ToObjects(State, oldTop + 3, top);
            } finally {
                LuaApi.SetTop(State, oldTop);
            }
        }
    }
}
