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
using System.Reflection;
using static Triton.Lua;
using static Triton.Lua.LuaType;

namespace Triton.Interop.Emit.Helpers
{
    /// <summary>
    /// Provides helper methods for array interop.
    /// </summary>
    internal static unsafe class ArrayHelpers
    {
        internal static readonly MethodInfo _getNdArrayIndices =
            typeof(ArrayHelpers).GetMethod(nameof(GetNdArrayIndices))!;

        /// <summary>
        /// Gets the n-dimensional array indices from the Lua stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the value.</param>
        /// <param name="arrayIndices">The resulting array indices.</param>
        /// <returns>
        /// <see langword="true"/> if the indices were successfully obtained; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool GetNdArrayIndices(lua_State* state, int index, Span<int> arrayIndices)
        {
            var type = lua_type(state, index);
            if (type == LUA_TNUMBER)
            {
                if (arrayIndices.Length != 1 || !lua_isinteger(state, index))
                {
                    return false;
                }

                var integer = lua_tointeger(state, index);
                if ((ulong)(integer - int.MinValue) > uint.MaxValue)
                {
                    return false;
                }

                arrayIndices[0] = (int)integer;
                return true;
            }
            else if (type == LUA_TTABLE)
            {
                if (arrayIndices.Length != (int)lua_rawlen(state, index))
                {
                    return false;
                }

                for (var i = 0; i < arrayIndices.Length; ++i)
                {
                    if (lua_rawgeti(state, index, i + 1) != LUA_TNUMBER || !lua_isinteger(state, -1))
                    {
                        return false;
                    }

                    var integer = lua_tointeger(state, -1);
                    if ((ulong)(integer - int.MinValue) > uint.MaxValue)
                    {
                        return false;
                    }

                    arrayIndices[i] = (int)integer;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
