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

        // Store a union of (bool, long, double, ObjectTag) at offset 0. This allows us to use eight bytes to either
        // represent a primitive or identify an object tag.
        //
        [FieldOffset(0)] private readonly bool      _boolean;
        [FieldOffset(0)] private readonly long      _integer;
        [FieldOffset(0)] private readonly double    _number;
        [FieldOffset(0)] private readonly ObjectTag _objectTag;

        // Store either an object or a boxed primitive tag at offset 8. This allows us to use eight bytes to either
        // identify a primitive or any object.
        //
        // All together, this allows us to use sixteen bytes to represent any valid Lua argument.
        //
        [FieldOffset(8)] private readonly object?   _objectOrPrimitiveTag;

        #region LuaArgument constructors

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

        #endregion

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
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
        public static LuaArgument FromClrObject(object obj)
        {
            if (obj is null)
                ThrowHelper.ThrowArgumentNullException(nameof(obj));

            return new(ObjectTag.ClrObject, obj);
        }

        /// <summary>
        /// Converts CLR type(s) into an argument.
        /// </summary>
        /// <param name="types">The CLR type(s) to convert.</param>
        /// <returns>The resulting argument.</returns>
        /// <exception cref="ArgumentException"><paramref name="types"/> is empty, contains <see langword="null"/> or contains two types with the same generic arity.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="types"/> is <see langword="null"/>.</exception>
        public static LuaArgument FromClrTypes(params Type[] types)
        {
            if (types is null)
                ThrowHelper.ThrowArgumentNullException(nameof(types));
            if (types.Length == 0)
                ThrowHelper.ThrowArgumentException(nameof(types), "Types is empty");

            var genericArities = new HashSet<int>();
            foreach (var type in types)
            {
                if (type is null)
                    ThrowHelper.ThrowArgumentException(nameof(types), "Types contains null");

                // We do not want two types with the same generic arity. The intent of taking an array of types is to
                // allow for generic arity overloading: e.g., Task referring to `Task` or `Task<>`.
                //
                var genericArity = type.GetGenericArguments().Length;
                if (genericArities.Contains(genericArity))
                    ThrowHelper.ThrowArgumentException(nameof(types), "Types contains two types with the same generic arity");

                genericArities.Add(genericArity);
            }

            return new(ObjectTag.ClrTypes, types);
        }

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
                    {
                        var environment = lua_getenvironment(state);
                        environment.PushClrObject(state, objectOrPrimitiveTag);
                        break;
                    }

                    default:
                    {
                        var environment = lua_getenvironment(state);
                        environment.PushClrTypes(state, Unsafe.As<object, Type[]>(ref objectOrPrimitiveTag));
                        break;
                    }
                }
            }
        }

        [ExcludeFromCodeCoverage]
        internal string ToDebugString()
        {
            var objectOrPrimitiveTag = _objectOrPrimitiveTag;  // local optimization

            if (objectOrPrimitiveTag is null)
            {
                return "nil";
            }
            else if (objectOrPrimitiveTag.GetType() == typeof(StrongBox<PrimitiveTag>))
            {
                var tag = Unsafe.As<object, StrongBox<PrimitiveTag>>(ref objectOrPrimitiveTag).Value;
                Debug.Assert(tag is >= PrimitiveTag.Boolean and <= PrimitiveTag.Number);

                return tag switch
                {
                    PrimitiveTag.Boolean => _boolean.ToString(),
                    PrimitiveTag.Integer => _integer.ToString(),
                    _                    => _number.ToString()
                };
            }
            else
            {
                var tag = _objectTag;
                Debug.Assert(tag is >= ObjectTag.String and <= ObjectTag.ClrTypes);

                switch (tag)
                {
                    case ObjectTag.String:
                        return $"\"{Unsafe.As<object, string>(ref objectOrPrimitiveTag)}\"";

                    case ObjectTag.Table:
                        return Unsafe.As<object, LuaTable>(ref objectOrPrimitiveTag).ToDebugString();

                    case ObjectTag.Function:
                        return Unsafe.As<object, LuaFunction>(ref objectOrPrimitiveTag).ToDebugString();

                    case ObjectTag.Thread:
                        return Unsafe.As<object, LuaThread>(ref objectOrPrimitiveTag).ToDebugString();

                    case ObjectTag.ClrObject:
                        return $"CLR object: {objectOrPrimitiveTag}";

                    default:
                        var types = Unsafe.As<object, Type[]>(ref objectOrPrimitiveTag);
                        return types.Length == 1 ?
                            $"CLR type: {types[0]}" :
                            $"CLR types: ({string.Join<Type>(", ", types)})";
                }
            }
        }

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
