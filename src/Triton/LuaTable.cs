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
    /// Represents a Lua table.
    /// </summary>
    public sealed class LuaTable : LuaReference {
        internal LuaTable(IntPtr state, int reference) : base(state, reference) {
        }

        /// <summary>
        /// Gets or sets the value associated with the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value, or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c> and is being set.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaTable"/> is disposed.</exception>
        public object this[object key] {
            get {
                if (IsDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                return GetValueInternal(key);
            }
            set {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                if (IsDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                SetValueInternal(key, value);
            }
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaTable"/> is disposed.</exception>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            result = GetValueInternal(binder.Name);
            return true;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="LuaTable"/> is disposed.</exception>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            SetValueInternal(binder.Name, value);
            return true;
        }
#endif

        private object GetValueInternal(object key) {
            LuaApi.RawGetI(State, LuaApi.RegistryIndex, Reference);
            
            object result;
            if (key is string s) {
                LuaApi.GetField(State, -1, s);
                result = LuaApi.ToObject(State, -1);
            } else {
                LuaApi.PushObject(State, key);
                var type = LuaApi.GetTable(State, -2);
                result = LuaApi.ToObject(State, -1, type);
            }
            
            LuaApi.Pop(State, 2);
            return result;
        }

        private void SetValueInternal(object key, object value) {
            LuaApi.RawGetI(State, LuaApi.RegistryIndex, Reference);
            
            if (key is string s) {
                LuaApi.PushObject(State, value);
                LuaApi.SetField(State, -2, s);
            } else {
                LuaApi.PushObject(State, key);
                LuaApi.PushObject(State, value);
                LuaApi.SetTable(State, -3);
            }
            
            LuaApi.Pop(State, 1);
        }
    }
}
