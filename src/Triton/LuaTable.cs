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
    /// Represents a Lua table that may be read and modified.
    /// </summary>
    public sealed class LuaTable : LuaReference {
        internal LuaTable(Lua lua, int referenceId) : base(lua, referenceId) {
        }

        /// <summary>
        /// Gets or sets the value corresponding to the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
		public object this[object key] {
            get {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }

                return GetValueInternal(key);
            }
            set {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }

                SetValueInternal(key, value);
            }
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = GetValueInternal(binder.Name);
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            SetValueInternal(binder.Name, value);
            return true;
        }
#endif

        private object GetValueInternal(object key) {
            PushOnto(Lua.State);
            Lua.PushObject(key);
            var type = LuaApi.GetTable(Lua.State, -2);
            var result = Lua.ToObject(-1, type);
            LuaApi.Pop(Lua.State, 2);
            return result;
        }

        private void SetValueInternal(object key, object value) {
            PushOnto(Lua.State);
            Lua.PushObject(key);
            Lua.PushObject(value);
            LuaApi.SetTable(Lua.State, -3);
            LuaApi.Pop(Lua.State, 1);
        }
    }
}
