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
#endif
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua reference that is tied to a specific <see cref="Triton.Lua"/> instance.
    /// </summary>
#if NETSTANDARD || NET40
    public abstract class LuaReference : DynamicObject, IDisposable {
#else
    public abstract class LuaReference : IDisposable {
#endif
        internal LuaReference(IntPtr state, int reference) {
            State = state;
            Reference = reference;
        }
        
        /// <summary>
        /// Gets a value indicating whether the <see cref="LuaReference"/> is disposed.
        /// </summary>
        /// <value>A value indicating whether the <see cref="LuaReference"/> is disposed.</value>
        public bool IsDisposed { get; private set; }
        
        internal IntPtr State { get; }
        internal int Reference { get; }

        /// <summary>
        /// Disposes the <see cref="LuaReference"/>, releasing its reference.
        /// </summary>
        public void Dispose() {
            if (IsDisposed) {
                return;
            }

            LuaApi.Unref(State, LuaApi.RegistryIndex, Reference);
            IsDisposed = true;
        }
    }
}
