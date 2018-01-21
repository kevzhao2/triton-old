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
using System.Collections;
using System.Collections.Generic;
#if NETSTANDARD || NET40
using System.Dynamic;
#endif
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua table that may be read and modified.
    /// </summary>
    public sealed class LuaTable : LuaReference, IEnumerable<KeyValuePair<object, object>> {
        internal LuaTable(Lua lua, int referenceId) : base(lua, referenceId) {
        }

        /// <summary>
        /// Gets the number of key-value pairs.
        /// </summary>
        /// <value>The number of key-value pairs.</value>
        public int Count {
            get {
                PushOnto(Lua.MainState);
                LuaApi.PushNil(Lua.MainState);

                var result = 0;
                while (LuaApi.Next(Lua.MainState, -2)) {
                    LuaApi.Pop(Lua.MainState, 1);
                    ++result;
                }
                LuaApi.Pop(Lua.MainState, 1);
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the value corresponding to the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is a <see cref="LuaReference"/> which is tied to a different <see cref="Lua"/> environment.
        /// </exception>
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

        /// <summary>
        /// Determines whether the <see cref="LuaTable"/> contains the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the <see cref="LuaTable"/> contains <paramref name="key"/>, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool ContainsKey(object key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            
            PushOnto(Lua.MainState);
            Lua.PushObject(key);
            var type = LuaApi.GetTable(Lua.MainState, -2);
            var result = type != LuaType.Nil;
            LuaApi.Pop(Lua.MainState, 2);
            return result;
        }

        /// <summary>
        /// Gets an enumerator iterating through the key-value pairs.
        /// </summary>
        /// <returns>An enumerator iterating through the key-value pairs.</returns>
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Tries to get the value associated with the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the <see cref="LuaTable"/> contains <paramref name="key"/>, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool TryGetValue(object key, out object value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            value = GetValueInternal(key);
            return value != null;
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
            PushOnto(Lua.MainState);
            Lua.PushObject(key);
            var type = LuaApi.GetTable(Lua.MainState, -2);
            var result = Lua.ToObject(-1, type);
            LuaApi.Pop(Lua.MainState, 2);
            return result;
        }

        private void SetValueInternal(object key, object value) {
            PushOnto(Lua.MainState);
            Lua.PushObject(key);
            Lua.PushObject(value);
            LuaApi.SetTable(Lua.MainState, -3);
            LuaApi.Pop(Lua.MainState, 1);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        private sealed class Enumerator : IEnumerator<KeyValuePair<object, object>> {
            private LuaTable _table;
            
            public Enumerator(LuaTable table) => _table = table;
            
            public KeyValuePair<object, object> Current { get; private set; }

            object IEnumerator.Current => Current;
            
            public void Dispose() => _table = null;
            
            public bool MoveNext() {
                ThrowIfDisposed();

                var lua = _table.Lua;
                _table.PushOnto(lua.MainState);
                lua.PushObject(Current.Key);
                if (!LuaApi.Next(lua.MainState, -2)) {
                    LuaApi.Pop(lua.MainState, 1);
                    return false;
                }

                var key = lua.ToObject(-2);
                var value = lua.ToObject(-1);
                Current = new KeyValuePair<object, object>(key, value);
                LuaApi.Pop(lua.MainState, 3);
                return true;
            }

            public void Reset() {
                ThrowIfDisposed();

                Current = default(KeyValuePair<object, object>);
            }

            private void ThrowIfDisposed() {
                if (_table == null) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }
        }
    }
}
