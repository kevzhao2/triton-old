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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Triton.Interop;

namespace Triton
{
    /// <summary>
    /// Represents a Lua value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct LuaValue : IEquatable<LuaValue>
    {
        /// <summary>
        /// Tags a Lua value as a primitive.
        /// </summary>
        internal sealed class PrimitiveTag
        {
            internal PrimitiveTag(PrimitiveType primitiveType)
            {
                PrimitiveType = primitiveType;
            }

            /// <summary>
            /// Gets the primitive type.
            /// </summary>
            public PrimitiveType PrimitiveType { get; }
        }

        /// <summary>
        /// Specifies a Lua primitive: a boolean, light userdata, integer, or number.
        /// </summary>
        internal enum PrimitiveType
        {
            Boolean,
            LightUserdata,
            Integer,
            Number
        }

        /// <summary>
        /// Specifies a Lua object: a string, Lua object, or CLR entity.
        /// </summary>
        internal enum ObjectType
        {
            String,
            LuaObject,
            ClrEntity
        }

        private static readonly PrimitiveTag _booleanTag = new PrimitiveTag(PrimitiveType.Boolean);
        private static readonly PrimitiveTag _lightUserdataTag = new PrimitiveTag(PrimitiveType.LightUserdata);
        private static readonly PrimitiveTag _integerTag = new PrimitiveTag(PrimitiveType.Integer);
        private static readonly PrimitiveTag _numberTag = new PrimitiveTag(PrimitiveType.Number);

        // These fields are internal to centralize logic inside of `LuaEnvironment`.
        //

        [FieldOffset(0)] internal readonly bool _boolean;
        [FieldOffset(0)] internal readonly IntPtr _lightUserdata;
        [FieldOffset(0)] internal readonly long _integer;
        [FieldOffset(0)] internal readonly double _number;
        [FieldOffset(0)] internal readonly ObjectType _objectType;
        [FieldOffset(8)] internal readonly object? _objectOrTag;

        /// <summary>
        /// Gets the nil value.
        /// </summary>
        public static LuaValue Nil => default;

        /// <summary>
        /// Gets a value indicating whether the Lua value is nil.
        /// </summary>
        public bool IsNil => _objectOrTag is null;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a primitive.
        /// </summary>
        public bool IsPrimitive => _objectOrTag is PrimitiveTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a boolean.
        /// </summary>
        public bool IsBoolean => _objectOrTag == _booleanTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a light userdata.
        /// </summary>
        public bool IsLightUserdata => _objectOrTag == _lightUserdataTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is an integer.
        /// </summary>
        public bool IsInteger => _objectOrTag == _integerTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a number.
        /// </summary>
        public bool IsNumber => _objectOrTag == _numberTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is an object.
        /// </summary>
        public bool IsObject => !IsNil && !IsPrimitive;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a string.
        /// </summary>
        public bool IsString => _objectType == ObjectType.String && _objectOrTag is string;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a Lua object.
        /// </summary>
        public bool IsLuaObject => _objectType == ObjectType.LuaObject && _objectOrTag is LuaObject;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR type.
        /// </summary>
        public bool IsClrType => _objectOrTag is ProxyClrType;

        /// <summary>
        /// Gets a value indicating whether the Lua value is generic CLR types.
        /// </summary>
        public bool IsGenericClrTypes => _objectOrTag is ProxyGenericClrTypes;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR object.
        /// </summary>
        public bool IsClrObject => _objectType == ObjectType.ClrEntity && !IsPrimitive && !IsClrType && !IsGenericClrTypes;

        /// <summary>
        /// Creates a Lua value from the given boolean.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromBoolean(bool boolean)
        {
            FromBoolean(boolean, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given light userdata.
        /// </summary>
        /// <param name="lightUserdata">The light userdata.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromLightUserdata(IntPtr lightUserdata)
        {
            FromLightUserdata(lightUserdata, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given integer.
        /// </summary>
        /// <param name="integer">The integer.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromInteger(long integer)
        {
            FromInteger(integer, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromNumber(double number)
        {
            FromNumber(number, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromString(string? str)
        {
            FromString(str, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given Lua object.
        /// </summary>
        /// <param name="obj">The Lua object.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromLuaObject(LuaObject? obj)
        {
            FromLuaObject(obj, out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given CLR type.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentException"><paramref name="type"/> is generic.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Type is generic", nameof(type));
            }

            FromClrEntity(new ProxyClrType(type), out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given generic CLR types.
        /// </summary>
        /// <param name="types">The generic CLR types.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="types"/> contains <see langword="null"/> or more than one non-generic type.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="types"/> is <see langword="null"/>.</exception>
        public static LuaValue FromGenericClrTypes(params Type[] types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (types.Any(t => t is null))
            {
                throw new ArgumentException("Types contains null", nameof(types));
            }

            if (types.Count(t => !t.IsGenericTypeDefinition) > 1)
            {
                throw new ArgumentException("Types contains more than one non-generic type", nameof(types));
            }

            FromClrEntity(new ProxyGenericClrTypes(types), out var value);
            return value;
        }

        /// <summary>
        /// Creates a Lua value from the given CLR object.
        /// </summary>
        /// <param name="obj">The CLR object.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrObject(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            FromClrEntity(obj, out var value);
            return value;
        }

        // The following methods employ manual RVO (return value optimization) using out variables and bypass
        // initialization using `Unsafe.SkipInit` for maximum performance.
        //

        /// <summary>
        /// Creates a Lua value representing nil.
        /// </summary>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromNil(out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectOrTag) = null;
        }

        /// <summary>
        /// Creates a Lua value from the given boolean.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromBoolean(bool boolean, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._boolean) = boolean;
            Unsafe.AsRef(in value._objectOrTag) = _booleanTag;
        }

        /// <summary>
        /// Creates a Lua value from the given light userdata.
        /// </summary>
        /// <param name="lightUserdata">The light userdata.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromLightUserdata(IntPtr lightUserdata, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._lightUserdata) = lightUserdata;
            Unsafe.AsRef(in value._objectOrTag) = _lightUserdataTag;
        }

        /// <summary>
        /// Creates a Lua value from the given integer.
        /// </summary>
        /// <param name="integer">The integer.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromInteger(long integer, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._integer) = integer;
            Unsafe.AsRef(in value._objectOrTag) = _integerTag;
        }

        /// <summary>
        /// Creates a Lua value from the given number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromNumber(double number, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._number) = number;
            Unsafe.AsRef(in value._objectOrTag) = _numberTag;
        }

        /// <summary>
        /// Creates a Lua value from the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromString(string? str, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = ObjectType.String;
            Unsafe.AsRef(in value._objectOrTag) = str;
        }

        /// <summary>
        /// Creates a Lua value from the given Lua object.
        /// </summary>
        /// <param name="obj">The Lua object.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromLuaObject(LuaObject? obj, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = ObjectType.LuaObject;
            Unsafe.AsRef(in value._objectOrTag) = obj;
        }

        /// <summary>
        /// Creates a Lua value from the given CLR entity.
        /// </summary>
        /// <param name="entity">The CLR entity.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromClrEntity(object entity, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = ObjectType.ClrEntity;
            Unsafe.AsRef(in value._objectOrTag) = entity;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LuaValue value && Equals(value);

        /// <inheritdoc cref="IEquatable{LuaValue}.Equals(LuaValue)"/>
        public bool Equals(in LuaValue other) =>
            Equals(_objectOrTag, other._objectOrTag) && _objectOrTag switch
            {
                null => true,
                PrimitiveTag { PrimitiveType: var primitiveType } => primitiveType switch
                {
                    PrimitiveType.Boolean       => _boolean == other._boolean,
                    PrimitiveType.LightUserdata => _lightUserdata == other._lightUserdata,
                    PrimitiveType.Integer       => _integer == other._integer,
                    _                           => _number == other._number
                },
                _ => _objectType == other._objectType
            };

        /// <inheritdoc/>
        public override int GetHashCode() => _objectOrTag switch
        {
            null => 0,
            PrimitiveTag { PrimitiveType: var primitiveType } => primitiveType switch
            {
                PrimitiveType.Boolean       => _boolean.GetHashCode(),
                PrimitiveType.LightUserdata => _lightUserdata.GetHashCode(),
                PrimitiveType.Integer       => _integer.GetHashCode(),
                _                           => _number.GetHashCode()
            },
            _ => HashCode.Combine(_objectType, _objectOrTag)
        };

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string ToString() => _objectOrTag switch
        {
            null => "<nil>",
            PrimitiveTag { PrimitiveType: var primitiveType } => primitiveType switch
            {
                PrimitiveType.Boolean       => $"<boolean: {_boolean}>",
                PrimitiveType.LightUserdata => $"<light userdata: 0x{_lightUserdata.ToInt64():8}>",
                PrimitiveType.Integer       => $"<integer: {_integer}>",
                _                           => $"<number: {_number}>"
            },
            _ => _objectType switch
            {
                ObjectType.String       => $"<string: {_objectOrTag}",
                ObjectType.LuaObject => $"<Lua object: {_objectOrTag}>",
                _                       => _objectOrTag switch
                {
                    ProxyClrType _         => $"<CLR type: {_objectOrTag}>",
                    ProxyGenericClrTypes _ => $"<generic CLR types: {_objectOrTag}>",
                    _                      => $"<CLR object: {_objectOrTag}>"
                }
            }
        };

        /// <summary>
        /// Converts the Lua value into a boolean.
        /// </summary>
        /// <returns>The resulting boolean.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a boolean.</exception>
        public bool AsBoolean() => IsBoolean ? _boolean : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a light userdata.
        /// </summary>
        /// <returns>The resulting light userdata.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a light userdata.</exception>
        public IntPtr AsLightUserdata() => IsLightUserdata ? _lightUserdata : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into an integer.
        /// </summary>
        /// <returns>The resulting integer.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not an integer.</exception>
        public long AsInteger() => IsInteger ? _integer : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a number.
        /// </summary>
        /// <returns>The resulting number.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a number.</exception>
        public double AsNumber() => IsNumber ? _number : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a string.
        /// </summary>
        /// <returns>The resulting string.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a string.</exception>
        public string AsString() =>
            (_objectType == ObjectType.String && _objectOrTag is string str)
                ? str
                : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a Lua object.
        /// </summary>
        /// <returns>The resulting Lua object.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a Lua object.</exception>
        public LuaObject AsLuaObject() =>
            (_objectType == ObjectType.LuaObject && _objectOrTag is LuaObject obj)
                ? obj
                : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a CLR type.
        /// </summary>
        /// <returns>The resulting CLR type.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a CLR type.</exception>
        public Type AsClrType() =>
            _objectOrTag is ProxyClrType { Type: var type }
                ? type
                : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into generic CLR types.
        /// </summary>
        /// <returns>The resulting generic CLR types.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not generic CLR types.</exception>
        public Type[] AsGenericClrTypes() =>
            _objectOrTag is ProxyGenericClrTypes { Types: var types }
                ? types
                : throw new InvalidCastException();

        /// <summary>
        /// Converts the Lua value into a CLR object.
        /// </summary>
        /// <returns>The resulting CLR object.</returns>
        /// <exception cref="InvalidCastException">The Lua value is not a CLR object.</exception>
        public object AsClrObject() => IsClrObject ? _objectOrTag! : throw new InvalidCastException();

        [ExcludeFromCodeCoverage]
        bool IEquatable<LuaValue>.Equals(LuaValue other) => Equals(other);

        /// <summary>
        /// Returns a value indicating whether two Lua values are equal.
        /// </summary>
        /// <param name="left">The left Lua value.</param>
        /// <param name="right">The right Lua value.</param>
        /// <returns>
        /// <see langword="true"/> if the two Lua values are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(in LuaValue left, in LuaValue right) => left.Equals(right);

        /// <summary>
        /// Returns a value indicating whether two Lua values are not equal.
        /// </summary>
        /// <param name="left">The left Lua value.</param>
        /// <param name="right">The right Lua value.</param>
        /// <returns>
        /// <see langword="true"/> if the two Lua values are not equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(in LuaValue left, in LuaValue right) => !left.Equals(right);

        /// <summary>
        /// Converts the given boolean into a Lua value.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        public static implicit operator LuaValue(bool boolean) => FromBoolean(boolean);

        /// <summary>
        /// Converts the given light userdata into a Lua value.
        /// </summary>
        /// <param name="lightUserdata">The light userdata.</param>
        public static implicit operator LuaValue(IntPtr lightUserdata) => FromLightUserdata(lightUserdata);

        /// <summary>
        /// Converts the given integer into a Lua value.
        /// </summary>
        /// <param name="integer">The integer.</param>
        public static implicit operator LuaValue(long integer) => FromInteger(integer);

        /// <summary>
        /// Converts the given number into a Lua value.
        /// </summary>
        /// <param name="number">The number.</param>
        public static implicit operator LuaValue(double number) => FromNumber(number);

        /// <summary>
        /// Converts the given string into a Lua value.
        /// </summary>
        /// <param name="str">The string.</param>
        public static implicit operator LuaValue(string? str) => FromString(str);

        /// <summary>
        /// Converts the given Lua object into a Lua value.
        /// </summary>
        /// <param name="obj">The Lua object.</param>
        public static implicit operator LuaValue(LuaObject? obj) => FromLuaObject(obj);

        /// <summary>
        /// Cnoverts the given Lua value into a boolean.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a boolean.</exception>
        public static explicit operator bool(in LuaValue value) => value.AsBoolean();

        /// <summary>
        /// Cnoverts the given Lua value into a light userdata.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a light userdata.</exception>
        public static explicit operator IntPtr(in LuaValue value) => value.AsLightUserdata();

        /// <summary>
        /// Cnoverts the given Lua value into an integer.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not an integer.</exception>
        public static explicit operator long(in LuaValue value) => value.AsInteger();

        /// <summary>
        /// Cnoverts the given Lua value into a number.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a number.</exception>
        public static explicit operator double(in LuaValue value) => value.AsNumber();

        /// <summary>
        /// Cnoverts the given Lua value into a string.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a string.</exception>
        public static explicit operator string(in LuaValue value) => value.AsString();

        /// <summary>
        /// Cnoverts the given Lua value into a Lua object.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        /// <exception cref="InvalidCastException">The Lua value is not a Lua object.</exception>
        public static explicit operator LuaObject(in LuaValue value) => value.AsLuaObject();
    }
}
