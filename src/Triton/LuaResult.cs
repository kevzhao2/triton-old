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
using System.Text;
using static Triton.LuaResultType;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents the result of a Lua access. This structure is ephemeral.
    /// </summary>
    public readonly ref struct LuaResult
    {
        private readonly nint _stateAndIdx;

        internal unsafe LuaResult(lua_State* state, int idx)
        {
            Debug.Assert(idx is >= 1 and <= 8);

            _stateAndIdx = (nint)state | (idx - 1);
        }

        /// <summary>
        /// Gets the type of the result.
        /// </summary>
        public unsafe LuaResultType Type =>
            lua_type(State, Idx) switch
            {
                LUA_TNUMBER        =>          lua_isinteger(State, Idx) ? Integer   : Number,
                LUA_TLIGHTUSERDATA => *(bool*)lua_touserdata(State, Idx) ? ClrObject : ClrTypes,
                int type           => (LuaResultType)type
            };

        private unsafe lua_State* State => (lua_State*)(_stateAndIdx & ~7);

        private int Idx => (int)(_stateAndIdx & 7) + 1;

        /// <summary>
        /// Converts the result into a boolean, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting boolean.</returns>
        public unsafe bool ToBoolean() => lua_toboolean(State, Idx);

        /// <summary>
        /// Converts the result into an integer, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting integer.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into an integer.</exception>
        public unsafe long ToInteger()
        {
            bool isInteger;

            var result = lua_tointegerx(State, Idx, &isInteger);
            if (!isInteger)
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return result;
        }

        /// <summary>
        /// Converts the result into a number, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting number.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a number.</exception>
        public unsafe double ToNumber()
        {
            bool isNumber;

            var result = lua_tonumberx(State, Idx, &isNumber);
            if (!isNumber)
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return result;
        }

        /// <summary>
        /// Converts the result into a string, performing coercion if necessary.
        /// </summary>
        /// <returns>The resulting string.</returns>
        /// <exception cref="InvalidCastException">The result cannot be coerced into a string.</exception>
        public unsafe new string ToString()
        {
            nuint len;

            var bytes = lua_tolstring(State, Idx, &len);
            if (bytes is null)
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return Encoding.UTF8.GetString(bytes, (int)len);
        }

        /// <summary>
        /// Converts the result into a table.
        /// </summary>
        /// <returns>The resulting table.</returns>
        /// <exception cref="InvalidCastException">The result is not a table.</exception>
        public LuaTable ToTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the result into a function.
        /// </summary>
        /// <returns>The resulting function.</returns>
        /// <exception cref="InvalidCastException">The result is not a function.</exception>
        public LuaFunction ToFunction()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the result into a thread.
        /// </summary>
        /// <returns>The resulting thread.</returns>
        /// <exception cref="InvalidCastException">The result is not a thread.</exception>
        public LuaThread ToThread()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the result into a CLR object.
        /// </summary>
        /// <returns>The resulting CLR object.</returns>
        /// <exception cref="InvalidCastException">The result is not a CLR object.</exception>
        public object ToClrObject()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the result into CLR types.
        /// </summary>
        /// <returns>The resulting CLR types.</returns>
        /// <exception cref="InvalidCastException">The result is not CLR types.</exception>
        public IReadOnlyList<Type> ToClrTypes()
        {
            throw new NotImplementedException();
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