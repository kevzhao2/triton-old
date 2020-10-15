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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Triton.Lua;
using static Triton.Lua.LuaType;

namespace Triton
{
    /// <summary>
    /// Represents a Lua table. A table is a Lua object which consists of key-value elements and is used to represent
    /// structured data.
    /// </summary>
    public sealed unsafe class LuaTable : LuaObject, IDictionary<LuaValue, LuaValue>
    {
        private KeyCollection? _keys;
        private ValueCollection? _values;

        internal LuaTable(lua_State* state, LuaEnvironment environment, int @ref) : base(state, environment, @ref)
        {
        }

        /// <summary>
        /// Gets the number of elements contained in the table.
        /// </summary>
        /// <value>The number of elements contained in the table.</value>
        public int Count
        {
            get
            {
                PushSelf();  // Performs validation

                var count = 0;

                lua_pushnil(_state);
                while (lua_next(_state, -2))
                {
                    ++count;

                    lua_pop(_state, 1);
                }

                return count;
            }
        }

        /// <summary>
        /// Gets a collection of the table's keys.
        /// </summary>
        /// <value>A collection of the table's keys.</value>
        public ICollection<LuaValue> Keys => _keys ??= new(_state, this);

        /// <summary>
        /// Gets a collection of the table's values.
        /// </summary>
        /// <value>A collection of the table's values.</value>
        public ICollection<LuaValue> Values => _values ??= new(_state, this);

        /// <summary>
        /// Gets or sets the table's metatable.
        /// </summary>
        /// <value>The table's metatable.</value>
        public LuaTable? Metatable
        {
            get
            {
                PushSelf();  // Performs validation

                return lua_getmetatable(_state, -1) ?
                    (LuaTable)_environment.LoadLuaObject(_state, -1, LUA_TTABLE) : null;
            }

            set
            {
                PushSelf();  // Performs validation

                if (value is null)
                {
                    lua_pushnil(_state);
                }
                else
                {
                    value.Push(_state);
                }
                lua_setmetatable(_state, -2);
            }
        }
        
        bool ICollection<KeyValuePair<LuaValue, LuaValue>>.IsReadOnly => false;

        /// <summary>
        /// Gets or sets the value of the element with the given key.
        /// </summary>
        /// <param name="key">The key of the element whose value to get or set.</param>
        /// <value>The value of the element.</value>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">
        /// The property is retrieved and <paramref name="key"/> does not exist in the table.
        /// </exception>
        public LuaValue this[string key]
        {
            get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

            set
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                PushSelf();  // Performs validation

                value.Push(_state);
                lua_setfield(_state, -2, key);
            }
        }

        /// <summary>
        /// Gets or sets the value of the element with the given key.
        /// </summary>
        /// <param name="key">The key of the element whose value to get or set.</param>
        /// <value>The value of the element.</value>
        /// <exception cref="KeyNotFoundException">
        /// The property is retrieved and <paramref name="key"/> does not exist in the table.
        /// </exception>
        public LuaValue this[long key]
        {
            get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

            set
            {
                PushSelf();  // Performs validation

                value.Push(_state);
                lua_seti(_state, -2, key);
            }
        }

        /// <summary>
        /// Gets or sets the value of the element with the given key.
        /// </summary>
        /// <param name="key">The key of the element whose value to get or set.</param>
        /// <value>The value of the element.</value>
        /// <exception cref="KeyNotFoundException">
        /// The property is retrieved and <paramref name="key"/> does not exist in the table.
        /// </exception>
        public LuaValue this[in LuaValue key]
        {
            get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

            set
            {
                PushSelf();  // Performs validation

                key.Push(_state);
                value.Push(_state);
                lua_settable(_state, -3);
            }
        }

        LuaValue IDictionary<LuaValue, LuaValue>.this[LuaValue key]
        {
            get => this[key];
            set => this[key] = value;
        }

        /// <summary>
        /// Adds an element to the table.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the table.</exception>
        public void Add(string key, in LuaValue value)
        {
            if (ContainsKey(key))  // Performs validation
            {
                throw new ArgumentException("An element with the same key already exists in the table");
            }

            value.Push(_state);
            lua_setfield(_state, -3, key);
        }

        /// <summary>
        /// Adds an element to the table.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in the table.</exception>
        public void Add(long key, in LuaValue value)
        {
            if (ContainsKey(key))  // Performs validation
            {
                throw new ArgumentException("An element with the same key already exists in the table");
            }

            value.Push(_state);
            lua_seti(_state, -3, key);
        }

        /// <summary>
        /// Adds an element to the table.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in the table.</exception>
        public void Add(in LuaValue key, in LuaValue value)
        {
            if (ContainsKey(key))  // Performs validation
            {
                throw new ArgumentException("An element with the same key already exists in the table");
            }

            key.Push(_state);
            value.Push(_state);
            lua_settable(_state, -4);
        }

        /// <summary>
        /// Removes all elements from the table.
        /// </summary>
        public void Clear()
        {
            PushSelf();  // Performs validation

            lua_pushnil(_state);
            while (lua_next(_state, -2))
            {
                lua_pop(_state, 1);

                lua_pushvalue(_state, -1);
                lua_pushnil(_state);
                lua_settable(_state, -4);
            }
        }

        /// <summary>
        /// Determines whether the table contains an element with the given key.
        /// </summary>
        /// <param name="key">The key of the element to find.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given key; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(string key) => PushValue(key) != LUA_TNIL;  // Performs validation

        /// <summary>
        /// Determines whether the table contains an element with the given key.
        /// </summary>
        /// <param name="key">The key of the element to find.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given key; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsKey(long key) => PushValue(key) != LUA_TNIL;  // Performs validation

        /// <summary>
        /// Determines whether the table contains an element with the given key.
        /// </summary>
        /// <param name="key">The key of the element to find.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given key; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsKey(in LuaValue key) => PushValue(key) != LUA_TNIL;  // Performs validation

        /// <summary>
        /// Determines whether the table contains an element with the given value.
        /// </summary>
        /// <param name="value">The value of the element to find.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given value; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsValue(in LuaValue value)
        {
            PushSelf();  // Performs validation

            lua_pushnil(_state);
            while (lua_next(_state, -2))
            {
                LuaValue.FromLua(_state, -1, lua_type(_state, -1), out var candidate);
                if (candidate == value)
                {
                    return true;
                }

                lua_pop(_state, 1);
            }

            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the table's elements.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the table's elements.</returns>
        public IEnumerator<KeyValuePair<LuaValue, LuaValue>> GetEnumerator() => new Enumerator(_state, this);

        /// <summary>
        /// Removes the element with the given key from the table.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the element is successfully removed from the table; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool Remove(string key)
        {
            if (!ContainsKey(key))  // Performs validation
            {
                return false;
            }

            lua_pushnil(_state);
            lua_setfield(_state, -3, key);
            return true;
        }

        /// <summary>
        /// Removes the element with the given key from the table.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the element is successfully removed from the table; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Remove(long key)
        {
            if (!ContainsKey(key))  // Performs validation
            {
                return false;
            }

            lua_pushnil(_state);
            lua_seti(_state, -3, key);
            return true;
        }

        /// <summary>
        /// Removes the element with the given key from the table.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the element is successfully removed from the table; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Remove(in LuaValue key)
        {
            if (!ContainsKey(key))  // Performs validation
            {
                return false;
            }

            key.Push(_state);
            lua_pushnil(_state);
            lua_settable(_state, -4);
            return true;
        }

        /// <summary>
        /// Tries to get the value of the element with the given key in the table.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given key; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool TryGetValue(string key, out LuaValue value)
        {
            var type = PushValue(key);  // Performs validation
            if (type == LUA_TNIL)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            LuaValue.FromLua(_state, -1, type, out value);
            return true;
        }

        /// <summary>
        /// Tries to get the value of the element with the given key in the table.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given key; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(long key, out LuaValue value)
        {
            var type = PushValue(key);  // Performs validation
            if (type == LUA_TNIL)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            LuaValue.FromLua(_state, -1, type, out value);
            return true;
        }

        /// <summary>
        /// Tries to get the value of the element with the given key in the table.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns>
        /// <see langword="true"/> if the table contains an element with the given key; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(in LuaValue key, out LuaValue value)
        {
            var type = PushValue(key);  // Performs validation
            if (type == LUA_TNIL)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            LuaValue.FromLua(_state, -1, type, out value);
            return true;
        }

        private void PushSelf()
        {
            _environment.ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _ref);
        }

        private LuaType PushValue(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            PushSelf();

            return lua_getfield(_state, -1, key);
        }

        private LuaType PushValue(long key)
        {
            PushSelf();

            return lua_geti(_state, -1, key);
        }

        private LuaType PushValue(in LuaValue key)
        {
            PushSelf();

            key.Push(_state);
            return lua_gettable(_state, -2);
        }

        void IDictionary<LuaValue, LuaValue>.Add(LuaValue key, LuaValue value) => Add(key, value);

        bool IDictionary<LuaValue, LuaValue>.ContainsKey(LuaValue key) => ContainsKey(key);

        bool IDictionary<LuaValue, LuaValue>.Remove(LuaValue key) => Remove(key);

        bool IDictionary<LuaValue, LuaValue>.TryGetValue(LuaValue key, out LuaValue value) => TryGetValue(key, out value);

        void ICollection<KeyValuePair<LuaValue, LuaValue>>.Add(KeyValuePair<LuaValue, LuaValue> item) =>
            Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<LuaValue, LuaValue>>.Contains(KeyValuePair<LuaValue, LuaValue> item) =>
            TryGetValue(item.Key, out var value) && value == item.Value;

        void ICollection<KeyValuePair<LuaValue, LuaValue>>.CopyTo(KeyValuePair<LuaValue, LuaValue>[] array, int arrayIndex)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            PushSelf();  // Performs validation

            lua_pushnil(_state);
            while (lua_next(_state, -2))
            {
                LuaValue.FromLua(_state, -2, lua_type(_state, -2), out var key);
                LuaValue.FromLua(_state, -1, lua_type(_state, -1), out var value);
                array[arrayIndex++] = new(key, value);

                lua_pop(_state, 1);
            }
        }

        bool ICollection<KeyValuePair<LuaValue, LuaValue>>.Remove(KeyValuePair<LuaValue, LuaValue> item) =>
            ((IDictionary<LuaValue, LuaValue>)this).Contains(item) && Remove(item.Key);

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class KeyCollection : ICollection<LuaValue>
        {
            private readonly lua_State* _state;
            private readonly LuaTable _table;

            internal KeyCollection(lua_State* state, LuaTable table)
            {
                _state = state;
                _table = table;
            }

            public int Count => _table.Count;

            bool ICollection<LuaValue>.IsReadOnly => true;

            public bool Contains(LuaValue item) => _table.ContainsKey(item);

            public void CopyTo(LuaValue[] array, int arrayIndex)
            {
                if (array is null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                _table.PushSelf();  // Performs validation

                lua_pushnil(_state);
                while (lua_next(_state, -2))
                {
                    LuaValue.FromLua(_state, -2, lua_type(_state, -2), out array[arrayIndex++]);

                    lua_pop(_state, 1);
                }
            }

            public IEnumerator<LuaValue> GetEnumerator() => new Enumerator(_state, _table);

            [ExcludeFromCodeCoverage]
            void ICollection<LuaValue>.Add(LuaValue item) => throw new NotSupportedException();

            [ExcludeFromCodeCoverage]
            void ICollection<LuaValue>.Clear() => throw new NotSupportedException();

            [ExcludeFromCodeCoverage]
            bool ICollection<LuaValue>.Remove(LuaValue item) => throw new NotSupportedException();

            [ExcludeFromCodeCoverage]
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<LuaValue>
            {
                private readonly lua_State* _state;
                private readonly LuaTable _table;

                private LuaValue _currentKey;

                internal Enumerator(lua_State* state, LuaTable table)
                {
                    _state = state;
                    _table = table;
                }

                public LuaValue Current => _currentKey;

                [ExcludeFromCodeCoverage]
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    _table.PushSelf();  // Performs validation

                    _currentKey.Push(_state);
                    if (!lua_next(_state, -2))
                    {
                        return false;
                    }

                    LuaValue.FromLua(_state, -2, lua_type(_state, -2), out _currentKey);
                    return true;
                }

                public void Reset()
                {
                    _currentKey = LuaValue.Nil;
                }
            }
        }

        private sealed class ValueCollection : ICollection<LuaValue>
        {
            private readonly lua_State* _state;
            private readonly LuaTable _table;

            internal ValueCollection(lua_State* state, LuaTable table)
            {
                _state = state;
                _table = table;
            }

            public int Count => _table.Count;
            
            bool ICollection<LuaValue>.IsReadOnly => true;

            public bool Contains(LuaValue item) => _table.ContainsValue(item);

            public void CopyTo(LuaValue[] array, int arrayIndex)
            {
                if (array is null)
                {
                    throw new ArgumentNullException(nameof(array));
                }

                _table.PushSelf();  // Performs validation

                lua_pushnil(_state);
                while (lua_next(_state, -2))
                {
                    LuaValue.FromLua(_state, -1, lua_type(_state, -1), out array[arrayIndex++]);

                    lua_pop(_state, 1);
                }
            }

            public IEnumerator<LuaValue> GetEnumerator() => new Enumerator(_state, _table);

            [ExcludeFromCodeCoverage]
            void ICollection<LuaValue>.Add(LuaValue item) => throw new NotSupportedException();

            [ExcludeFromCodeCoverage]
            void ICollection<LuaValue>.Clear() => throw new NotSupportedException();

            [ExcludeFromCodeCoverage]
            bool ICollection<LuaValue>.Remove(LuaValue item) => throw new NotSupportedException();

            [ExcludeFromCodeCoverage]
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<LuaValue>
            {
                private readonly lua_State* _state;
                private readonly LuaTable _table;

                private LuaValue _currentKey;
                private LuaValue _currentValue;

                internal Enumerator(lua_State* state, LuaTable table)
                {
                    _state = state;
                    _table = table;
                }

                public LuaValue Current => _currentValue;

                [ExcludeFromCodeCoverage]
                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    _table.PushSelf();  // Performs validation

                    _currentKey.Push(_state);
                    if (!lua_next(_state, -2))
                    {
                        return false;
                    }

                    LuaValue.FromLua(_state, -2, lua_type(_state, -2), out _currentKey);
                    LuaValue.FromLua(_state, -1, lua_type(_state, -1), out _currentValue);
                    return true;
                }

                public void Reset()
                {
                    _currentKey = LuaValue.Nil;
                }
            }
        }

        private sealed class Enumerator : IEnumerator<KeyValuePair<LuaValue, LuaValue>>
        {
            private readonly lua_State* _state;
            private readonly LuaTable _table;

            private LuaValue _currentKey;
            private LuaValue _currentValue;

            internal Enumerator(lua_State* state, LuaTable table)
            {
                _state = state;
                _table = table;
            }

            public KeyValuePair<LuaValue, LuaValue> Current => new(_currentKey, _currentValue);

            [ExcludeFromCodeCoverage]
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _table.PushSelf();  // Performs validation

                _currentKey.Push(_state);
                if (!lua_next(_state, -2))
                {
                    return false;
                }

                LuaValue.FromLua(_state, -2, lua_type(_state, -2), out _currentKey);
                LuaValue.FromLua(_state, -1, lua_type(_state, -1), out _currentValue);
                return true;
            }

            public void Reset()
            {
                _currentKey = LuaValue.Nil;
            }
        }
    }
}
