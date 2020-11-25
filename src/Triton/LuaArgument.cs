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
    /// Represents the argument of a Lua access as a tagged union. This structure is ephemeral.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly ref struct LuaArgument
    {
        private enum Tag
        {
            Boolean,
            Integer,
            Number
        }

        private static readonly StrongBox<Tag> _booleanTag = new(Tag.Boolean);
        private static readonly StrongBox<Tag> _integerTag = new(Tag.Integer);
        private static readonly StrongBox<Tag> _numberTag  = new(Tag.Number);

        [FieldOffset(0)] private readonly bool  _boolean;
        [FieldOffset(0)] private readonly long  _integer;
        [FieldOffset(0)] private readonly double _number;

        [FieldOffset(8)] private readonly object?         _tagOrObject;
        [FieldOffset(8)] private readonly StrongBox<Tag>? _tag;
        [FieldOffset(8)] private readonly string?         _string;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(bool boolean)
        {
            _boolean = boolean;
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);

            Unsafe.SkipInit(out _tagOrObject);
            _tag = _booleanTag;
            Unsafe.SkipInit(out _string);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(long integer)
        {
            Unsafe.SkipInit(out _boolean);
            _integer = integer;
            Unsafe.SkipInit(out _number);

            Unsafe.SkipInit(out _tagOrObject);
            _tag = _integerTag;
            Unsafe.SkipInit(out _string);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(double number)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            _number = number;

            Unsafe.SkipInit(out _tagOrObject);
            _tag = _numberTag;
            Unsafe.SkipInit(out _string);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(string? str)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);

            Unsafe.SkipInit(out _tagOrObject);
            Unsafe.SkipInit(out _tag);
            _string = str;
        }

        /// <summary>
        /// Gets the <see langword="nil"/> argument.
        /// </summary>
        public static LuaArgument Nil => default;

        /// <summary>
        /// Creates an argument from the given boolean.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromBoolean(bool boolean) => new(boolean);

        /// <summary>
        /// Creates an argument from the given integer.
        /// </summary>
        /// <param name="integer">The integer.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromInteger(long integer) => new(integer);

        /// <summary>
        /// Creates an argument from the given number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromNumber(double number) => new(number);

        /// <summary>
        /// Creates an argument from the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromString(string? str) => new(str);

        internal unsafe void Push(lua_State* state)
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
        /// Converts the boolean into an argument.
        /// </summary>
        /// <param name="boolean">The boolean to convert.</param>
        public static implicit operator LuaArgument(bool boolean) => FromBoolean(boolean);

        /// <summary>
        /// Converts the integer into an argument.
        /// </summary>
        /// <param name="integer">The integer to convert.</param>
        public static implicit operator LuaArgument(long integer) => FromInteger(integer);

        /// <summary>
        /// Converts the number into an argument.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        public static implicit operator LuaArgument(double number) => FromNumber(number);

        /// <summary>
        /// Converts the string into an argument.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        public static implicit operator LuaArgument(string? str) => FromString(str);
    }
}
