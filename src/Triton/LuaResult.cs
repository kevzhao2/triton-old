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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua result.
    /// </summary>
    /// <remarks>
    /// This structure is ephemeral and is invalidated immediately after calling another API. It allows for lazy
    /// computation; if the result is not inspected, no extra work is done.
    /// </remarks>
    [DebuggerDisplay("{ToDebugString(),nq}")]
    [DebuggerStepThrough]
    public unsafe readonly ref struct LuaResult
    {
        private readonly nint _stateAndIndex;

        internal LuaResult(lua_State* state, int index)
        {
            Debug.Assert(((nint)state & 7) == 0);
            Debug.Assert(index is >= 1 and <= 8);

            _stateAndIndex = (nint)state | (index - 1);
        }

        #region IsXxx properties

        /// <summary>
        /// Gets a value indicating whether the result is <see langword="nil"/>.
        /// </summary>
        public bool IsNil
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) <= LUA_TNIL;  // can include LUA_TNONE
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a boolean.
        /// </summary>
        public bool IsBoolean
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) == LUA_TBOOLEAN;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is an integer.
        /// </summary>
        public bool IsInteger
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_isinteger(state, index);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a number.
        /// </summary>
        public bool IsNumber
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) == LUA_TNUMBER && !lua_isinteger(state, index);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a string.
        /// </summary>
        public bool IsString
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) == LUA_TSTRING;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a table.
        /// </summary>
        public bool IsTable
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) == LUA_TTABLE;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a function.
        /// </summary>
        public bool IsFunction
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) == LUA_TFUNCTION;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a thread.
        /// </summary>
        public bool IsThread
        {
            get
            {
                var (state, index) = this;

                return state is not null && lua_type(state, index) == LUA_TTHREAD;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is a CLR object.
        /// </summary>
        public bool IsClrObject
        {
            get
            {
                var (state, index) = this;

                if (state is null)
                {
                    return false;
                }

                var ptr = lua_touserdata(state, index);
                return ptr is not null && *(bool*)ptr;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is CLR types.
        /// </summary>
        public bool IsClrTypes
        {
            get
            {
                var (state, index) = this;

                if (state is null)
                {
                    return false;
                }

                var ptr = lua_touserdata(state, index);
                return ptr is not null && !*(bool*)ptr;
            }
        }

        #endregion

        private void Deconstruct(out lua_State* state, out int index)
        {
            state = (lua_State*)(_stateAndIndex & ~7);
            index = (int)(_stateAndIndex & 7) + 1;
        }

        #region ToXxx() methods

        /// <summary>
        /// Converts the result into a boolean, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting boolean.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a boolean.</exception>
        /// <remarks>
        /// All values are coercible to booleans. <see langword="false"/> and <see langword="nil"/> are coerced into
        /// <see langword="false"/>; every other value is coerced into <see langword="true"/>.
        /// </remarks>
        public bool ToBoolean()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            return lua_toboolean(state, index);
        }

        /// <summary>
        /// Converts the result into an integer, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting integer.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into an integer.</exception>
        /// <remarks>
        /// Integers, numbers with integral values, and strings parseable as integers are coercible to integers.
        /// </remarks>
        [SkipLocalsInit]
        public long ToInteger()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            bool isInteger;

            var result = lua_tointegerx(state, index, &isInteger);
            if (!isInteger)
                ThrowHelper.ThrowInvalidCastException();

            return result;
        }

        /// <summary>
        /// Converts the result into a number, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting number.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a number.</exception>
        /// <remarks>
        /// Integers, numbers, and strings parseable as numbers are coercible to numbers.
        /// </remarks>
        [SkipLocalsInit]
        public double ToNumber()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            bool isNumber;

            var result = lua_tonumberx(state, index, &isNumber);
            if (!isNumber)
                ThrowHelper.ThrowInvalidCastException();

            return result;
        }

        /// <summary>
        /// Converts the result into a string, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting string.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a string.</exception>
        /// <remarks>
        /// Integers, numbers, and strings are coercible to strings. <i>Note that this operation can actually mutate the
        /// result!</i>
        /// </remarks>
        [SkipLocalsInit]
        public new string ToString()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            nuint len;

            var bytes = lua_tolstring(state, index, &len);
            if (bytes is null)
                ThrowHelper.ThrowInvalidCastException();

            return Encoding.UTF8.GetString(bytes, (int)len);
        }

        /// <summary>
        /// Converts the result into a table.
        /// </summary>
        /// <returns>The resulting table.</returns>
        /// <exception cref="InvalidCastException">The result is not a table.</exception>
        public LuaTable ToTable()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            if (lua_type(state, index) != LUA_TTABLE)
                ThrowHelper.ThrowInvalidCastException();

            lua_pushvalue(state, index);
            return new(state, luaL_ref(state, LUA_REGISTRYINDEX));
        }

        /// <summary>
        /// Converts the result into a function.
        /// </summary>
        /// <returns>The resulting function.</returns>
        /// <exception cref="InvalidCastException">The result is not a function.</exception>
        public LuaFunction ToFunction()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            if (lua_type(state, index) != LUA_TFUNCTION)
                ThrowHelper.ThrowInvalidCastException();

            lua_pushvalue(state, index);
            return new(state, luaL_ref(state, LUA_REGISTRYINDEX));
        }

        /// <summary>
        /// Converts the result into a thread.
        /// </summary>
        /// <returns>The resulting thread.</returns>
        /// <exception cref="InvalidCastException">The result is not a thread.</exception>
        public LuaThread ToThread()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            if (lua_type(state, index) != LUA_TTHREAD)
                ThrowHelper.ThrowInvalidCastException();

            lua_pushvalue(state, index);
            return new((lua_State*)lua_topointer(state, index), luaL_ref(state, LUA_REGISTRYINDEX));
        }

        /// <summary>
        /// Converts the result into a CLR object.
        /// </summary>
        /// <returns>The resulting CLR object.</returns>
        /// <exception cref="InvalidCastException">The result is not a CLR object.</exception>
        public object ToClrObject()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            var ptr = lua_touserdata(state, index);
            if (ptr is null || !*(bool*)ptr)
                ThrowHelper.ThrowInvalidCastException();

            var environment = lua_getenvironment(state);
            return environment.ToClrObject(state, index);
        }

        /// <summary>
        /// Converts the result into CLR types.
        /// </summary>
        /// <returns>The resulting CLR types.</returns>
        /// <exception cref="InvalidCastException">The result is not CLR types.</exception>
        public IReadOnlyList<Type> ToClrTypes()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            var ptr = lua_touserdata(state, index);
            if (ptr is null || *(bool*)ptr)
                ThrowHelper.ThrowInvalidCastException();

            var environment = lua_getenvironment(state);
            return environment.ToClrTypes(state, index);
        }

        #endregion

        [ExcludeFromCodeCoverage]
        internal string ToDebugString()
        {
            var (state, index) = this;

            return state is null ?
                "<uninitialized>" :
                lua_type(state, index) switch
                {
                    LUA_TBOOLEAN  => lua_toboolean(state, index).ToString(),
                    LUA_TNUMBER   => lua_isinteger(state, index) ?
                                         lua_tointegerx(state, index, null).ToString() :
                                         lua_tonumberx(state, index, null).ToString(),
                    LUA_TSTRING   => $"\"{lua_tostring(state, index)}\"",
                    LUA_TTABLE    => $"table: 0x{Convert.ToString((long)lua_topointer(state, index), 16)}",
                    LUA_TFUNCTION => $"function: 0x{Convert.ToString((long)lua_topointer(state, index), 16)}",
                    LUA_TTHREAD   => $"thread: 0x{Convert.ToString((long)lua_topointer(state, index), 16)}",
                    LUA_TUSERDATA => throw new NotImplementedException(),
                    _             => "nil",
                };
        }

        #region explicit operators

        /// <summary>
        /// Converts the result into a boolean, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a boolean.</exception>
        /// <remarks>
        /// All values are coercible to booleans. <see langword="false"/> and <see langword="nil"/> are coerced into
        /// <see langword="false"/>; every other value is coerced into <see langword="true"/>.
        /// </remarks>
        public static explicit operator bool(LuaResult result) => result.ToBoolean();

        /// <summary>
        /// Converts the result into an integer, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into an integer.</exception>
        /// <remarks>
        /// Integers, numbers with integral values, and strings parseable as integers are coercible to integers.
        /// </remarks>
        public static explicit operator long(LuaResult result) => result.ToInteger();

        /// <summary>
        /// Converts the result into a number, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a number.</exception>
        /// <remarks>
        /// Integers, numbers, and strings parseable as numbers are coercible to numbers.
        /// </remarks>
        public static explicit operator double(LuaResult result) => result.ToNumber();

        /// <summary>
        /// Converts the result into a string, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a string.</exception>
        /// <remarks>
        /// Integers, numbers, and strings are coercible to strings. <i>Note that this operation can actually mutate the
        /// result!</i>
        /// </remarks>
        public static explicit operator string(LuaResult result) => result.ToString();

        /// <summary>
        /// Converts the result into a table.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result is not a table.</exception>
        public static explicit operator LuaTable(LuaResult result) => result.ToTable();

        /// <summary>
        /// Converts the result into a function.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result is not a function.</exception>
        public static explicit operator LuaFunction(LuaResult result) => result.ToFunction();

        /// <summary>
        /// Converts the result into a thread.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result is not a thread.</exception>
        public static explicit operator LuaThread(LuaResult result) => result.ToThread();

        #endregion
    }
}
