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
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua table, which consists of key-value elements and is used to represent structured data.
    /// </summary>
    public sealed unsafe class LuaTable : IDisposable
    {
        private readonly lua_State* _state;
        private readonly int _ref;

        private bool _isDisposed;

        internal unsafe LuaTable(lua_State* state, int @ref)
        {
            _state = state;
            _ref = @ref;
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

        internal void Push(lua_State* state)
        {
            if (*(GCHandle*)lua_getextraspace(state) != *(GCHandle*)lua_getextraspace(_state))
                ThrowHelper.ThrowInvalidOperationException("Table is not associated with the given environment");

            var type = lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
            Debug.Assert(type == LUA_TTABLE);
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaTable));
        }
    }
}
