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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment. This class is the entrypoint for embedding Lua into a CLR application.
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

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                // Cleanup must be done in a very specific order:
                // - We must retrieve the `GCHandle` in the extra space portion of the Lua state prior to closing the state.
                // - We must close the state prior to freeing the handle (since the `__gc` metamethods depend on it).
                //
                var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_getextraspace(_state));
                lua_close(_state);
                handle.Free();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Gets the value of the global with the given name.
        /// </summary>
        /// <param name="name">The name of the global to get.</param>
        /// <returns>The value of the global.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public LuaResult GetGlobal(string name)
        {
            if (name is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(name));
            }

            ThrowIfDisposed();

            // Reset the top of the stack so that the value will be at index 1.
            //
            lua_settop(_state, 0);

            _ = lua_getglobal(_state, name);
            return new(_state, 1);
        }

        /// <summary>
        /// Sets the value of the global with the given name.
        /// </summary>
        /// <param name="name">The name of the global to set.</param>
        /// <param name="value">The value to set the global to.</param>
        public void SetGlobal(string name, LuaArgument value)
        {
            if (name is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(name));
            }

            ThrowIfDisposed();

            value.Push(_state);
            lua_setglobal(_state, name);
        }

        /// <summary>
        /// Evaluates the given string.
        /// </summary>
        /// <param name="str">The string to evaluate.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">The string results in a Lua load error.</exception>
        /// <exception cref="LuaRuntimeException">The string results in a Lua runtime error.</exception>
        public LuaResults Eval(string str)
        {
            if (str is null)
                ThrowHelper.ThrowArgumentNullException(nameof(str));

            ThrowIfDisposed();

            // Reset the top of the stack so that the results will begin at index 1.
            //
            lua_settop(_state, 0);

            luaL_loadstring(_state, str);
            return lua_pcall(_state, 0, LUA_MULTRET);
        }

        /// <summary>
        /// Creates a new function from the given string.
        /// </summary>
        /// <param name="str">The string to create a function from.</param>
        /// <returns>The resulting function.</returns>
        /// <exception cref="LuaLoadException">The string results in a Lua load error.</exception>
        public LuaFunction CreateFunction(string str)
        {
            if (str is null)
                ThrowHelper.ThrowArgumentNullException(nameof(str));

            ThrowIfDisposed();

            luaL_loadstring(_state, str);
            var @ref = luaL_ref(_state, LUA_REGISTRYINDEX);
            return new(_state, @ref);
        }

        /// <summary>
        /// Creates a new thread.
        /// </summary>
        /// <returns>The resulting thread.</returns>
        public LuaThread CreateThread()
        {
            ThrowIfDisposed();

            var state = lua_newthread(_state);
            var @ref = luaL_ref(_state, LUA_REGISTRYINDEX);
            return new(state, @ref);
        }

        internal object ToClrObject(lua_State* state, int index)
        {
            throw new NotImplementedException();
        }

        internal IReadOnlyList<Type> ToClrTypes(lua_State* state, int index)
        {
            throw new NotImplementedException();
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaEnvironment));
        }
    }
}
