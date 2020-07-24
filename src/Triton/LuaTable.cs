﻿// Copyright (c) 2020 Kevin Zhao
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
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua table.
    /// </summary>
    public sealed class LuaTable : LuaObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaTable"/> class with the specified Lua
        /// <paramref name="state"/>, <paramref name="environment"/>, and <paramref name="reference"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="environment">The Lua environment.</param>
        /// <param name="reference">The reference.</param>
        internal LuaTable(IntPtr state, LuaEnvironment environment, int reference) :
            base(state, environment, reference)
        {
        }

        /// <summary>
        /// Gets or sets the value of the given <paramref name="field"/>.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>The value of the given <paramref name="field"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The Lua table is disposed.</exception>
        public LuaValue this[string field]
        {
            get
            {
                if (field is null)
                {
                    throw new ArgumentNullException(nameof(field));
                }

                IndexerPrologue();  // Performs validation
                return GetShared(lua_getfield(_state, -1, field));
            }

            set
            {
                if (field is null)
                {
                    throw new ArgumentNullException(nameof(field));
                }

                IndexerPrologue();  // Performs validation
                _environment.PushValue(_state, value);
                lua_setfield(_state, -2, field);
            }
        }

        /// <summary>
        /// Gets or sets the value of the given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The value of the given <paramref name="index"/>.</returns>
        /// <exception cref="ObjectDisposedException">The Lua table is disposed.</exception>
        public LuaValue this[long index]
        {
            get
            {
                IndexerPrologue();  // Performs validation
                return GetShared(lua_geti(_state, -1, index));
            }

            set
            {
                IndexerPrologue();  // Performs validation
                _environment.PushValue(_state, value);
                lua_seti(_state, -2, index);
            }
        }

        /// <summary>
        /// Gets or sets the value of the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value of the given <paramref name="key"/>.</returns>
        /// <exception cref="ObjectDisposedException">The Lua table is disposed.</exception>
        public LuaValue this[in LuaValue key]
        {
            get
            {
                IndexerPrologue();  // Performs validation
                _environment.PushValue(_state, key);
                return GetShared(lua_gettable(_state, -2));
            }

            set
            {
                IndexerPrologue();  // Performs validation
                _environment.PushValue(_state, key);
                _environment.PushValue(_state, value);
                lua_settable(_state, -3);
            }
        }

        private void IndexerPrologue()
        {
            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
        }

        private LuaValue GetShared(LuaType type)
        {
            _environment.ToValue(_state, -1, out var value, type);
            return value;
        }
    }
}
