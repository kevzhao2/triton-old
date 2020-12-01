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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua result.
    /// </summary>
    /// <remarks>
    /// This structure is ephemeral and is invalidated immediately after calling another Lua API.
    /// </remarks>
    [DebuggerDisplay("{ToDebugString(),nq}")]
    [DebuggerStepThrough]
    public unsafe readonly ref struct LuaResult
    {
        // Keep a combination of the Lua state and index.
        //
        // The `LuaResult` structure can lazily represent any Lua result with indices 1 through 8 using just eight
        // bytes, making it quite efficient. The limitation of not being able to represent a result with index greater
        // than 8 is very unlikely to have a real-world impact.
        //
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
                    return false;

                var userdata = lua_touserdata(state, index);
                return userdata is not null && (*(nint*)userdata & 1) == 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is CLR type(s).
        /// </summary>
        public bool IsClrTypes
        {
            get
            {
                var (state, index) = this;

                if (state is null)
                    return false;

                var userdata = lua_touserdata(state, index);
                return userdata is not null && (*(nint*)userdata & 1) != 0;
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

            bool isInteger;  // skip initialization

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

            bool isNumber;  // skip initialization

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

            nuint len;  // skip initialization

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

            var userdata = lua_touserdata(state, index);
            if (userdata is null)
                ThrowHelper.ThrowInvalidCastException();

            var ptr = *(nint*)userdata;
            if ((ptr & 1) != 0)
                ThrowHelper.ThrowInvalidCastException();

            var handle = GCHandle.FromIntPtr(ptr);  // lowest bit is already cleared
            return handle.Target!;
        }

        /// <summary>
        /// Converts the result into CLR type(s).
        /// </summary>
        /// <returns>The resulting CLR type(s).</returns>
        /// <exception cref="InvalidCastException">The result is not CLR type(s).</exception>
        public Type[] ToClrTypes()
        {
            var (state, index) = this;

            if (state is null)
                ThrowHelper.ThrowInvalidCastException();

            var userdata = lua_touserdata(state, index);
            if (userdata is null)
                ThrowHelper.ThrowInvalidCastException();

            var ptr = *(nint*)userdata;
            if ((ptr & 1) == 0)
                ThrowHelper.ThrowInvalidCastException();

            var handle = GCHandle.FromIntPtr(ptr & ~1);  // clear lowest bit
            var target = handle.Target!;
            return Unsafe.As<object, Type[]>(ref target);  // using an unsafe cast results in optimal codegen
        }

        #endregion

        // Because this method is not on a hot path, it is optimized for readability instead.
        //
        [ExcludeFromCodeCoverage]
        internal string ToDebugString()
        {
            var (state, index) = this;

            return state is null ?
                "<uninitialized>" :
                lua_type(state, index) switch
                {
                    LUA_TBOOLEAN  => ToBoolean().ToDebugString(),
                    LUA_TNUMBER   => IsInteger ? ToInteger().ToDebugString() : ToNumber().ToDebugString(),
                    LUA_TSTRING   => ToString().ToDebugString(),
                    LUA_TTABLE    => $"table: 0x{(nint)lua_topointer(state, index):x}",
                    LUA_TFUNCTION => $"function: 0x{(nint)lua_topointer(state, index):x}",
                    LUA_TTHREAD   => $"thread: 0x{(nint)lua_topointer(state, index):x}",
                    LUA_TUSERDATA => IsClrObject ? ToClrObject().ToDebugString() : ToClrTypes().ToDebugString(),
                    _             => "nil"
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
