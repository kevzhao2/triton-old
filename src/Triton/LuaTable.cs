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
using System.Text;
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

                // We require 2 stack slots since the table and the value of the field are pushed onto the stack.
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);

#if DEBUG
                var oldTop = lua_gettop(_state);

                try
                {
#endif
                    // May throw an exception
                    var buffer = _environment.MarshalString(s, out _, out var wasAllocated, isNullTerminated: true);

                    lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
                    var type = lua_getfield(_state, -1, buffer);

                    if (wasAllocated)
                    {
                        Marshal.FreeHGlobal((IntPtr)buffer);
                    }

                    try
                    {
                        return _environment.ToObject(_state, -1, typeHint: type);  // May throw an exception
                    }
                    finally
                    {
                        lua_pop(_state, 2);  // Pop the table and the field value off of the stack
                    }
#if DEBUG
                }
                finally
                {
                    Debug.Assert(lua_gettop(_state) == oldTop);
                }
#endif
            }

            set
            {
                if (s is null)
                {
                    throw new ArgumentNullException(nameof(s));
                }

                _environment.ThrowIfDisposed();

                // We require 2 stack slots since the table and the new value of the field are pushed onto the stack.
                _environment.ThrowIfNotEnoughLuaStack(_state, 2);

#if DEBUG
                var oldTop = lua_gettop(_state);
#endif

                lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);

                try
                {
                    // Push the value onto the stack first, as it may involve using the string buffer.
                    _environment.PushObject(_state, value);  // May throw an exception

                    try
                    {
                        // May throw an exception
                        var buffer = _environment.MarshalString(s, out _, out var wasAllocated, isNullTerminated: true);
                        lua_setfield(_state, -2, buffer);
                        if (wasAllocated)
                        {
                            Marshal.FreeHGlobal((IntPtr)buffer);
                        }

                    }
                    catch (EncoderFallbackException)
                    {
                        lua_pop(_state, 1);  // Pop the new value of the field off the stack
                        throw;
                    }
                }
                finally
                {
                    lua_pop(_state, 1);  // Pop the table off the stack
#if DEBUG

                    Debug.Assert(lua_gettop(_state) == oldTop);
#endif
                }
            }
        }
    }
}
