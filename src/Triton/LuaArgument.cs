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
    [DebuggerDisplay("{ToDebugString(),nq}")]
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct LuaArgument
    {
        private enum PrimitiveTag
        {
            Boolean,
            Integer,
            Number
        }

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

        // At field offset 0, keep a union of (`bool`, `long`, `double`, `ObjectTag`) at field offset 0. This either
        // represents a primitive or helps identify an object.
        //
        // At field offset 8, keep either a boxed primitive tag or a object. This either identifies a primitive or
        // represents an object.
        //
        // All together, the `LuaArgument` structure can represent any Lua argument using just sixteen bytes, making it
        // quite efficient.
        //
        [FieldOffset(0)] private readonly bool      _boolean;
        [FieldOffset(0)] private readonly long      _integer;
        [FieldOffset(0)] private readonly double    _number;
        [FieldOffset(0)] private readonly ObjectTag _objectTag;

        [FieldOffset(8)] private readonly object?   _objectOrPrimitiveTag;

        #region constructors

        // These constructors are marked with `AggressiveInlining` to ensure that the `Unsafe.SkipInit` calls do not
        // block inlining.
        //
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
                int         integer  => FromInteger(integer),
                uint        integer  => FromInteger(integer),
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
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <see langword="null"/>.</exception>
        public static LuaArgument FromString(string? str)
        {
            if (str is null)
                ThrowHelper.ThrowArgumentNullException(nameof(str));

            return FromStringRelaxed(str);
        }

        /// <summary>
        /// Converts a table into an argument.
        /// </summary>
        /// <param name="table">The table to convert.</param>
        /// <returns>The resulting argument.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
        public static LuaArgument FromTable(LuaTable? table)
        {
            if (table is null)
                ThrowHelper.ThrowArgumentNullException(nameof(table));

            return FromTableRelaxed(table);
        }

        /// <summary>
        /// Converts a function into an argument.
        /// </summary>
        /// <param name="function">The function to convert.</param>
        /// <returns>The resulting argument.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
        public static LuaArgument FromFunction(LuaFunction? function)
        {
            if (function is null)
                ThrowHelper.ThrowArgumentNullException(nameof(function));

            return FromFunctionRelaxed(function);
        }

        /// <summary>
        /// Converts a thread into an argument.
        /// </summary>
        /// <param name="thread">The thread to convert.</param>
        /// <returns>The resulting argument.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="thread"/> is <see langword="null"/>.</exception>
        public static LuaArgument FromThread(LuaThread? thread)
        {
            if (thread is null)
                ThrowHelper.ThrowArgumentNullException(nameof(thread));

            return FromThreadRelaxed(thread);
        }

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

                // Do not allow types to have the same generic arity. The intent of accepting an array of types is to
                // allow for generic arity overloading: e.g., System.Task referring to either `Task` or `Task<>`.
                //
                var genericArity = type.GetGenericArguments().Length;
                if (genericArities.Contains(genericArity))
                    ThrowHelper.ThrowArgumentException(nameof(types), "Types contains two types with the same generic arity");

                genericArities.Add(genericArity);
            }

            return new(ObjectTag.ClrTypes, types);
        }

        private static LuaArgument FromStringRelaxed(string? str) => new(ObjectTag.String, str);

        private static LuaArgument FromTableRelaxed(LuaTable? table) => new(ObjectTag.Table, table);

        private static LuaArgument FromFunctionRelaxed(LuaFunction? function) => new(ObjectTag.Function, function);

        private static LuaArgument FromThreadRelaxed(LuaThread? thread) => new(ObjectTag.Thread, thread);

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
                // Using an unsafe cast to obtain the primitive tag results in optimal codegen.
                //
                // The if statements first check for integers and numbers since those are more common than booleans.
                //
                var tag = Unsafe.As<object, StrongBox<PrimitiveTag>>(ref objectOrPrimitiveTag).Value;
                if (tag == PrimitiveTag.Integer)
                    lua_pushinteger(state, _integer);
                else if (tag == PrimitiveTag.Number)
                    lua_pushnumber(state, _number);
                else  // `Boolean`
                    lua_pushboolean(state, _boolean);
            }
            else
            {
                // Using unsafe casts to obtain the relevant types results in optimal codegen.
                //
                // The switch statement's default case is CLR objects since those are more common than the other
                // objects.
                //
                switch (_objectTag)
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

                    default:  // `ClrObject`
                    {
                        var environment = lua_getenvironment(state);
                        environment.PushClrObject(state, objectOrPrimitiveTag);
                        break;
                    }

                    case ObjectTag.ClrTypes:
                    {
                        var environment = lua_getenvironment(state);
                        var types = Unsafe.As<object, Type[]>(ref objectOrPrimitiveTag);
                        environment.PushClrTypes(state, types);
                        break;
                    }
                }
            }
        }

        // Because this method is not on a hot path, it is optimized for readability instead.
        //
        [ExcludeFromCodeCoverage]
        internal string ToDebugString() =>
            _objectOrPrimitiveTag switch
            {
                null => "nil",
                StrongBox<PrimitiveTag> { Value: var tag } => tag switch
                {
                    PrimitiveTag.Boolean => _boolean.ToDebugString(),
                    PrimitiveTag.Integer => _integer.ToDebugString(),
                    _                    => _number.ToDebugString()
                },
                var obj => _objectTag switch
                {
                    ObjectTag.String   => ((string)obj).ToDebugString(),
                    ObjectTag.Table    => ((LuaTable)obj).ToDebugString(),
                    ObjectTag.Function => ((LuaFunction)obj).ToDebugString(),
                    ObjectTag.Thread   => ((LuaThread)obj).ToDebugString(),
                    ObjectTag.ClrTypes => ((Type[])obj).ToDebugString(),
                    _                  => obj.ToDebugString()
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
        public static implicit operator LuaArgument(string? str) => FromStringRelaxed(str);

        /// <summary>
        /// Converts a table into an argument.
        /// </summary>
        /// <param name="table">The table to convert.</param>
        public static implicit operator LuaArgument(LuaTable? table) => FromTableRelaxed(table);

        /// <summary>
        /// Converts a function into an argument.
        /// </summary>
        /// <param name="function">The function to convert.</param>
        public static implicit operator LuaArgument(LuaFunction? function) => FromFunctionRelaxed(function);

        /// <summary>
        /// Converts a thread into an argument.
        /// </summary>
        /// <param name="thread">The thread to convert.</param>
        public static implicit operator LuaArgument(LuaThread? thread) => FromThreadRelaxed(thread);

        #endregion
    }
}
