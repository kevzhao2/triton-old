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
using System.Runtime.CompilerServices;
using System.Text;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua result. This structure is intended to be ephemeral.
    /// </summary>
    public unsafe readonly ref struct LuaResult
    {
        private readonly nint _stateAndIndex;

        internal LuaResult(lua_State* state, int index)
        {
            Debug.Assert(index is >= 1 and <= 8);

            _stateAndIndex = (nint)state | (index - 1);
        }

        /// <summary>
        /// Gets a value indicating whether the result is <see langword="nil"/>.
        /// </summary>
        public bool IsNil => lua_type(State, Index) <= LUA_TNIL;  // can include LUA_TNONE

        /// <summary>
        /// Gets a value indicating whether the result is a boolean.
        /// </summary>
        public bool IsBoolean => lua_type(State, Index) == LUA_TBOOLEAN;

        /// <summary>
        /// Gets a value indicating whether the result is an integer.
        /// </summary>
        public bool IsInteger => lua_isinteger(State, Index);

        /// <summary>
        /// Gets a value indicating whether the result is a number.
        /// </summary>
        public bool IsNumber => lua_type(State, Index) == LUA_TNUMBER && !IsInteger;

        /// <summary>
        /// Gets a value indicating whether the result is a string.
        /// </summary>
        public bool IsString => lua_type(State, Index) == LUA_TSTRING;

        /// <summary>
        /// Gets a value indicating whether the result is a table.
        /// </summary>
        public bool IsTable => lua_type(State, Index) == LUA_TTABLE;

        /// <summary>
        /// Gets a value indicating whether the result is a function.
        /// </summary>
        public bool IsFunction => lua_type(State, Index) == LUA_TFUNCTION;

        /// <summary>
        /// Gets a value indicating whether the result is a thread.
        /// </summary>
        public bool IsThread => lua_type(State, Index) == LUA_TTHREAD;

        /// <summary>
        /// Gets a value indicating whether the result is a CLR object.
        /// </summary>
        public bool IsClrObject
        {
            get
            {
                var userdata = lua_touserdata(State, Index);
                return userdata is not null && *(bool*)userdata;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the result is CLR types.
        /// </summary>
        public bool IsClrTypes
        {
            get
            {
                var userdata = lua_touserdata(State, Index);
                return userdata is not null && !*(bool*)userdata;
            }
        }

        private lua_State* State => (lua_State*)(_stateAndIndex & ~7);

        private int Index => (int)(_stateAndIndex & 7) + 1;

        /// <inheritdoc cref="explicit operator bool"/>
        public bool ToBoolean() => lua_toboolean(State, Index);

        /// <inheritdoc cref="explicit operator long"/>
        /// <returns>The resulting integer.</returns>
        [SkipLocalsInit]
        public long ToInteger()
        {
            bool isInteger;

            var result = lua_tointegerx(State, Index, &isInteger);
            if (!isInteger)
                ThrowHelper.ThrowInvalidCastException();

            return result;
        }

        /// <inheritdoc cref="explicit operator double"/>
        /// <returns>The resulting number.</returns>
        [SkipLocalsInit]
        public double ToNumber()
        {
            bool isNumber;

            var result = lua_tonumberx(State, Index, &isNumber);
            if (!isNumber)
                ThrowHelper.ThrowInvalidCastException();

            return result;
        }

        /// <inheritdoc cref="explicit operator string"/>
        /// <returns>The resulting string.</returns>
        [SkipLocalsInit]
        public new string ToString()
        {
            nuint len;

            var bytes = lua_tolstring(State, Index, &len);
            if (bytes is null)
                ThrowHelper.ThrowInvalidCastException();

            return Encoding.UTF8.GetString(bytes, (int)len);
        }

        /// <inheritdoc cref="explicit operator LuaTable"/>
        /// <returns>The resulting table.</returns>
        public LuaTable ToTable()
        {
            if (lua_type(State, Index) != LUA_TTABLE)
                ThrowHelper.ThrowInvalidCastException();

            lua_pushvalue(State, Index);
            var @ref = luaL_ref(State, LUA_REGISTRYINDEX);
            return new(State, @ref);
        }

        /// <inheritdoc cref="explicit operator LuaFunction"/>
        /// <returns>The resulting function.</returns>
        public LuaFunction ToFunction()
        {
            if (lua_type(State, Index) != LUA_TFUNCTION)
                ThrowHelper.ThrowInvalidCastException();

            lua_pushvalue(State, Index);
            var @ref = luaL_ref(State, LUA_REGISTRYINDEX);
            return new(State, @ref);
        }

        /// <inheritdoc cref="explicit operator LuaThread"/>
        /// <returns>The resulting thread.</returns>
        public LuaThread ToThread()
        {
            if (lua_type(State, Index) != LUA_TTHREAD)
                ThrowHelper.ThrowInvalidCastException();

            lua_pushvalue(State, Index);
            var @ref = luaL_ref(State, LUA_REGISTRYINDEX);
            return new((lua_State*)lua_topointer(State, Index), @ref);
        }

        /// <summary>
        /// Converts the result into a CLR object.
        /// </summary>
        /// <returns>The resulting CLR object.</returns>
        /// <exception cref="InvalidCastException">The result is not a CLR object.</exception>
        public object ToClrObject()
        {
            var userdata = lua_touserdata(State, Index);
            if (userdata is null || !*(bool*)userdata)
                ThrowHelper.ThrowInvalidCastException();

            var environment = lua_getenvironment(State);
            return environment.ToClrObject(State, Index);
        }

        /// <summary>
        /// Converts the result into CLR types.
        /// </summary>
        /// <returns>The resulting CLR types.</returns>
        /// <exception cref="InvalidCastException">The result is not CLR types.</exception>
        public IReadOnlyList<Type> ToClrTypes()
        {
            var userdata = lua_touserdata(State, Index);
            if (userdata is null || *(bool*)userdata)
                ThrowHelper.ThrowInvalidCastException();

            var environment = lua_getenvironment(State);
            return environment.ToClrTypes(State, Index);
        }

        /// <summary>
        /// Converts the result into a boolean, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        public static explicit operator bool(LuaResult result) => result.ToBoolean();

        /// <summary>
        /// Converts the result into an integer, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into an integer.</exception>
        public static explicit operator long(LuaResult result) => result.ToInteger();

        /// <summary>
        /// Converts the result into a number, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a number.</exception>
        public static explicit operator double(LuaResult result) => result.ToNumber();

        /// <summary>
        /// Converts the result into a string, performing coercion if necessary.
        /// </summary>
        /// <param name="result">The Lua result to convert.</param>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a string.</exception>
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
    }
}
