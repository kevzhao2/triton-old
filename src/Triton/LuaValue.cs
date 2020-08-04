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
        // Lua values are represented as a structure in the form of a _tagged union_. This allows for faster
        // type-checking and for fewer allocations compared to the alternative of representing Lua values with an
        // `object`.
        //
        // On x64, this structure is 16 bytes, and so RVO (return value optimization) is manually performed wherever
        // possible using out variables.
        //

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
        /// Specifies a Lua object: a string, Lua reference, or CLR entity.
        /// </summary>
        internal enum ObjectType
        {
            String,
            LuaReference,
            ClrEntity
        }

        private static readonly PrimitiveTag _booleanTag = new PrimitiveTag(PrimitiveType.Boolean);
        private static readonly PrimitiveTag _lightUserdataTag = new PrimitiveTag(PrimitiveType.LightUserdata);
        private static readonly PrimitiveTag _integerTag = new PrimitiveTag(PrimitiveType.Integer);
        private static readonly PrimitiveTag _numberTag = new PrimitiveTag(PrimitiveType.Number);

        [FieldOffset(0)] private readonly bool _boolean;
        [FieldOffset(0)] private readonly IntPtr _lightUserdata;
        [FieldOffset(0)] private readonly long _integer;
        [FieldOffset(0)] private readonly double _number;
        [FieldOffset(0)] private readonly ObjectType _objectType;
        [FieldOffset(8)] private readonly object? _objectOrTag;

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
        /// Gets a value indicating whether the Lua value is a Lua reference.
        /// </summary>
        public bool IsLuaReference => _objectType == ObjectType.LuaReference && _objectOrTag is LuaReference;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR type.
        /// </summary>
        public bool IsClrType => _objectOrTag is ProxyClrType;

        /// <summary>
        /// Gets a value indicating whether the Lua value is CLR generic types.
        /// </summary>
        public bool IsClrGenericTypes => _objectOrTag is ProxyClrGenericTypes;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR object.
        /// </summary>
        public bool IsClrObject => _objectType == ObjectType.ClrEntity && !IsPrimitive && !IsClrType && !IsClrGenericTypes;

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
        /// Creates a Lua value from the given Lua reference.
        /// </summary>
        /// <param name="reference">The Lua reference.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromLuaReference(LuaReference? reference)
        {
            FromLuaReference(reference, out var value);
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
        /// Creates a Lua value from the given CLR generic types.
        /// </summary>
        /// <param name="types">The CLR generic types.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="types"/> contains <see langword="null"/> or more than one non-generic type.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="types"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrGenericTypes(params Type[] types)
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

            FromClrEntity(new ProxyClrGenericTypes(types), out var value);
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
        /// Creates a Lua value from the given Lua reference.
        /// </summary>
        /// <param name="reference">The Lua reference.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromLuaReference(LuaReference? reference, out LuaValue value)
        {
            Unsafe.SkipInit(out value);
            Unsafe.AsRef(in value._objectType) = ObjectType.LuaReference;
            Unsafe.AsRef(in value._objectOrTag) = reference;
        }

        /// <summary>
        /// Creates a Lua value from the given CLR entity.
        /// </summary>
        /// <param name="entity">The CLR entity.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal static void FromClrEntity(object entity, out LuaValue value)
        {
            Debug.Assert(entity is { });

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
                ObjectType.LuaReference => $"<Lua reference: {_objectOrTag}>",
                _                       => _objectOrTag switch
                {
                    ProxyClrGenericTypes _ => $"<CLR generic types: {_objectOrTag}>",
                    ProxyClrType _         => $"<CLR type: {_objectOrTag}>",
                    _                      => $"<CLR object: {_objectOrTag}>"
                }
            }
        };

        /// <summary>
        /// Converts the Lua value into a boolean. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting boolean.</returns>
        public bool AsBoolean() => _boolean;

        /// <summary>
        /// Converts the Lua value into a light userdata. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting light userdata.</returns>
        public IntPtr AsLightUserdata() => _lightUserdata;

        /// <summary>
        /// Converts the Lua value into an integer. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting integer.</returns>
        public long AsInteger() => _integer;

        /// <summary>
        /// Converts the Lua value into a number. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting number.</returns>
        public double AsNumber() => _number;

        /// <summary>
        /// Converts the Lua value into a string. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting string.</returns>
        public string? AsString() => _objectOrTag as string;

        /// <summary>
        /// Converts the Lua value into a Lua reference. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting Lua reference.</returns>
        public LuaReference? AsLuaReference() => _objectOrTag as LuaReference;

        /// <summary>
        /// Converts the Lua value into a CLR type. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting CLR type.</returns>
        public Type? AsClrType() => _objectOrTag is ProxyClrType { Type: var type } ? type : null;

        /// <summary>
        /// Converts the Lua value into CLR generic types. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The resulting CLR generic types.</returns>
        public Type[]? AsClrGenericTypes() => _objectOrTag is ProxyClrGenericTypes { Types: var types } ? types : null;

        /// <summary>
        /// Gets the Lua value's object type. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value's object type.</returns>
        internal ObjectType GetObjectType() => _objectType;

        /// <summary>
        /// Gets the Lua value's object or tag.
        /// </summary>
        /// <returns>The Lua value's object or tag.</returns>
        internal object? GetObjectOrTag() => _objectOrTag;

        [ExcludeFromCodeCoverage]
        bool IEquatable<LuaValue>.Equals(LuaValue other) => Equals(other);

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
        /// Converts the given Lua reference into a Lua value.
        /// </summary>
        /// <param name="reference">The Lua reference.</param>
        public static implicit operator LuaValue(LuaReference? reference) => FromLuaReference(reference);

        /// <summary>
        /// Cnoverts the given Lua value into a boolean.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator bool(in LuaValue value) => value.AsBoolean();

        /// <summary>
        /// Cnoverts the given Lua value into a light userdata.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator IntPtr(in LuaValue value) => value.AsLightUserdata();

        /// <summary>
        /// Cnoverts the given Lua value into an integer.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator long(in LuaValue value) => value.AsInteger();

        /// <summary>
        /// Cnoverts the given Lua value into a number.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator double(in LuaValue value) => value.AsNumber();

        /// <summary>
        /// Cnoverts the given Lua value into a string.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator string?(in LuaValue value) => value.AsString();

        /// <summary>
        /// Cnoverts the given Lua value into a Lua reference.
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator LuaReference?(in LuaValue value) => value.AsLuaReference();
    }
}
