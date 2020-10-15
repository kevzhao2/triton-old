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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Triton.Lua;
using static Triton.Lua.LuaType;

namespace Triton
{
    /// <summary>
    /// Represents a Lua value as a tagged union.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe readonly struct LuaValue : IEquatable<LuaValue>
    {
        private enum PrimitiveType
        {
            Boolean,
            Pointer,
            Integer,
            Number
        }

        private enum ObjectType
        {
            String,
            LuaObject,
            ClrTypes,
            ClrObject
        }

        private sealed record PrimitiveTag(PrimitiveType Type);

        private static readonly PrimitiveTag _booleanTag = new(PrimitiveType.Boolean);
        private static readonly PrimitiveTag _pointerTag = new(PrimitiveType.Pointer);
        private static readonly PrimitiveTag _integerTag = new(PrimitiveType.Integer);
        private static readonly PrimitiveTag _numberTag = new(PrimitiveType.Number);

        [FieldOffset(0)] private readonly bool _boolean;
        [FieldOffset(0)] private readonly IntPtr _pointer;
        [FieldOffset(0)] private readonly long _integer;
        [FieldOffset(0)] private readonly double _number;
        [FieldOffset(0)] private readonly ObjectType _objectType;
        [FieldOffset(8)] private readonly object? _objectOrTag;

        /// <summary>
        /// Gets the nil value.
        /// </summary>
        /// <value>The nil value.</value>
        public static LuaValue Nil => default;

        /// <summary>
        /// Gets a value indicating whether the Lua value is nil.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is nil; otherwise, <see langword="false"/>.</value>
        public bool IsNil => _objectOrTag is null;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a boolean.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a boolean; otherwise, <see langword="false"/>.</value>
        public bool IsBoolean => ReferenceEquals(_objectOrTag, _booleanTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is a pointer.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a pointer; otherwise, <see langword="false"/>.</value>
        public bool IsPointer => ReferenceEquals(_objectOrTag, _pointerTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is an integer.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is an integer; otherwise, <see langword="false"/>.</value>
        public bool IsInteger => ReferenceEquals(_objectOrTag, _integerTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is a number.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a number; otherwise, <see langword="false"/>.</value>
        public bool IsNumber => ReferenceEquals(_objectOrTag, _numberTag);

        /// <summary>
        /// Gets a value indicating whether the Lua value is a string.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a string; otherwise, <see langword="false"/>.</value>
        public bool IsString => _objectType == ObjectType.String && _objectOrTag is string;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a Lua object.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a Lua object; otherwise, <see langword="false"/>.</value>
        public bool IsLuaObject => _objectType == ObjectType.LuaObject && _objectOrTag is LuaObject;

        /// <summary>
        /// Gets a value indicating whether the Lua value is CLR types.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is CLR types; otherwise, <see langword="false"/>.</value>
        public bool IsClrTypes => _objectType == ObjectType.ClrTypes && _objectOrTag is IReadOnlyList<Type>;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR object.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a CLR object; otherwise, <see langword="false"/>.</value>
        public bool IsClrObject => _objectType == ObjectType.ClrObject && !IsNil && !(_objectOrTag is PrimitiveTag);

        /// <summary>
        /// Creates a Lua value from the given object.
        /// </summary>
        /// <param name="obj">The object to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromObject(object? obj) => obj switch
        {
            null             => Nil,
            bool boolean     => FromBoolean(boolean),
            IntPtr pointer   => FromPointer(pointer),
            sbyte integer    => FromInteger(integer),
            byte integer     => FromInteger(integer),
            short integer    => FromInteger(integer),
            ushort integer   => FromInteger(integer),
            int integer      => FromInteger(integer),
            uint integer     => FromInteger(integer),
            long integer     => FromInteger(integer),
            ulong integer    => FromInteger((long)integer),
            float number     => FromNumber(number),
            double number    => FromNumber(number),
            char ch          => FromString(ch.ToString()),
            string str       => FromString(str),
            LuaObject luaObj => FromLuaObject(luaObj),
            _                => FromClrObject(obj),
        };

        /// <summary>
        /// Creates a Lua value from the given boolean.
        /// </summary>
        /// <param name="boolean">The boolean to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromBoolean(bool boolean)
        {
            FromBoolean(boolean, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given pointer.
        /// </summary>
        /// <param name="pointer">The pointer to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromPointer(IntPtr pointer)
        {
            FromPointer(pointer, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given integer.
        /// </summary>
        /// <param name="integer">The integer to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromInteger(long integer)
        {
            FromInteger(integer, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given number.
        /// </summary>
        /// <param name="number">The number to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromNumber(double number)
        {
            FromNumber(number, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given string.
        /// </summary>
        /// <param name="str">The string to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromString(string? str)
        {
            FromString(str, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given Lua object.
        /// </summary>
        /// <param name="obj">The Lua object to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromLuaObject(LuaObject? obj)
        {
            FromLuaObject(obj, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given CLR types.
        /// </summary>
        /// <param name="types">The CLR types to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="types"/> contains <see langword="null"/> or two types with the same generic arity.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="types"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrTypes(IReadOnlyList<Type> types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (types.Any(t => t is null))
            {
                throw new ArgumentException("Types contains null", nameof(types));
            }

            if (types.Select(t => t.GetGenericArguments().Length).Distinct().Count() != types.Count)
            {
                throw new ArgumentException("Types contains two types with the same generic arity", nameof(types));
            }

            FromClrEntity(types, isTypes: true, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given CLR object.
        /// </summary>
        /// <param name="obj">The CLR object to create a Lua value from.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrObject(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            FromClrEntity(obj, isTypes: false, out var value);
            return value;
        }

        internal static void FromLua(lua_State* state, int index, LuaType type, out LuaValue value)
        {
            switch (type)
            {
                case LUA_TBOOLEAN:
                    FromBoolean(lua_toboolean(state, index), out value);
                    break;

                case LUA_TLIGHTUSERDATA:
                    FromPointer((IntPtr)lua_topointer(state, index), out value);
                    break;

                case LUA_TNUMBER:
                    if (lua_isinteger(state, index))
                    {
                        FromInteger(lua_tointeger(state, index), out value);
                    }
                    else
                    {
                        FromNumber(lua_tonumber(state, index), out value);
                    }
                    break;

                case LUA_TSTRING:
                    FromString(lua_tostring(state, index), out value);
                    break;

                case LUA_TTABLE:
                case LUA_TFUNCTION:
                case LUA_TTHREAD:
                    var environment = lua_getenvironment(state);

                    FromLuaObject(environment.LoadLuaObject(state, index, type), out value);
                    break;

                case LUA_TUSERDATA:
                    var ptr = *(nint*)lua_topointer(state, index);
                    var entity = GCHandle.FromIntPtr(ptr & ~1).Target!;
                    FromClrEntity(entity, isTypes: (ptr & 1) != 0, out value);
                    break;

                default:
                    FromNil(out value);
                    break;
            }
        }

        private static void FromNil(out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectOrTag) = null;
        }

        private static void FromBoolean(bool boolean, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._boolean) = boolean;
            Unsafe.AsRef(in value._objectOrTag) = _booleanTag;
        }

        private static void FromPointer(IntPtr pointer, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._pointer) = pointer;
            Unsafe.AsRef(in value._objectOrTag) = _pointerTag;
        }

        private static void FromInteger(long integer, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._integer) = integer;
            Unsafe.AsRef(in value._objectOrTag) = _integerTag;
        }

        private static void FromNumber(double number, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._number) = number;
            Unsafe.AsRef(in value._objectOrTag) = _numberTag;
        }

        private static void FromString(string? str, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = ObjectType.String;
            Unsafe.AsRef(in value._objectOrTag) = str;
        }

        private static void FromLuaObject(LuaObject? obj, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = ObjectType.LuaObject;
            Unsafe.AsRef(in value._objectOrTag) = obj;
        }

        private static void FromClrEntity(object entity, bool isTypes, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = isTypes ? ObjectType.ClrTypes : ObjectType.ClrObject;
            Unsafe.AsRef(in value._objectOrTag) = entity;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LuaValue value && Equals(value);

        /// <inheritdoc cref="IEquatable{LuaValue}.Equals(LuaValue)"/>
        public bool Equals(in LuaValue other) => Equals(_objectOrTag, other._objectOrTag) && _objectOrTag switch
        {
            null => true,
            PrimitiveTag(var type) => type switch
            {
                PrimitiveType.Boolean => _boolean == other._boolean,
                PrimitiveType.Pointer => _pointer == other._pointer,
                PrimitiveType.Integer => _integer == other._integer,
                _                     => _number == other._number
            },
            _ => _objectType == other._objectType
        };

        /// <inheritdoc/>
        public override int GetHashCode() => _objectOrTag switch
        {
            null => 0,
            PrimitiveTag(var type) => type switch
            {
                PrimitiveType.Boolean => HashCode.Combine(_boolean),
                PrimitiveType.Pointer => HashCode.Combine(_pointer),
                PrimitiveType.Integer => HashCode.Combine(_integer),
                _                     => HashCode.Combine(_number)
            },
            _ => HashCode.Combine(_objectType, _objectOrTag)
        };

        /// <summary>
        /// Converts the Lua object into an object.
        /// </summary>
        /// <returns>The resulting object.</returns>
        public object? ToObject() => _objectOrTag switch
        {
            PrimitiveTag(var type) => type switch
            {
                PrimitiveType.Boolean => _boolean,
                PrimitiveType.Pointer => _pointer,
                PrimitiveType.Integer => _integer,
                _                     => _number
            },
            _ => _objectOrTag
        };

        /// <summary>
        /// Converts the Lua value into a boolean.
        /// </summary>
        /// <returns>The resulting boolean.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a boolean.</exception>
        public bool ToBoolean() => IsBoolean ? _boolean : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a pointer.
        /// </summary>
        /// <returns>The resulting pointer.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a pointer.</exception>
        public IntPtr ToPointer() => IsPointer ? _pointer : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into an integer.
        /// </summary>
        /// <returns>The resulting integer.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not an integer.</exception>
        public long ToInteger() => IsInteger ? _integer : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a number.
        /// </summary>
        /// <returns>The resulting number.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a number.</exception>
        public double ToNumber() => IsNumber ? _number : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a string.
        /// </summary>
        /// <returns>The resulting string.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a string.</exception>
        public new string ToString() =>
            (_objectType == ObjectType.String && _objectOrTag is string str) ? str : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a Lua object.
        /// </summary>
        /// <returns>The resulting Lua object.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a Lua object.</exception>
        public LuaObject ToLuaObject() =>
            (_objectType == ObjectType.LuaObject && _objectOrTag is LuaObject obj) ?
                obj : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into CLR types.
        /// </summary>
        /// <returns>The resulting CLR types.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not CLR types.</exception>
        public IReadOnlyList<Type> ToClrTypes() =>
            (_objectType == ObjectType.ClrTypes && _objectOrTag is IReadOnlyList<Type> types) ?
                types : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a CLR object.
        /// </summary>
        /// <returns>The resulting CLR object.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a CLR object.</exception>
        public object ToClrObject() => IsClrObject ? _objectOrTag! : throw new InvalidCastException();

        internal void Push(lua_State* state)
        {
            switch (_objectOrTag)
            {
                case null:
                    lua_pushnil(state);
                    break;

                case PrimitiveTag(var type):
                    switch (type)
                    {
                        case PrimitiveType.Boolean:
                            lua_pushboolean(state, _boolean);
                            break;

                        case PrimitiveType.Pointer:
                            lua_pushlightuserdata(state, _pointer);
                            break;

                        case PrimitiveType.Integer:
                            lua_pushinteger(state, _integer);
                            break;

                        default:
                            lua_pushnumber(state, _number);
                            break;
                    }
                    break;

                default:
                    switch (_objectType)
                    {
                        case ObjectType.String:
                            lua_pushstring(state, (string)_objectOrTag);
                            break;

                        case ObjectType.LuaObject:
                            ((LuaObject)_objectOrTag).Push(state);
                            break;

                        default:
                            var environment = lua_getenvironment(state);

                            environment.PushClrEntity(state, _objectOrTag, _objectType == ObjectType.ClrTypes);
                            break;
                    }
                    break;
            }
        }

        [ExcludeFromCodeCoverage]
        bool IEquatable<LuaValue>.Equals(LuaValue other) => Equals(other);

        /// <summary>
        /// Determines whether two Lua values are equal.
        /// </summary>
        /// <param name="left">The left Lua value to compare.</param>
        /// <param name="right">The right Lua value to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two Lua values are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(in LuaValue left, in LuaValue right) => left.Equals(right);

        /// <summary>
        /// Determines whether two Lua values are not equal.
        /// </summary>
        /// <param name="left">The left Lua value to compare.</param>
        /// <param name="right">The right Lua value to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the two Lua values are not equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(in LuaValue left, in LuaValue right) => !left.Equals(right);

        /// <summary>
        /// Converts the given boolean into a Lua value.
        /// </summary>
        /// <param name="boolean">The boolean to convert.</param>
        public static implicit operator LuaValue(bool boolean) => FromBoolean(boolean);

        /// <summary>
        /// Converts the given pointer into a Lua value.
        /// </summary>
        /// <param name="pointer">The pointer to convert.</param>
        public static implicit operator LuaValue(IntPtr pointer) => FromPointer(pointer);

        /// <summary>
        /// Converts the given integer into a Lua value.
        /// </summary>
        /// <param name="integer">The integer to convert.</param>
        public static implicit operator LuaValue(long integer) => FromInteger(integer);

        /// <summary>
        /// Converts the given number into a Lua value.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        public static implicit operator LuaValue(double number) => FromNumber(number);

        /// <summary>
        /// Converts the given string into a Lua value.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        public static implicit operator LuaValue(string? str) => FromString(str);

        /// <summary>
        /// Converts the given Lua object into a Lua value.
        /// </summary>
        /// <param name="obj">The Lua object to convert.</param>
        public static implicit operator LuaValue(LuaObject? obj) => FromLuaObject(obj);

        /// <summary>
        /// Converts the given Lua value into a boolean.
        /// </summary>
        /// <param name="value">The Lua value to convert.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a boolean.</exception>
        public static explicit operator bool(in LuaValue value) => value.ToBoolean();

        /// <summary>
        /// Converts the given Lua value into a pointer.
        /// </summary>
        /// <param name="value">The Lua value to convert.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a pointer.</exception>
        public static explicit operator IntPtr(in LuaValue value) => value.ToPointer();

        /// <summary>
        /// Converts the given Lua value into an integer.
        /// </summary>
        /// <param name="value">The Lua value to convert.</param>
        /// <exception cref="InvalidCastException">The Lua value is not an integer.</exception>
        public static explicit operator long(in LuaValue value) => value.ToInteger();

        /// <summary>
        /// Converts the given Lua value into a number.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a number.</exception>
        public static explicit operator double(in LuaValue value) => value.ToNumber();

        /// <summary>
        /// Converts the given Lua value into a string.
        /// </summary>
        /// <param name="value">The Lua value to convert.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a string.</exception>
        public static explicit operator string(in LuaValue value) => value.ToString();

        /// <summary>
        /// Converts the given Lua value into a Lua object.
        /// </summary>
        /// <param name="value">The Lua value to convert.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a Lua object.</exception>
        public static explicit operator LuaObject(in LuaValue value) => value.ToLuaObject();
    }
}
