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
using System.Runtime.InteropServices;

namespace Triton
{
    /// <summary>
    /// Represents a Lua value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct LuaValue : IEquatable<LuaValue>, IDisposable
    {
        internal class TypeTag
        {
        }

        internal static readonly TypeTag _booleanTag = new TypeTag();
        internal static readonly TypeTag _lightUserdataTag = new TypeTag();
        internal static readonly TypeTag _integerTag = new TypeTag();
        internal static readonly TypeTag _numberTag = new TypeTag();

        [FieldOffset(0)] internal readonly bool _boolean;
        [FieldOffset(0)] internal readonly IntPtr _lightUserdata;
        [FieldOffset(0)] internal readonly long _integer;
        [FieldOffset(0)] internal readonly double _number;

        [FieldOffset(8)] internal readonly object? _objectOrTag;

        // TODO: in .NET 5, use `Unsafe.SkipInit` for a small perf gain in constructor

        private LuaValue(bool value) : this()
        {
            _boolean = value;
            _objectOrTag = _booleanTag;
        }

        private LuaValue(IntPtr value) : this()
        {
            _lightUserdata = value;
            _objectOrTag = _lightUserdataTag;
        }

        private LuaValue(long value) : this()
        {
            _integer = value;
            _objectOrTag = _integerTag;
        }

        private LuaValue(double value) : this()
        {
            _number = value;
            _objectOrTag = _numberTag;
        }

        private LuaValue(long type, object? value) : this()
        {
            _integer = type;
            _objectOrTag = value;  // No null check here since it will be considered to be `nil`
        }

        /// <summary>
        /// Gets a Lua value representing the <see langword="nil"/> value.
        /// </summary>
        /// <value>A Lua value representing the <see langword="nil"/> value.</value>
        public static LuaValue Nil => default;

        /// <summary>
        /// Gets a value indicating whether the Lua value is <see langword="nil"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the Lua value is <see langword="nil"/>; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsNil => _objectOrTag is null;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a boolean.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a boolean; otherwise, <see langword="false"/>.</value>
        public bool IsBoolean => _objectOrTag == _booleanTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a light userdata.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the Lua value is a light userdata; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsLightUserdata => _objectOrTag == _lightUserdataTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is an integer.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is an integer; otherwise, <see langword="false"/>.</value>
        public bool IsInteger => _objectOrTag == _integerTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is an number.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is an number; otherwise, <see langword="false"/>.</value>
        public bool IsNumber => _objectOrTag == _numberTag;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a string.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a string; otherwise, <see langword="false"/>.</value>
        public bool IsString => _integer == 1 && _objectOrTag is string;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a Lua object.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a Lua object; otherwise, <see langword="false"/>.</value>
        public bool IsLuaObject => _integer == 2 && _objectOrTag is LuaObject;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR type.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a CLR type; otherwise, <see langword="false"/>.</value>
        public bool IsClrType => _integer == 3 && _objectOrTag is Type;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR object.
        /// </summary>
        /// <value><see langword="true"/> if the Lua value is a CLR object; otherwise, <see langword="false"/>.</value>
        public bool IsClrObject => _integer == 4 && !IsNil && !(_objectOrTag is TypeTag);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given object
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The object value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromObject(object? value) => value switch
        {
            null             => Nil,
            bool b           => FromBoolean(b),
            IntPtr p         => FromLightUserdata(p),
            sbyte i1         => FromInteger(i1),
            byte u1          => FromInteger(u1),
            short i2         => FromInteger(i2),
            ushort u2        => FromInteger(u2),
            int i4           => FromInteger(i4),
            uint u4          => FromInteger(u4),
            long i8          => FromInteger(i8),
            ulong u8         => FromInteger((long)u8),
            float r4         => FromNumber(r4),
            double r8        => FromNumber(r8),
            string s         => FromString(s),
            LuaObject luaObj => FromLuaObject(luaObj),
            _                => FromClrObject(value)
        };

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given boolean
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromBoolean(bool value) => new LuaValue(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given light userdata
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The light userdata value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromLightUserdata(IntPtr value) => new LuaValue(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given integer
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromInteger(long value) => new LuaValue(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given number
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The number value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromNumber(double value) => new LuaValue(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given string
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromString(string? value) => new LuaValue(1, value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given Lua object
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The Lua object value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromLuaObject(LuaObject? value) => new LuaValue(2, value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given CLR type
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The CLR type value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromClrType(Type? value) => new LuaValue(3, value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaValue"/> structure from the given CLR object
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The CLR object value.</param>
        /// <returns>A new instance of the <see cref="LuaValue"/> structure.</returns>
        public static LuaValue FromClrObject(object? value) => new LuaValue(4, value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LuaValue other && Equals(other);

        /// <inheritdoc cref="IEquatable{LuaValue}.Equals(LuaValue)"/>
        public bool Equals(in LuaValue other) => ((IEquatable<LuaValue>)this).Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _objectOrTag switch
        {
            null                                     => 0,
            _ when _objectOrTag == _booleanTag       => HashCode.Combine(_boolean),
            _ when _objectOrTag == _lightUserdataTag => HashCode.Combine(_lightUserdata),
            _ when _objectOrTag == _integerTag       => HashCode.Combine(_integer),
            _ when _objectOrTag == _numberTag        => HashCode.Combine(_number),
            _                                        => HashCode.Combine(_integer, _objectOrTag)
        };

        /// <summary>
        /// Returns a string that represents the Lua value.
        /// </summary>
        /// <returns>A string that represents the Lua value.</returns>
        [ExcludeFromCodeCoverage]
        public override string ToString() => _objectOrTag switch
        {
            null                                     => "<nil>",
            _ when _objectOrTag == _booleanTag       => $"<boolean: {_boolean}>",
            _ when _objectOrTag == _lightUserdataTag => $"<light userdata: 0x{_lightUserdata.ToInt64():x8}>",
            _ when _objectOrTag == _integerTag       => $"<integer: {_integer}>",
            _ when _objectOrTag == _numberTag        => $"<number: {_number}>",
            _ when _number == 1                      => $"<string: {_objectOrTag}>",
            _ when _number == 2                      => $"<Lua object: {_objectOrTag}>",
            _ when _number == 3                      => $"<CLR type: {_objectOrTag}>",
            _                                        => $"<CLR object: {_objectOrTag}>"
        };

        /// <inheritdoc/>
        public void Dispose() => (_objectOrTag as LuaObject)?.Dispose();

        /// <summary>
        /// Converts the Lua value into a boolean. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a boolean.</returns>
        public bool AsBoolean() => _boolean;

        /// <summary>
        /// Converts the Lua value into a light userdata. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a light userdata.</returns>
        public IntPtr AsLightUserdata() => _lightUserdata;

        /// <summary>
        /// Converts the Lua value into an integer. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as an integer.</returns>
        public long AsInteger() => _integer;

        /// <summary>
        /// Converts the Lua value into a number. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a number.</returns>
        public double AsNumber() => _number;

        /// <summary>
        /// Converts the Lua value into a string. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a string.</returns>
        public string? AsString() => _objectOrTag as string;

        /// <summary>
        /// Converts the Lua value into a Lua object. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a Lua object.</returns>
        public LuaObject? AsLuaObject() => _objectOrTag as LuaObject;

        /// <summary>
        /// Converts the Lua value into a CLR type. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a CLR type.</returns>
        public Type? AsClrType() => _objectOrTag as Type;

        /// <summary>
        /// Converts the Lua value into a CLR object. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a CLR object.</returns>
        public object? AsClrObject() => _objectOrTag;

        bool IEquatable<LuaValue>.Equals(LuaValue other)
        {
            if (_objectOrTag is null)
            {
                return other._objectOrTag is null;
            }

            if (!_objectOrTag.Equals(other._objectOrTag))
            {
                return false;
            }

            return _objectOrTag switch
            {
                _ when _objectOrTag == _booleanTag       => _boolean == other._boolean,
                _ when _objectOrTag == _lightUserdataTag => _lightUserdata == other._lightUserdata,
                _ when _objectOrTag == _integerTag       => _integer == other._integer,
                _ when _objectOrTag == _numberTag        => _number == other._number,
                _                                        => true
            };
        }

        /// <summary>
        /// Converts the given boolean <paramref name="value"/> into a Lua value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        public static implicit operator LuaValue(bool value) => FromBoolean(value);

        /// <summary>
        /// Converts the given light userdata <paramref name="value"/> into a Lua value.
        /// </summary>
        /// <param name="value">The light userdata value.</param>
        public static implicit operator LuaValue(IntPtr value) => FromLightUserdata(value);

        /// <summary>
        /// Converts the given integer <paramref name="value"/> into a Lua value.
        /// </summary>
        /// <param name="value">The integer value.</param>
        public static implicit operator LuaValue(long value) => FromInteger(value);

        /// <summary>
        /// Converts the given number <paramref name="value"/> into a Lua value.
        /// </summary>
        /// <param name="value">The number value.</param>
        public static implicit operator LuaValue(double value) => FromNumber(value);

        /// <summary>
        /// Converts the given string <paramref name="value"/> into a Lua value.
        /// </summary>
        /// <param name="value">The string value.</param>
        public static implicit operator LuaValue(string? value) => FromString(value);

        /// <summary>
        /// Converts the given Lua object <paramref name="value"/> into a Lua value.
        /// </summary>
        /// <param name="value">The Lua object value.</param>
        public static implicit operator LuaValue(LuaObject? value) => FromLuaObject(value);

        /// <summary>
        /// Converts the given Lua <paramref name="value"/> into a boolean. <i>This is unchecked!</i>
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator bool(in LuaValue value) => value.AsBoolean();
        
        /// <summary>
        /// Converts the given Lua <paramref name="value"/> into a light userdata. <i>This is unchecked!</i>
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator IntPtr(in LuaValue value) => value.AsLightUserdata();

        /// <summary>
        /// Converts the given Lua <paramref name="value"/> into an integer. <i>This is unchecked!</i>
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator long(in LuaValue value) => value.AsInteger();

        /// <summary>
        /// Converts the given Lua <paramref name="value"/> into a number. <i>This is unchecked!</i>
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator double(in LuaValue value) => value.AsNumber();

        /// <summary>
        /// Converts the given Lua <paramref name="value"/> into a string. <i>This is unchecked!</i>
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator string?(in LuaValue value) => value.AsString();

        /// <summary>
        /// Converts the given Lua <paramref name="value"/> into a Lua object. <i>This is unchecked!</i>
        /// </summary>
        /// <param name="value">The Lua value.</param>
        public static explicit operator LuaObject?(in LuaValue value) => value.AsLuaObject();
    }
}
