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
using System.Linq.Expressions;
#endif
using System.Linq;
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua table that may be read and modified.
    /// </summary>
    public sealed class LuaTable : LuaReference, IDictionary<object, object> {
#if NETSTANDARD || NET40
        private static readonly Dictionary<ExpressionType, string> BinaryOperations = new Dictionary<ExpressionType, string> {
            [ExpressionType.Add] = "__add",
            [ExpressionType.AddChecked] = "__add",
            [ExpressionType.Subtract] = "__sub",
            [ExpressionType.SubtractChecked] = "__sub",
            [ExpressionType.Multiply] = "__mul",
            [ExpressionType.MultiplyChecked] = "__mul",
            [ExpressionType.Divide] = "__div",
            [ExpressionType.Modulo] = "__mod",
            [ExpressionType.Power] = "__pow",
            [ExpressionType.And] = "__band",
            [ExpressionType.Or] = "__bor",
            [ExpressionType.ExclusiveOr] = "__bxor",
            [ExpressionType.RightShift] = "__shr",
            [ExpressionType.LeftShift] = "__shl",
            [ExpressionType.Equal] = "__eq",
            [ExpressionType.NotEqual] = "__eq",
            [ExpressionType.LessThan] = "__lt",
            [ExpressionType.GreaterThanOrEqual] = "__lt",
            [ExpressionType.LessThanOrEqual] = "__le",
            [ExpressionType.GreaterThan] = "__le",
        };

        // There are only the __eq, __lt, and __le metamethods. Here, we are relying on the assumption that metamethods lead to a total
        // ordering, which is almost always justified.
        private static readonly HashSet<ExpressionType> NegatedBinaryOperations = new HashSet<ExpressionType> {
            ExpressionType.NotEqual, ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual
        };

        private static readonly Dictionary<ExpressionType, string> UnaryOperations = new Dictionary<ExpressionType, string> {
            [ExpressionType.Negate] = "__unm",
            [ExpressionType.NegateChecked] = "__unm",
            [ExpressionType.Not] = "__bnot"
        };
#endif

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
        /// Gets or sets the metatable.
        /// </summary>
        /// <value>The metatable, or <c>null</c> if there is none.</value>
        public LuaTable Metatable {
            get {
                PushOnto(Lua.MainState);
                if (!LuaApi.GetMetatable(Lua.MainState, -1)) {
                    LuaApi.Pop(Lua.MainState, 1);
                    return null;
                }

                var result = (LuaTable)Lua.ToObject(-1, LuaType.Table);
                LuaApi.Pop(Lua.MainState, 2);
                return result;
            }
            set {
                PushOnto(Lua.MainState);
                if (value == null) {
                    LuaApi.PushNil(Lua.MainState);
                } else {
                    value.PushOnto(Lua.MainState);
                }
                LuaApi.SetMetatable(Lua.MainState, -2);
                LuaApi.Pop(Lua.MainState, 1);
            }
        }

        /// <summary>
        /// Gets the collection of keys.
        /// </summary>
        /// <value>The collection of keys.</value>
        public ICollection<object> Keys => new KeyCollection(this);

        /// <summary>
        /// Gets the collection of values.
        /// </summary>
        /// <value>The collection of values.</value>
        public ICollection<object> Values => new ValueCollection(this);

        /// <summary>
        /// Gets or sets the value corresponding to the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is a <see cref="LuaReference"/> which is tied to a different <see cref="Lua"/> environment.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c> and is set.</exception>
        public object this[object key] {
            get {
                PushOnto(Lua.MainState);
                Lua.PushObject(key);
                var type = LuaApi.GetTable(Lua.MainState, -2);
                var result = Lua.ToObject(-1, type);
                LuaApi.Pop(Lua.MainState, 2);
                return result;
            }
            set {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }

                PushOnto(Lua.MainState);
                Lua.PushObject(key);
                Lua.PushObject(value);
                LuaApi.SetTable(Lua.MainState, -3);
                LuaApi.Pop(Lua.MainState, 1);
            }
        }

        bool ICollection<KeyValuePair<object, object>>.IsReadOnly => false;

        /// <summary>
        /// Adds the given key-value pair.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="key"/> exists already.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is <c>null</c>.</exception>
        public void Add(object key, object value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (ContainsKey(key)) {
                throw new ArgumentException("Key exists already.", nameof(key));
            }

            this[key] = value;
        }

        /// <summary>
        /// Clears all of the key-value pairs.
        /// </summary>
        public void Clear() {
            PushOnto(Lua.MainState);
            LuaApi.PushNil(Lua.MainState);
            
            while (LuaApi.Next(Lua.MainState, -2)) {
                LuaApi.Pop(Lua.MainState, 1);
                LuaApi.PushValue(Lua.MainState, -1);
                LuaApi.PushNil(Lua.MainState);
                LuaApi.SetTable(Lua.MainState, -4);
            }
            LuaApi.Pop(Lua.MainState, 1);
        }

        /// <summary>
        /// Determines whether the given key exists.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if <paramref name="key"/> exists, <c>false</c> otherwise.</returns>
        public bool ContainsKey(object key) {
            if (key == null) {
                return false;
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
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() {
            object currentKey = null;
            while (true) {
                PushOnto(Lua.MainState);
                Lua.PushObject(currentKey);
                if (!LuaApi.Next(Lua.MainState, -2)) {
                    LuaApi.Pop(Lua.MainState, 1);
                    yield break;
                }

                currentKey = Lua.ToObject(-2);
                var value = Lua.ToObject(-1);
                LuaApi.Pop(Lua.MainState, 3);
                yield return new KeyValuePair<object, object>(currentKey, value);
            }
        }

        /// <summary>
        /// Removes the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if <paramref name="key"/> existed, <c>false</c> otherwise.</returns>
        public bool Remove(object key) {
            if (!ContainsKey(key)) {
                return false;
            }

            this[key] = null;
            return true;
        }

        /// <summary>
        /// Tries to get the value associated with the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if <paramref name="key"/> exists, <c>false</c> otherwise.</returns>
        public bool TryGetValue(object key, out object value) {
            value = this[key];
            return value != null;
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result) {
            var operation = binder.Operation;
            if (!BinaryOperations.TryGetValue(operation, out var metamethod)) {
                result = null;
                return false;
            }

            var metatable = Metatable;
            if (metatable == null || !(metatable[metamethod] is LuaFunction metafunction)) {
                result = null;
                return false;
            }

            result = metafunction.Call(this, arg)[0];
            if (NegatedBinaryOperations.Contains(operation) && result is bool b) {
                result = !b;
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = this[binder.Name];
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            this[binder.Name] = value;
            return true;
        }

        /// <inheritdoc/>
        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result) {
            result = null;
            if (!UnaryOperations.TryGetValue(binder.Operation, out var metamethod)) {
                result = null;
                return false;
            }

            var metatable = Metatable;
            if (metatable == null || !(metatable[metamethod] is LuaFunction metafunction)) {
                result = null;
                return false;
            }

            result = metafunction.Call(this)[0];
            return true;
        }
#endif

        void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item) {
            return item.Value != null && this[item.Key]?.Equals(item.Value) == true;
        }

        void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) {
            foreach (var kvp in this) {
                array[arrayIndex++] = kvp;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) {
            return item.Value != null && this[item.Key]?.Equals(item.Value) == true && Remove(item.Key);
        }

        private sealed class KeyCollection : ICollection<object> {
            private readonly LuaTable _table;

            public KeyCollection(LuaTable table) => _table = table;

            public int Count => _table.Count;
            public bool IsReadOnly => true;

            public void Add(object item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(object item) => _table.ContainsKey(item);

            public void CopyTo(object[] array, int arrayIndex) {
                foreach (var key in this) {
                    array[arrayIndex++] = key;
                }
            }

            public IEnumerator<object> GetEnumerator() => _table.Select(kvp => kvp.Key).GetEnumerator();
            public bool Remove(object item) => throw new NotSupportedException();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ValueCollection : ICollection<object> {
            private readonly LuaTable _table;

            public ValueCollection(LuaTable table) => _table = table;

            public int Count => _table.Count;
            public bool IsReadOnly => true;

            public void Add(object item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(object item) => _table.Any(kvp => kvp.Value.Equals(item));

            public void CopyTo(object[] array, int arrayIndex) {
                foreach (var value in this) {
                    array[arrayIndex++] = value;
                }
            }

            public IEnumerator<object> GetEnumerator() => _table.Select(kvp => kvp.Value).GetEnumerator();
            public bool Remove(object item) => throw new NotSupportedException();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
