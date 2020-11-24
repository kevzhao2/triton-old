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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua value as a tagged union.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly unsafe struct LuaValue
    {
        private enum Tag
        {
            Boolean,
            Integer,
            Number
        }

        private static readonly StrongBox<Tag> _booleanTag = new(Tag.Boolean);
        private static readonly StrongBox<Tag> _integerTag = new(Tag.Integer);
        private static readonly StrongBox<Tag> _numberTag = new(Tag.Number);

        [FieldOffset(0)] private readonly bool _boolean;
        [FieldOffset(0)] private readonly long _integer;
        [FieldOffset(0)] private readonly double _number;

        [FieldOffset(8)] private readonly object? _tagOrObject;
        [FieldOffset(8)] private readonly StrongBox<Tag>? _tag;
        [FieldOffset(8)] private readonly string? _string;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaValue(bool boolean)
        {
            _boolean = boolean;
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);

            Unsafe.SkipInit(out _tagOrObject);
            _tag = _booleanTag;
            Unsafe.SkipInit(out _string);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaValue(long integer)
        {
            Unsafe.SkipInit(out _boolean);
            _integer = integer;
            Unsafe.SkipInit(out _number);

            Unsafe.SkipInit(out _tagOrObject);
            _tag = _integerTag;
            Unsafe.SkipInit(out _string);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaValue(double number)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            _number = number;

            Unsafe.SkipInit(out _tagOrObject);
            _tag = _numberTag;
            Unsafe.SkipInit(out _string);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaValue(string? str)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);

            Unsafe.SkipInit(out _tagOrObject);
            Unsafe.SkipInit(out _tag);
            _string = str;
        }

        /// <summary>
        /// Gets the <see langword="nil"/> value.
        /// </summary>
        public static LuaValue Nil => default;

        /// <summary>
        /// Gets a value indicating whether the Lua value is <see langword="nil"/>.
        /// </summary>
        public bool IsNil => _tagOrObject is null;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a boolean.
        /// </summary>
        public bool IsBoolean => ReferenceEquals(_tagOrObject, _booleanTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is an integer.
        /// </summary>
        public bool IsInteger => ReferenceEquals(_tagOrObject, _integerTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is a number.
        /// </summary>
        public bool IsNumber => ReferenceEquals(_tagOrObject, _numberTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is a string.
        /// </summary>
        public bool IsString => _tagOrObject is not null && _tagOrObject.GetType() == typeof(string);

        /// <summary>
        /// Creates a Lua value from the given boolean.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromBoolean(bool boolean) => new(boolean);

        /// <summary>
        /// Creates a Lua value from the given integer.
        /// </summary>
        /// <param name="integer">The integer.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromInteger(long integer) => new(integer);

        /// <summary>
        /// Creates a Lua value from the given number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromNumber(double number) => new(number);

        /// <summary>
        /// Creates a Lua value from the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromString(string? str) => new(str);

        internal static LuaValue From(lua_State* state, int idx, int type)
        {
            Debug.Assert(type is >= LUA_TNONE and <= LUA_TTHREAD);

            switch (type)
            {
                default:
                    return Nil;

                case LUA_TBOOLEAN:
                    return FromBoolean(lua_toboolean(state, idx));

                case LUA_TNUMBER:
                    return lua_isinteger(state, idx) ?
                        FromInteger(lua_tointeger(state, idx)) :
                        FromNumber(lua_tonumber(state, idx));

                case LUA_TSTRING:
                    return FromString(lua_tostring(state, idx));
            }
        }

        /// <summary>
        /// Converts the Lua value into a boolean.
        /// </summary>
        /// <returns>The resulting boolean.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a boolean.</exception>
        public bool AsBoolean()
        {
            if (!ReferenceEquals(_tagOrObject, _booleanTag))
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return _boolean;
        }

        /// <summary>
        /// Converts the Lua value into an integer.
        /// </summary>
        /// <returns>The resulting integer.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not an integer.</exception>
        public long AsInteger()
        {
            if (!ReferenceEquals(_tagOrObject, _integerTag))
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return _integer;
        }

        /// <summary>
        /// Converts the Lua value into a number.
        /// </summary>
        /// <returns>The resulting number.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a number.</exception>
        public double AsNumber()
        {
            if (!ReferenceEquals(_tagOrObject, _numberTag))
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return _number;
        }

        /// <summary>
        /// Converts the Lua value into a string.
        /// </summary>
        /// <returns>The resulting string.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a string.</exception>
        public string AsString()
        {
            if (_tagOrObject is null || _tagOrObject.GetType() != typeof(string))
            {
                ThrowHelper.ThrowInvalidCastException();
            }

            return _string!;
        }

        internal void Push(lua_State* state)
        {
            if (_tagOrObject is null)
            {
                lua_pushnil(state);
            }
            else if (_tagOrObject.GetType() == typeof(StrongBox<Tag>))
            {
                Debug.Assert(_tag is { Value: >= Tag.Boolean and <= Tag.Number });

                if (_tag.Value == Tag.Boolean)
                {
                    lua_pushboolean(state, _boolean);
                }
                else if (_tag.Value == Tag.Integer)
                {
                    lua_pushinteger(state, _integer);
                }
                else
                {
                    lua_pushnumber(state, _number);
                }
            }
            else if (_tagOrObject.GetType() == typeof(string))
            {
                Debug.Assert(_string is { });

                lua_pushstring(state, _string);
            }
            else if (_tagOrObject.GetType() == typeof(LuaTable))
            {
                throw new NotImplementedException();
            }
            else if (_tagOrObject.GetType() == typeof(LuaFunction))
            {
                throw new NotImplementedException();
            }
            else if (_tagOrObject.GetType() == typeof(LuaThread))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Converts the boolean into a Lua value.
        /// </summary>
        /// <param name="boolean">The boolean to convert.</param>
        public static implicit operator LuaValue(bool boolean) => FromBoolean(boolean);

        /// <summary>
        /// Converts the integer into a Lua value.
        /// </summary>
        /// <param name="integer">The integer to convert.</param>
        public static implicit operator LuaValue(long integer) => FromInteger(integer);

        /// <summary>
        /// Converts the number into a Lua value.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        public static implicit operator LuaValue(double number) => FromNumber(number);

        /// <summary>
        /// Converts the string into a Lua value.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        public static implicit operator LuaValue(string? str) => FromString(str);

        /// <summary>
        /// Converts the Lua value into a boolean.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a boolean.</exception>
        public static explicit operator bool(in LuaValue value) => value.AsBoolean();

        /// <summary>
        /// Converts the Lua value into an integer.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not an integer.</exception>
        public static explicit operator long(in LuaValue value) => value.AsInteger();

        /// <summary>
        /// Converts the Lua value into a number.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a number.</exception>
        public static explicit operator double(in LuaValue value) => value.AsNumber();

        /// <summary>
        /// Converts the Lua value into a string.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a string.</exception>
        public static explicit operator string(in LuaValue value) => value.AsString();
    }
}
