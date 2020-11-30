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
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents the argument of a Lua access as a tagged union.
    /// </summary>
    /// <remarks>
    /// This structure is ephemeral.
    /// </remarks>
    [DebuggerDisplay("{ToDebugString(),nq}")]
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Explicit)]
    public readonly ref struct LuaArgument
    {
        /// <summary>
        /// Specifies the tag of a primitive <see cref="LuaArgument"/>.
        /// </summary>
        private enum PrimitiveTag
        {
            Boolean,
            Integer,
            Number
        }

        /// <summary>
        /// Specifies the tag of an object <see cref="LuaArgument"/>.
        /// </summary>
        private enum ObjectTag
        {
            String,
            Table,
            Function,
            Thread,
            ClrObject,
            ClrTypes
        }

        private static readonly StrongBox<PrimitiveTag> s_booleanTag = new(PrimitiveTag.Boolean);
        private static readonly StrongBox<PrimitiveTag> s_integerTag = new(PrimitiveTag.Integer);
        private static readonly StrongBox<PrimitiveTag> s_numberTag  = new(PrimitiveTag.Number);

        [FieldOffset(0)] private readonly bool      _boolean;
        [FieldOffset(0)] private readonly long      _integer;
        [FieldOffset(0)] private readonly double    _number;
        [FieldOffset(0)] private readonly ObjectTag _objectTag;

        [FieldOffset(8)] private readonly object?   _objectOrPrimitiveTag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(bool boolean)
        {
            _boolean = boolean;
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);
            Unsafe.SkipInit(out _objectTag);

            _objectOrPrimitiveTag = s_booleanTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(long integer)
        {
            Unsafe.SkipInit(out _boolean);
            _integer = integer;
            Unsafe.SkipInit(out _number);
            Unsafe.SkipInit(out _objectTag);

            _objectOrPrimitiveTag = s_integerTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(double number)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            _number = number;
            Unsafe.SkipInit(out _objectTag);

            _objectOrPrimitiveTag = s_numberTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LuaArgument(ObjectTag tag, object? obj)
        {
            Unsafe.SkipInit(out _boolean);
            Unsafe.SkipInit(out _integer);
            Unsafe.SkipInit(out _number);
            _objectTag = tag;

            _objectOrPrimitiveTag = obj;
        }

        /// <summary>
        /// Gets the <see langword="nil"/> value.
        /// </summary>
        public static LuaArgument Nil => default;

        #region FromXxx(...) methods

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

        /// <summary>
        /// Converts a boolean into an argument.
        /// </summary>
        /// <param name="boolean">The boolean to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromBoolean(bool boolean) => new(boolean);

        /// <summary>
        /// Converts an integer into an argument.
        /// </summary>
        /// <param name="integer">The integer to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromInteger(long integer) => new(integer);

        /// <summary>
        /// Converts a number into an argument.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromNumber(double number) => new(number);

        /// <summary>
        /// Converts a string into an argument.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromString(string? str) => new(ObjectTag.String, str);

        /// <summary>
        /// Converts a table into an argument.
        /// </summary>
        /// <param name="table">The table to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromTable(LuaTable? table) => new(ObjectTag.Table, table);

        /// <summary>
        /// Converts a function into an argument.
        /// </summary>
        /// <param name="function">The function to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromFunction(LuaFunction? function) => new(ObjectTag.Function, function);

        /// <summary>
        /// Converts a thread into an argument.
        /// </summary>
        /// <param name="thread">The thread to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromThread(LuaThread? thread) => new(ObjectTag.Thread, thread);

        /// <summary>
        /// Converts a CLR object into an argument.
        /// </summary>
        /// <param name="obj">The CLR object to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromClrObject(object? obj) => new(ObjectTag.ClrObject, obj);

        /// <summary>
        /// Converts CLR types into an argument.
        /// </summary>
        /// <param name="types">The CLR types to convert.</param>
        /// <returns>The resulting argument.</returns>
        public static LuaArgument FromClrTypes(IReadOnlyList<Type>? types) => new(ObjectTag.ClrTypes, types);

        #endregion

        internal unsafe void Push(lua_State* state)
        {
            var objectOrPrimitiveTag = _objectOrPrimitiveTag;  // local optimization

            if (objectOrPrimitiveTag is null)
            {
                lua_pushnil(state);
            }
            else if (objectOrPrimitiveTag.GetType() == typeof(StrongBox<PrimitiveTag>))
            {
                var tag = Unsafe.As<object, StrongBox<PrimitiveTag>>(ref objectOrPrimitiveTag).Value;
                Debug.Assert(tag is >= PrimitiveTag.Boolean and <= PrimitiveTag.Number);

                if (tag == PrimitiveTag.Boolean)
                    lua_pushboolean(state, _boolean);
                else if (tag == PrimitiveTag.Integer)
                    lua_pushinteger(state, _integer);
                else
                    lua_pushnumber(state, _number);
            }
            else
            {
                var tag = _objectTag;
                Debug.Assert(tag is >= ObjectTag.String and <= ObjectTag.ClrTypes);

                switch (tag)
                {
                    case ObjectTag.String:
                        lua_pushstring(state, Unsafe.As<object, string>(ref objectOrPrimitiveTag));
                        break;

                    case ObjectTag.Table:
                        Unsafe.As<object, LuaTable>(ref objectOrPrimitiveTag).Push(state);
                        break;

                    case ObjectTag.Function:
                        Unsafe.As<object, LuaFunction>(ref objectOrPrimitiveTag).Push(state);
                        break;

                    case ObjectTag.Thread:
                        Unsafe.As<object, LuaThread>(ref objectOrPrimitiveTag).Push(state);
                        break;

                    case ObjectTag.ClrObject:
                        ThrowHelper.ThrowInvalidCastException();
                        break;

                    default:
                        ThrowHelper.ThrowInvalidCastException();
                        break;
                }
            }
        }

        [ExcludeFromCodeCoverage]
        internal string ToDebugString() =>
            _objectOrPrimitiveTag switch
            {
                null => "nil",
                StrongBox<PrimitiveTag> { Value: var primitiveTag } =>
                    primitiveTag switch
                    {
                        PrimitiveTag.Boolean => _boolean.ToString(),
                        PrimitiveTag.Integer => _integer.ToString(),
                        _                    => _number.ToString(),
                    },
                object obj =>
                    _objectTag switch
                    {
                        ObjectTag.String    => $"\"{(string)obj}\"",
                        ObjectTag.Table     => ((LuaTable)obj).ToDebugString(),
                        ObjectTag.Function  => ((LuaFunction)obj).ToDebugString(),
                        ObjectTag.Thread    => ((LuaThread)obj).ToDebugString(),
                        ObjectTag.ClrObject => $"CLR object: {obj}",
                        _                   => $"CLR types: ({string.Join(", ", (IReadOnlyList<Type>)obj)})"
                    }
            };

        #region implicit operators

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

        #endregion
    }
}
