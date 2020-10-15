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
using static Triton.Lua;

namespace Triton
{
    /// <summary>
    /// Provides the base class for a Lua object.
    /// </summary>
    public abstract unsafe class LuaObject
    {
        private protected readonly lua_State* _state;
        private protected readonly LuaEnvironment _environment;
        private protected readonly int _ref;

        private protected LuaObject(lua_State* state, LuaEnvironment environment, int @ref)
        {
            _state = state;
            _environment = environment;
            _ref = @ref;
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string ToString() => $"{GetType().Name[3..].ToLower()} {_ref}";

        internal void Push(lua_State* state)
        {
            if (*(IntPtr*)lua_getextraspace(state) != *(IntPtr*)lua_getextraspace(_state))
            {
                throw new InvalidOperationException("Lua object does not belong to the correct environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
        }
    }
}
