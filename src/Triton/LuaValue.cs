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
using System.Runtime.InteropServices;

namespace Triton
{
    /// <summary>
    /// Represents a Lua value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct LuaValue : IEquatable<LuaValue>
    {
        // Acts as a proxy around a CLR object. Signals that the instance members of a type can be accessed.
        // 
        internal class ClrObjectProxy
        {
            internal ClrObjectProxy(object @object)
            {
                Debug.Assert(@object != null);

                Object = @object;
            }

            internal object Object { get; }
        }

        // Acts as a proxy around a CLR type. Signals that the static members and constructors of a type can be
        // accessed.
        //
        internal class ClrTypeProxy
        {
            internal ClrTypeProxy(Type type)
            {
                Debug.Assert(type != null);
                Debug.Assert(!type.IsGenericParameter && !type.IsGenericTypeDefinition);

                Type = type;
            }

            internal Type Type { get; }
        }

        // Acts as a proxy around generic CLR types with the same name. Signals that the generic constructions can be
        // accessed (and if there is a nullary type included, static members and constructors of that type).
        //
        internal class ClrGenericTypesProxy
        {
            internal ClrGenericTypesProxy(Type[] types)
            {
                Debug.Assert(types.Length >= 1);
                Debug.Assert(types.Count(t => !t.IsGenericTypeDefinition) <= 1);

                Types = types;
            }

            internal Type[] Types { get; }
        }

        internal static readonly object _booleanTag = new object();
        internal static readonly object _lightUserdataTag = new object();
        internal static readonly object _integerTag = new object();
        internal static readonly object _numberTag = new object();

        [FieldOffset(0)] internal readonly bool _boolean;
        [FieldOffset(0)] internal readonly IntPtr _lightUserdata;
        [FieldOffset(0)] internal readonly long _integer;
        [FieldOffset(0)] internal readonly double _number;
        [FieldOffset(8)] internal readonly object? _objectOrTag;

        private LuaValue(bool boolean) : this()
        {
            _boolean = boolean;
            _objectOrTag = _booleanTag;
        }

        private LuaValue(IntPtr lightUserdata) : this()
        {
            _lightUserdata = lightUserdata;
            _objectOrTag = _lightUserdataTag;
        }

        private LuaValue(long integer) : this()
        {
            _integer = integer;
            _objectOrTag = _integerTag;
        }

        private LuaValue(double number) : this()
        {
            _number = number;
            _objectOrTag = _numberTag;
        }

        private LuaValue(object? obj) : this()
        {
            _objectOrTag = obj;
        }

        /// <summary>
        /// The Lua value representing the <see langword="nil"/> value.
        /// </summary>
        public static readonly LuaValue Nil = default;

        /// <summary>
        /// Gets a value indicating whether the Lua value is <see langword="nil"/>.
        /// </summary>
        public bool IsNil => _objectOrTag is null;

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
        /// Gets a value indicating whether the Lua value is a string.
        /// </summary>
        public bool IsString => _objectOrTag is string;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a Lua object.
        /// </summary>
        public bool IsLuaObject => _objectOrTag is LuaObject;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR type.
        /// </summary>
        public bool IsClrType => _objectOrTag is ClrTypeProxy;

        /// <summary>
        /// Gets a value indicating whether the Lua value is CLR generic types.
        /// </summary>
        public bool IsClrGenericTypes => _objectOrTag is ClrGenericTypesProxy;

        /// <summary>
        /// Gets a value indicating whether the Lua value is a CLR object.
        /// </summary>
        public bool IsClrObject => _objectOrTag is ClrObjectProxy;

        /// <summary>
        /// Creates a Lua value from the given <paramref name="boolean"/>.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromBoolean(bool boolean) => new LuaValue(boolean);

        /// <summary>
        /// Creates a Lua value from the given <paramref name="lightUserdata"/>.
        /// </summary>
        /// <param name="lightUserdata">The light userdata.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromLightUserdata(IntPtr lightUserdata) => new LuaValue(lightUserdata);

        /// <summary>
        /// Creates a Lua value from the given from the given <paramref name="integer"/>.
        /// </summary>
        /// <param name="integer">The integer.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromInteger(long integer) => new LuaValue(integer);

        /// <summary>
        /// Creates a Lua value from the given from the given <paramref name="number"/>.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromNumber(double number) => new LuaValue(number);

        /// <summary>
        /// Creates a Lua value from the given from the given <paramref name="str"/>.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromString(string? str) => new LuaValue(str);

        /// <summary>
        /// Creates a Lua value from the given from the given Lua <paramref name="object"/>.
        /// </summary>
        /// <param name="object">The Lua object.</param>
        /// <returns>The resulting Lua value.</returns>
        public static LuaValue FromLuaObject(LuaObject? @object) => new LuaValue(@object);

        /// <summary>
        /// Creates a Lua value from the given from the given CLR <paramref name="type"/>.
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

            if (type.IsGenericParameter || type.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Type is generic", nameof(type));
            }

            return new LuaValue(new ClrTypeProxy(type));
        }

        /// <summary>
        /// Creates a Lua value from the given from the given CLR generic <paramref name="types"/>.
        /// </summary>
        /// <param name="types">The CLR generic types.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="types"/> contains more than one non-generic type.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="types"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrGenericTypes(Type[] types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (types.Count(t => !t.IsGenericTypeDefinition) > 1)
            {
                throw new ArgumentException("Types contains more than one non-generic type", nameof(types));
            }

            return new LuaValue(new ClrGenericTypesProxy(types));
        }

        /// <summary>
        /// Creates a Lua value from the given from the given CLR <paramref name="object"/>.
        /// </summary>
        /// <param name="object">The CLR object.</param>
        /// <returns>The resulting Lua value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="object"/> is <see langword="null"/>.</exception>
        public static LuaValue FromClrObject(object @object)
        {
            if (@object is null)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            return new LuaValue(new ClrObjectProxy(@object));
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LuaValue other && Equals(other);

        /// <inheritdoc cref="IEquatable{LuaValue}.Equals(LuaValue)"/>
        public bool Equals(in LuaValue other)
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

        /// <inheritdoc/>
        public override int GetHashCode() => _objectOrTag switch
        {
            null                                     => 0,
            _ when _objectOrTag == _booleanTag       => _boolean.GetHashCode(),
            _ when _objectOrTag == _lightUserdataTag => _lightUserdata.GetHashCode(),
            _ when _objectOrTag == _integerTag       => _integer.GetHashCode(),
            _ when _objectOrTag == _numberTag        => _number.GetHashCode(),
            _                                        => _objectOrTag.GetHashCode(),
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
            _                                        => $"<object: {_objectOrTag}>"
        };

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
        public Type? AsClrType() => _objectOrTag is ClrTypeProxy { Type: var type } ? type : null;

        /// <summary>
        /// Converts the Lua value into CLR generic types. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as CLR generic types.</returns>
        public Type[]? AsClrGenericTypes() => _objectOrTag is ClrGenericTypesProxy { Types: var types } ? types : null;

        /// <summary>
        /// Converts the Lua value into a CLR object. <i>This is unchecked!</i>
        /// </summary>
        /// <returns>The Lua value as a CLR object.</returns>
        public object? AsClrObject() => _objectOrTag is ClrObjectProxy { Object: var @object } ? @object : null;

        [ExcludeFromCodeCoverage]
        bool IEquatable<LuaValue>.Equals(LuaValue other) => Equals(other);

        /// <summary>
        /// Returns a value indicating whether <paramref name="left"/> and <paramref name="right"/> are equal.
        /// </summary>
        /// <param name="left">The left Lua value.</param>
        /// <param name="right">The right Lua value.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(in LuaValue left, in LuaValue right) => left.Equals(right);

        /// <summary>
        /// Returns a value indicating whether <paramref name="left"/> and <paramref name="right"/> are not equal.
        /// </summary>
        /// <param name="left">The left Lua value.</param>
        /// <param name="right">The right Lua value.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(in LuaValue left, in LuaValue right) => !left.Equals(right);

        /// <summary>
        /// Converts the given <paramref name="boolean"/> into a Lua value.
        /// </summary>
        /// <param name="boolean">The boolean.</param>
        public static implicit operator LuaValue(bool boolean) => FromBoolean(boolean);

        /// <summary>
        /// Converts the given <paramref name="lightUserdata"/> into a Lua value.
        /// </summary>
        /// <param name="lightUserdata">The light userdata.</param>
        public static implicit operator LuaValue(IntPtr lightUserdata) => FromLightUserdata(lightUserdata);

        /// <summary>
        /// Converts the given <paramref name="integer"/> into a Lua value.
        /// </summary>
        /// <param name="integer">The integer.</param>
        public static implicit operator LuaValue(long integer) => FromInteger(integer);

        /// <summary>
        /// Converts the given <paramref name="number"/> into a Lua value.
        /// </summary>
        /// <param name="number">The number.</param>
        public static implicit operator LuaValue(double number) => FromNumber(number);

        /// <summary>
        /// Converts the given <paramref name="str"/> into a Lua value.
        /// </summary>
        /// <param name="str">The string.</param>
        public static implicit operator LuaValue(string? str) => FromString(str);

        /// <summary>
        /// Converts the given Lua <paramref name="obj"/> into a Lua value.
        /// </summary>
        /// <param name="obj">The Lua object.</param>
        public static implicit operator LuaValue(LuaObject? obj) => FromLuaObject(obj);

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
