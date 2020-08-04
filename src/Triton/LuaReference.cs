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
    /// Represents a Lua reference: a table, function, or thread.
    /// </summary>
    public abstract class LuaReference
    {
        private protected readonly IntPtr _state;
        private protected readonly LuaEnvironment _environment;
        private protected readonly int _ref;

        private protected LuaReference(IntPtr state, LuaEnvironment environment, int @ref)
        {
            Debug.Assert(environment is { });

            _state = state;
            _environment = environment;
            _ref = @ref;
        }

        /// <summary>
        /// Pushes the Lua reference onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="environment">The Lua environment.</param>
        internal void Push(IntPtr state, LuaEnvironment environment)
        {
            if (environment != _environment)
            {
                throw new InvalidOperationException("Lua object does not belong to the environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, _ref);
        }
    }
}
