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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua table, an object which consists of key-value entries.
    /// </summary>
    /// <remarks>
    /// Instances of this class store the tables in the Lua registry in order to reference them. This means that an
    /// instance is associated with its environment, and <i>must</i> be disposed of to prevent a Lua memory leak.
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(DebuggerView))]
    public sealed unsafe class LuaTable : IDisposable
    {
        /// <summary>
        /// Represents a key-value entry in a <see cref="LuaTable"/>.
        /// </summary>
        public readonly ref struct Entry
        {
            private readonly lua_State* _state;

            internal Entry(lua_State* state)
            {
                _state = state;
            }

            /// <summary>
            /// Deconstructs the entry.
            /// </summary>
            /// <param name="key">The resulting key.</param>
            /// <param name="value">The resulting value.</param>
            public void Deconstruct(out LuaResult key, out LuaResult value)
            {
                var state = _state;  // local optimization

                key = new(state, 2);
                value = new(state, 3);
            }
        }

        /// <summary>
        /// Provides an enumerator for the entries in a <see cref="LuaTable"/>.
        /// </summary>
        public readonly ref struct Enumerator
        {
            private readonly lua_State* _state;
            private readonly int _tableRef;
            private readonly int _keyRef;

            internal Enumerator(lua_State* state, int tableRef)
            {
                _state = state;
                _tableRef = tableRef;

                // Allocate a reference to temporarily store the key during table enumeration (since the stack could get
                // trashed). This is done using a dummy value as nil would just result in `LUA_REFNIL`.
                //
                lua_pushinteger(state, 0);
                var keyRef = luaL_ref(state, LUA_REGISTRYINDEX);
                lua_pushnil(state);
                lua_rawseti(state, LUA_REGISTRYINDEX, keyRef);

                _keyRef = keyRef;
            }

            /// <summary>
            /// Gets the entry at the current position of the enumerator.
            /// </summary>
            public Entry Current => new(_state);

            /// <summary>
            /// Advances the enumerator to the next position.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator successfully advanced to the next position; otherwise, <see langword="false"/>.</returns>
            public bool MoveNext()
            {
                var state = _state;        // local optimization
                var tableRef = _tableRef;  // local optimization
                var keyRef = _keyRef;      // local optimization

                lua_settop(state, 0);  // ensure that table, key, and value will be at indices 1, 2, and 3, respectively

                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, tableRef);
                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, keyRef);
                if (lua_next(state, 1))
                {
                    // Save the key since the stack could get trashed.
                    //
                    lua_pushvalue(state, 2);
                    lua_rawseti(state, LUA_REGISTRYINDEX, keyRef);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Disposes the enumerator.
            /// </summary>
            public void Dispose()
            {
                luaL_unref(_state, LUA_REGISTRYINDEX, _keyRef);
            }
        }

        /// <summary>
        /// Provides a view into a <see cref="LuaTable"/> for the debugger.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private sealed class DebuggerView
        {
            private readonly LuaTable _table;

            internal DebuggerView(LuaTable table)
            {
                _table = table;
            }

            /// <summary>
            /// Gets the table's metatable.
            /// </summary>
            public LuaTable? Metatable => _table.Metatable;

            /// <summary>
            /// Gets the table's items.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<string, string>[] Items
            {
                get
                {
                    var result = new List<KeyValuePair<string, string>>();
                    foreach (var (key, value) in _table)
                        result.Add(new(key.ToDebugString(), value.ToDebugString()));

                    return result.ToArray();
                }
            }
        }

        private readonly lua_State* _state;
        private readonly int _ref;

        private bool _isDisposed;

        internal LuaTable(lua_State* state, int @ref)
        {
            _state = state;
            _ref = @ref;
        }

        /// <summary>
        /// Gets the number of elements contained in the table.
        /// </summary>
        public int Count
        {
            get
            {
                ThrowIfDisposed();

                var state = _state;  // local optimization
                var @ref = _ref;     // local optimization

                var count = 0;

                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
                lua_pushnil(state);
                while (lua_next(state, -2))
                {
                    lua_pop(state, 1);  // pop the value off of the stack

                    ++count;
                }

                lua_pop(state, 1);  // pop the table off of the stack
                return count;
            }
        }

        /// <summary>
        /// Gets or sets the table's metatable. A value of <see langword="null"/> indicates a lack of a metatable.
        /// </summary>
        public LuaTable? Metatable
        {
            get
            {
                ThrowIfDisposed();

                var state = _state;  // local optimization
                var @ref = _ref;     // local optimization

                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
                var metatable = lua_getmetatable(state, -1) ?
                    new LuaTable(state, luaL_ref(state, LUA_REGISTRYINDEX)) :
                    null;

                lua_pop(state, 1);  // pop the table off of the stack
                return metatable;
            }

            set
            {
                ThrowIfDisposed();

                var state = _state;  // local optimization
                var @ref = _ref;     // local optimization

                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
                if (value is null)
                    lua_pushnil(state);
                else
                    value.Push(state);
                lua_setmetatable(state, -2);

                lua_pop(state, 1);  // pop the table off of the stack
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                luaL_unref(_state, LUA_REGISTRYINDEX, _ref);

                _isDisposed = true;
            }
        }

        #region GetValue overloads

        /// <inheritdoc cref="GetValue(in LuaArgument)"/>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public LuaResult GetValue(string key)
        {
            if (key is null)
                ThrowHelper.ThrowArgumentNullException(nameof(key));

            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            lua_settop(state, 0);  // ensure that the table and result will be at indices 1 and 2, respectively

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            _ = lua_getfield(state, 1, key);
            return new(state, 2);
        }

        /// <inheritdoc cref="GetValue(in LuaArgument)"/>
        public LuaResult GetValue(long key)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            lua_settop(state, 0);  // ensure that the table and result will be at indices 1 and 2, respectively

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            _ = lua_geti(state, 1, key);
            return new(state, 2);
        }

        /// <summary>
        /// Gets the value of the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element whose value to get.</param>
        /// <returns>The value of the element with the specified key.</returns>
        public LuaResult GetValue(in LuaArgument key)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            lua_settop(state, 0);  // ensure that the table and result will be at indices 1 and 2, respectively

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            key.Push(state);
            _ = lua_gettable(state, 1);
            return new(state, 2);
        }

        #endregion

        #region SetValue overloads

        /// <inheritdoc cref="SetValue(in LuaArgument, in LuaArgument)"/>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public void SetValue(string key, in LuaArgument value)
        {
            if (key is null)
                ThrowHelper.ThrowArgumentNullException(nameof(key));

            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);

            value.Push(state);
            lua_setfield(state, -2, key);

            lua_pop(state, 1);  // pop the table off of the stack
        }

        /// <inheritdoc cref="SetValue(in LuaArgument, in LuaArgument)"/>
        public void SetValue(long key, in LuaArgument value)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);

            value.Push(state);
            lua_seti(state, -2, key);

            lua_pop(state, 1);  // pop the table off of the stack
        }

        /// <summary>
        /// Sets the value of the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element whose value to set.</param>
        /// <param name="value">The value.</param>
        public void SetValue(in LuaArgument key, in LuaArgument value)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);

            key.Push(state);
            value.Push(state);
            lua_settable(state, -3);

            lua_pop(state, 1);  // pop the table off of the stack
        }

        #endregion

        #region Add overloads

        /// <inheritdoc cref="Add(in LuaArgument, in LuaArgument)"/>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public void Add(string key, in LuaArgument value)
        {
            if (key is null)
                ThrowHelper.ThrowArgumentNullException(nameof(key));

            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            var type = lua_getfield(state, -1, key);
            if (type != LUA_TNIL)
            {
                lua_pop(state, 2);  // pop the table and value off of the stack

                ThrowHelper.ThrowArgumentException(nameof(key), "An element with the same key has already been added");
            }

            value.Push(state);
            lua_setfield(state, -3, key);

            lua_pop(state, 2);  // pop the table and value off of the stack
        }

        /// <inheritdoc cref="Add(in LuaArgument, in LuaArgument)"/>
        public void Add(long key, in LuaArgument value)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            var type = lua_geti(state, -1, key);
            if (type != LUA_TNIL)
            {
                lua_pop(state, 2);  // pop the table and value off of the stack

                ThrowHelper.ThrowArgumentException(nameof(key), "An element with the same key has already been added");
            }

            value.Push(state);
            lua_seti(state, -3, key);

            lua_pop(state, 2);  // pop the table and value off of the stack
        }

        /// <summary>
        /// Adds the specified key-value element to the table.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentException">An element with the same key has already been added to the table.</exception>
        public void Add(in LuaArgument key, in LuaArgument value)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            key.Push(state);
            var type = lua_gettable(state, -2);
            if (type != LUA_TNIL)
            {
                lua_pop(state, 2);  // pop the table and value off of the stack

                ThrowHelper.ThrowArgumentException(nameof(key), "An element with the same key has already been added");
            }

            key.Push(state);
            value.Push(state);
            lua_settable(state, -4);

            lua_pop(state, 2);  // pop the table and value off of the stack
        }

        #endregion

        /// <summary>
        /// Removes all elements from the table.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            lua_pushnil(state);
            while (lua_next(state, -2))
            {
                lua_pop(state, 1);

                lua_pushvalue(state, -1);
                lua_pushnil(state);
                lua_settable(state, -4);
            }
        }

        #region ContainsKey overloads

        /// <inheritdoc cref="ContainsKey(in LuaArgument)"/>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(string key)
        {
            if (key is null)
                ThrowHelper.ThrowArgumentNullException(nameof(key));

            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            var type = lua_getfield(state, -1, key);

            lua_pop(state, 2);  // pop the table and value off of the stack
            return type != LUA_TNIL;
        }

        /// <inheritdoc cref="ContainsKey(in LuaArgument)"/>
        public bool ContainsKey(long key)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            var type = lua_geti(state, -1, key);

            lua_pop(state, 2);  // pop the table and value off of the stack
            return type != LUA_TNIL;
        }

        /// <summary>
        /// Determines whether the table contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to find in the table.</param>
        /// <returns><see langword="true"/> if the table contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
        public bool ContainsKey(in LuaArgument key)
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            key.Push(state);
            var type = lua_gettable(state, -2);

            lua_pop(state, 2);  // pop the table and value off of the stack
            return type != LUA_TNIL;
        }

        #endregion

        /// <summary>
        /// Returns an enumerator for the table.
        /// </summary>
        /// <returns>An enumerator for the table.</returns>
        public Enumerator GetEnumerator() => new(_state, _ref);

        #region Remove overloads

        /// <inheritdoc cref="Remove(in LuaArgument)"/>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Remove(in LuaArgument, out LuaResult)"/>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool Remove(string key, out LuaResult value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Remove(in LuaArgument)"/>
        public bool Remove(long key)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Remove(in LuaArgument, out LuaResult)"/>
        public bool Remove(long key, out LuaResult value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the element with the specified key from the table.
        /// </summary>
        /// <param name="key">The key whose element to remove.</param>
        /// <returns><see langword="true"/> if the key is successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool Remove(in LuaArgument key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the element with the specified key from the table.
        /// </summary>
        /// <param name="key">The key whose element to remove.</param>
        /// <param name="value">The value of the element.</param>
        /// <returns><see langword="true"/> if the key is successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool Remove(in LuaArgument key, out LuaResult value)
        {
            throw new NotImplementedException();
        }

        #endregion

        [ExcludeFromCodeCoverage]
        internal void Push(lua_State* state)
        {
            if (*(GCHandle*)lua_getextraspace(state) != *(GCHandle*)lua_getextraspace(_state))
                ThrowHelper.ThrowInvalidOperationException("Table is not associated with this environment");

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
        }

        [ExcludeFromCodeCoverage]
        internal string ToDebugString()
        {
            var state = _state;  // local optimization
            var @ref = _ref;     // local optimization

            _ = lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
            var ptr = lua_topointer(state, -1);
            lua_pop(state, 1);  // pop the table off of the stack

            return $"table: 0x{Convert.ToString((long)ptr, 16)}";
        }

        [DebuggerStepThrough]
        [ExcludeFromCodeCoverage]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaTable));
        }
    }
}
