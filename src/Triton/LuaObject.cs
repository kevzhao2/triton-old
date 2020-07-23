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
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua object.
    /// </summary>
    public abstract class LuaObject : IDisposable
    {
        private protected readonly IntPtr _state;
        private protected readonly LuaEnvironment _environment;
        private protected readonly int _reference;

        private bool _isDisposed;

        private protected LuaObject(IntPtr state, LuaEnvironment environment, int reference)
        {
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
        /// Pushes the Lua object onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        internal void Push(IntPtr state)
        {
            // Check if the environments match.
            if (Marshal.ReadIntPtr(lua_getextraspace(state)) != Marshal.ReadIntPtr(lua_getextraspace(_state)))
            {
                throw new InvalidOperationException("Lua object does not belong to the given state's environment");
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
