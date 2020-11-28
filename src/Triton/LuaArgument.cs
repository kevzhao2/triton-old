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
    /// Represents the argument of a Lua access as a tagged union. This structure is intended to be ephemeral.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly ref struct LuaArgument
    {
        // TODO: optimize by including tags for string, etc
        private enum Tag
        {
            Boolean,
            Integer,
            Number
        }

        private static readonly StrongBox<Tag> _booleanTag = new(Tag.Boolean);
        private static readonly StrongBox<Tag> _integerTag = new(Tag.Integer);
        private static readonly StrongBox<Tag> _numberTag  = new(Tag.Number);

        [FieldOffset(0)] private readonly bool    _boolean;
        [FieldOffset(0)] private readonly long    _integer;
        [FieldOffset(0)] private readonly double  _number;

        [FieldOffset(8)] private readonly object? _tagOrObject;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(bool boolean)
        {
            _boolean = boolean;
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);

            _tagOrObject = _booleanTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(long integer)
        {
            Unsafe.SkipInit(out _boolean);
            _integer = integer;
            Unsafe.SkipInit(out _number);

            _tagOrObject = _integerTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(double number)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            _number = number;

            _tagOrObject = _numberTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(object? obj)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);

            _tagOrObject = obj;
        }

        /// <summary>
        /// Gets the <see langword="nil"/> argument.
        /// </summary>
        public static LuaArgument Nil => default;

        /// <summary>
        /// Converts an object into an argument.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromObject(object? obj) =>
            obj switch
            {
                null                 => Nil,
                bool        boolean  => FromBoolean(boolean),
                sbyte       integer  => FromInteger(integer),
                byte        integer  => FromInteger(integer),
                short       integer  => FromInteger(integer),
                ushort      integer  => FromInteger(integer),
                long        integer  => FromInteger(integer),
                ulong       integer  => FromInteger((long)integer),
                float       number   => FromNumber(number),
                double      number   => FromNumber(number),
                char        chr      => FromString(chr.ToString()),
                string      str      => FromString(str),
                LuaTable    table    => FromTable(table),
                LuaFunction function => FromFunction(function),
                LuaThread   thread   => FromThread(thread),
                _                    => FromClrObject(obj)
            };

        /// <inheritdoc cref="implicit operator LuaArgument(bool)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromBoolean(bool boolean) => new(boolean);

        /// <inheritdoc cref="implicit operator LuaArgument(long)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromInteger(long integer) => new(integer);

        /// <inheritdoc cref="implicit operator LuaArgument(double)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromNumber(double number) => new(number);

        /// <inheritdoc cref="implicit operator LuaArgument(string)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromString(string? str) => new(str);

        /// <inheritdoc cref="implicit operator LuaArgument(LuaTable)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromTable(LuaTable? table) => new(table);

        /// <inheritdoc cref="implicit operator LuaArgument(LuaFunction)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromFunction(LuaFunction? function) => new(function);

        /// <inheritdoc cref="implicit operator LuaArgument(LuaThread)"/>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromThread(LuaThread? thread) => new(thread);

        /// <summary>
        /// Converts a CLR object into an argument.
        /// </summary>
        /// <param name="obj">The CLR object to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromClrObject(object obj) => new(obj);

        internal unsafe void Push(lua_State* state)
        {
            var tagOrObject = _tagOrObject;

            if (tagOrObject is null)
            {
                lua_pushnil(state);
            }
            else if (tagOrObject.GetType() == typeof(StrongBox<Tag>))
            {
                var tag = Unsafe.As<object, StrongBox<Tag>>(ref tagOrObject);  // optimal cast, should be safe
                Debug.Assert(tag is { Value: >= Tag.Boolean and <= Tag.Number });

                if (tag.Value == Tag.Boolean)
                {
                    lua_pushboolean(state, _boolean);
                }
                else if (tag.Value == Tag.Integer)
                {
                    lua_pushinteger(state, _integer);
                }
                else
                {
                    lua_pushnumber(state, _number);
                }
            }
            else if (tagOrObject.GetType() == typeof(string))
            {
                var str = Unsafe.As<object, string>(ref tagOrObject);  // optimal cast, should be safe
                Debug.Assert(str is { });

                lua_pushstring(state, str);
            }
            else if (tagOrObject.GetType() == typeof(LuaTable))
            {
                var table = Unsafe.As<object, LuaTable>(ref tagOrObject);  // optimal cast, should be safe
                Debug.Assert(table is { });

                table.Push(state);
            }
            else if (tagOrObject.GetType() == typeof(LuaFunction))
            {
                var function = Unsafe.As<object, LuaFunction>(ref tagOrObject);  // optimal cast, should be safe
                Debug.Assert(function is { });

                function.Push(state);
            }
            else if (tagOrObject.GetType() == typeof(LuaThread))
            {
                var thread = Unsafe.As<object, LuaThread>(ref tagOrObject);  // optimal cast, should be safe
                Debug.Assert(thread is { });

                thread.Push(state);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Converts a boolean into an argument.
        /// </summary>
        /// <param name="boolean">The boolean to convert.</param>
        public static implicit operator LuaArgument(bool boolean) => FromBoolean(boolean);

        /// <summary>
        /// Converts an integer into an argument.
        /// </summary>
        /// <param name="integer">The integer to convert.</param>
        public static implicit operator LuaArgument(long integer) => FromInteger(integer);

        /// <summary>
        /// Converts a number into an argument.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        public static implicit operator LuaArgument(double number) => FromNumber(number);

        /// <summary>
        /// Converts a string into an argument.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        public static implicit operator LuaArgument(string? str) => FromString(str);

        /// <summary>
        /// Converts a table into an argument.
        /// </summary>
        /// <param name="table">The table to convert.</param>
        public static implicit operator LuaArgument(LuaTable? table) => FromTable(table);

        /// <summary>
        /// Converts a function into an argument.
        /// </summary>
        /// <param name="function">The function to convert.</param>
        public static implicit operator LuaArgument(LuaFunction? function) => FromFunction(function);

        /// <summary>
        /// Converts a thread into an argument.
        /// </summary>
        /// <param name="thread">The thread to convert.</param>
        public static implicit operator LuaArgument(LuaThread? thread) => FromThread(thread);
    }
}
