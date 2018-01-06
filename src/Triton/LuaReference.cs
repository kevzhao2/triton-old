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
    /// Represents a Lua reference that is tied to a specific <see cref="Triton.Lua"/> instance.
    /// </summary>
    public abstract class LuaReference : IDisposable {
        private readonly IntPtr _pointer;
        private readonly int _reference;

        private bool _isDisposed;

        private protected LuaReference(Lua lua, IntPtr state, int reference, IntPtr pointer) {
            Lua = lua;
            State = state;
            _reference = reference;
            _pointer = pointer;
        }

        /// <summary>
        /// Finalizes the <see cref="LuaReference"/>, releasing its reference.
        /// </summary>
        ~LuaReference() => Dispose(false);

        /// <summary>
        /// Gets a value indicating whether the <see cref="LuaReference"/> is disposed.
        /// </summary>
        /// <value>A value indicating whether the <see cref="LuaReference"/> is disposed.</value>
        public bool IsDisposed => _isDisposed || Lua.IsDisposed;

        private protected Lua Lua { get; }
        private protected IntPtr State { get; }

        /// <summary>
        /// Disposes the <see cref="LuaReference"/>, releasing its reference.
        /// </summary>
        /// <remarks>
        /// Great care must be taken when calling this method. LuaReferences are cached, so disposing one LuaReference may have a rather
        /// far-reaching effect!
        /// </remarks>
        public void Dispose() => Dispose(true);

        internal void PushSelf() => LuaApi.RawGetI(State, LuaApi.RegistryIndex, _reference);

        private void Dispose(bool disposing) {
            if (IsDisposed) {
                return;
            }

            if (disposing) {
                GC.SuppressFinalize(this);
            }

            Lua.Unref(_reference, _pointer, disposing);
            _isDisposed = true;
        }
    }
}
