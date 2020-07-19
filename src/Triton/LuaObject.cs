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
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua object.
    /// </summary>
    public abstract class LuaObject : IDisposable
    {
        private protected readonly LuaEnvironment _environment;
        private protected readonly int _reference;
        private protected readonly IntPtr _state;

        private bool _isDisposed;

        private protected LuaObject(LuaEnvironment environment, int reference, IntPtr state)
        {
            Debug.Assert(environment != null);
            Debug.Assert(reference > LUA_RIDX_GLOBALS);
            Debug.Assert(state != IntPtr.Zero);

            _environment = environment;
            _reference = reference;
            _state = state;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                luaL_unref(_state, LUA_REGISTRYINDEX, _reference);
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Pushes the object onto the stack of the given Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        internal void Push(IntPtr state)
        {
            Debug.Assert(state != IntPtr.Zero);
            Debug.Assert(lua_checkstack(state, 1));

            if (state != _state)
            {
                lua_rawgeti(state, LUA_REGISTRYINDEX, LUA_RIDX_MAINTHREAD);

                try
                {
                    if (lua_topointer(state, -1) != _state)
                    {
                        throw new InvalidOperationException("Lua object cannot be pushed onto the given state");
                    }
                }
                finally
                {
                    lua_pop(state, 1);
                }
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, _reference);
        }

        private protected void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
