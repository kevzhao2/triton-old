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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment. This is the entrypoint for embedding Lua into a CLR application.
    /// 
    /// <para/>
    /// 
    /// This class is <i>not</i> thread-safe.
    /// </summary>
    public sealed unsafe class LuaEnvironment : IDisposable
    {
        private readonly lua_State* _state;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);

            // Store a handle to the environment in the extra space portion of the Lua state. This enables us to
            // retrieve the environment associated with a Lua state, allowing the usage of static callbacks (and hence
            // unmanaged function pointers).
            //
            var handle = GCHandle.Alloc(this);
            *(IntPtr*)lua_getextraspace(_state) = GCHandle.ToIntPtr(handle);
        }

        /// <summary>
        /// Gets or sets the value of the global with the given name.
        /// </summary>
        /// <param name="name">The name of the global whose value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public LuaValue this[string name]
        {
            get
            {
                if (name is null)
                {
                    ThrowHelper.ThrowArgumentNullException(nameof(name));
                }

                ThrowIfDisposed();

                var type = lua_getglobal(_state, name);
                var value = LuaValue.From(_state, -1, type);
                lua_pop(_state, 1);  // pop value off of the stack
                return value;
            }

            set
            {
                if (name is null)
                {
                    ThrowHelper.ThrowArgumentNullException(nameof(name));
                }

                ThrowIfDisposed();

                value.Push(_state);
                lua_setglobal(_state, name);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // Cleanup must be done in a very specific order:
            // - The `GCHandle` allocated in the constructor must be retrieved prior to closing the state, as otherwise
            //   the handle would be lost.
            // - The state must then be closed, as doing so will call `__gc` metamethods which might depend on the
            //   handle remaining valid.
            //
            var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_getextraspace(_state));
            lua_close(_state);
            handle.Free();

            _isDisposed = true;
        }

        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                ThrowObjectDisposedException();
            }

            [DoesNotReturn]
            static void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(LuaEnvironment));
        }
    }
}
