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
using static Triton.Native.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua table.
    /// </summary>
    public sealed unsafe class LuaTable : LuaObject
    {
        internal LuaTable(LuaEnvironment environment, int reference, lua_State* state) :
            base(environment, reference, state)
        {
        }

        /// <summary>
        /// Gets or sets the value of the field with name <paramref name="s"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The value of the field with name <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object? this[string s]
        {
            get
            {
                if (s is null)
                {
                    throw new ArgumentNullException(nameof(s));
                }

                _environment.ThrowIfDisposed();
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);  // 2 stack slots required

                var stackDelta = 0;

                try
                {
                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    ++stackDelta;

                    using var buffer = _environment.MarshalString(s, isNullTerminated: true);
                    var type = lua_getfield(_state, -1, buffer.Pointer);
                    ++stackDelta;

                    return _environment.ToObject(_state, -1, typeHint: type);
                }
                finally
                {
                    lua_pop(_state, stackDelta);
                }
            }

            set
            {
                if (s is null)
                {
                    throw new ArgumentNullException(nameof(s));
                }

                _environment.ThrowIfDisposed();
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);  // 2 stack slots required

                var stackDelta = 0;

                try
                {
                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    ++stackDelta;

                    _environment.PushObject(_state, value);
                    ++stackDelta;

                    using var buffer = _environment.MarshalString(s, isNullTerminated: true);
                    lua_setfield(_state, -2, buffer.Pointer);
                    --stackDelta;
                }
                finally
                {
                    lua_pop(_state, stackDelta);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value with index <paramref name="n"/>.
        /// </summary>
        /// <param name="n">The number.</param>
        /// <returns>The value with index <paramref name="n"/>.</returns>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object? this[long n]
        {
            get
            {
                _environment.ThrowIfDisposed();
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);  // 2 stack slots required

                var stackDelta = 0;

                try
                { 
                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    ++stackDelta;

                    var type = lua_geti(_state, -1, n);
                    ++stackDelta;

                    return _environment.ToObject(_state, -1, typeHint: type);
                }
                finally
                {
                    lua_pop(_state, stackDelta);
                }
            }

            set
            {
                _environment.ThrowIfDisposed();
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);  // 2 stack slots required

                var stackDelta = 0;

                try
                {
                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    ++stackDelta;

                    _environment.PushObject(_state, value);
                    ++stackDelta;

                    lua_seti(_state, -2, n);
                    --stackDelta;
                }
                finally
                {
                    lua_pop(_state, stackDelta);
                }
            }
        }
 
        /// <summary>
        /// Gets or sets the value corresponding to the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value corresponding to the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        public object? this[object key]
        {
            get
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _environment.ThrowIfDisposed();
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);  // 2 stack slots required

                var stackDelta = 0;

                try
                {
                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    ++stackDelta;

                    _environment.PushObject(_state, key);
                    ++stackDelta;

                    var type = lua_gettable(_state, -2);
                    return _environment.ToObject(_state, -1, typeHint: type);
                }
                finally
                {
                    lua_pop(_state, stackDelta);
                }
            }

            set
            {
                if (key is null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _environment.ThrowIfDisposed();
                _environment.ThrowIfNotEnoughLuaStack(_state, 3);  // 3 stack slots required

                var stackDelta = 0;

                try
                {
                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    ++stackDelta;

                    _environment.PushObject(_state, key);
                    ++stackDelta;

                    _environment.PushObject(_state, value);
                    ++stackDelta;

                    lua_settable(_state, -3);
                    stackDelta -= 2;
                }
                finally
                {
                    lua_pop(_state, stackDelta);
                }
            }
        }
    }
}
