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
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua table.
    /// </summary>
    public sealed class LuaTable : LuaObject
    {
        internal LuaTable(LuaEnvironment environment, int reference, IntPtr state) :
            base(environment, reference, state)
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
                return GetterShared(lua_getfield(_state, -1, field));
            }

            set
            {
                if (field is null)
                {
                    throw new ArgumentNullException(nameof(field));
                }

                IndexerPrologue();  // Performs validation
                value.Push(_state);
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
                return GetterShared(lua_geti(_state, -1, index));
            }

            set
            {
                IndexerPrologue();  // Performs validation
                value.Push(_state);
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
                key.Push(_state);
                return GetterShared(lua_gettable(_state, -2));
            }

            set
            {
                IndexerPrologue();  // Performs validation
                key.Push(_state);
                value.Push(_state);
                lua_settable(_state, -3);
            }
        }

        private void IndexerPrologue()
        {
            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
        }

        private LuaValue GetterShared(LuaType type)
        {
            _environment.ToValue(_state, -1, out var value, type);
            return value;
        }
    }
}
