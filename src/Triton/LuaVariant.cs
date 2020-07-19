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
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a variant Lua value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct LuaVariant : IDisposable
    {
        private static readonly object _booleanTag = new object();
        private static readonly object _integerTag = new object();
        private static readonly object _numberTag = new object();

        [FieldOffset(0)] private readonly bool _boolean;
        [FieldOffset(0)] private readonly long _integer;
        [FieldOffset(0)] private readonly double _number;

        // Either a reference or a type tag. This allows `LuaVariant` to use only 16 bytes, which means that the
        // structure can be copied much more efficiently.
        [FieldOffset(8)] private readonly object? _objectOrTag;

        // TODO: in .NET 5, use `Unsafe.SkipInit` for a small perf gain in constructor

        private LuaVariant(bool value) : this()
        {
            _boolean = value;
            _objectOrTag = _booleanTag;
        }

        private LuaVariant(long value) : this()
        {
            _integer = value;
            _objectOrTag = _integerTag;
        }

        private LuaVariant(double value) : this()
        {
            _number = value;
            _objectOrTag = _numberTag;
        }

        private LuaVariant(object? value) : this()  // Shared ctor for all reference types
        {
            _objectOrTag = value;  // No null check here since it will be considered to be `nil`
        }

        /// <summary>
        /// Gets a variant representing the <see langword="nil"/> value.
        /// </summary>
        /// <value>A variant representing the <see langword="nil"/> value.</value>
        public static LuaVariant Nil => default;

        /// <summary>
        /// Gets a value indicating whether the variant is <see langword="nil"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the variant is <see langword="nil"/>; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsNil => _objectOrTag is null;

        /// <summary>
        /// Gets a value indicating whether the variant is a boolean.
        /// </summary>
        /// <value><see langword="true"/> if the variant is a boolean; otherwise, <see langword="false"/>.</value>
        public bool IsBoolean => _objectOrTag == _booleanTag;

        /// <summary>
        /// Gets a value indicating whether the variant is an integer.
        /// </summary>
        /// <value><see langword="true"/> if the variant is an integer; otherwise, <see langword="false"/>.</value>
        public bool IsInteger => _objectOrTag == _integerTag;

        /// <summary>
        /// Gets a value indicating whether the variant is an number.
        /// </summary>
        /// <value><see langword="true"/> if the variant is an number; otherwise, <see langword="false"/>.</value>
        public bool IsNumber => _objectOrTag == _numberTag;

        /// <summary>
        /// Gets a value indicating whether the variant is a string.
        /// </summary>
        /// <value><see langword="true"/> if the variant is a string; otherwise, <see langword="false"/>.</value>
        public bool IsString => _objectOrTag is string;

        /// <summary>
        /// Gets a value indicating whether the variant is a Lua object.
        /// </summary>
        /// <value><see langword="true"/> if the variant is a Lua object; otherwise, <see langword="false"/>.</value>
        public bool IsLuaObject => _objectOrTag is LuaObject;

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given boolean
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromBoolean(bool value) => new LuaVariant(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given integer
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromInteger(long value) => new LuaVariant(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given number
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The number value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromNumber(double value) => new LuaVariant(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given string
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromString(string? value) => new LuaVariant(value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given Lua object
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The Lua object value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromLuaObject(LuaObject? value) => new LuaVariant(value);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_objectOrTag is LuaObject obj)
            {
                obj.Dispose();
            }
        }

        /// <summary>
        /// Converts the variant into a boolean.
        /// </summary>
        /// <returns>The variant as a boolean.</returns>
        public bool AsBoolean()
        {
            Debug.Assert(_objectOrTag == _booleanTag);

            return _boolean;
        }

        /// <summary>
        /// Converts the variant into an integer.
        /// </summary>
        /// <returns>The variant as an integer.</returns>
        public long AsInteger()
        {
            Debug.Assert(_objectOrTag == _integerTag);

            return _integer;
        }

        /// <summary>
        /// Converts the variant into a number.
        /// </summary>
        /// <returns>The variant as a number.</returns>
        public double AsNumber()
        {
            Debug.Assert(_objectOrTag == _numberTag);

            return _number;
        }

        /// <summary>
        /// Converts the variant into a string.
        /// </summary>
        /// <returns>The variant as a string.</returns>
        public string? AsString()
        {
            Debug.Assert(_objectOrTag is null || _objectOrTag is string);

            return _objectOrTag as string;
        }

        /// <summary>
        /// Converts the variant into a Lua object.
        /// </summary>
        /// <returns>The variant as a Lua object.</returns>
        public LuaObject? AsLuaObject()
        {
            Debug.Assert(_objectOrTag is null || _objectOrTag is LuaObject);

            return _objectOrTag as LuaObject;
        }

        /// <summary>
        /// Pushes the variant onto the stack of the given Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        internal void Push(IntPtr state)
        {
            Debug.Assert(state != IntPtr.Zero);
            Debug.Assert(lua_checkstack(state, 1));

            if (_objectOrTag is null)
            {
                lua_pushnil(state);
            }
            else if (_objectOrTag == _booleanTag)
            {
                lua_pushboolean(state, _boolean);
            }
            else if (_objectOrTag == _integerTag)
            {
                lua_pushinteger(state, _integer);
            }
            else if (_objectOrTag == _numberTag)
            {
                lua_pushnumber(state, _number);
            }
            else if (_objectOrTag is string s)
            {
                lua_pushstring(state, s);
            }
            else if (_objectOrTag is LuaObject obj)
            {
                obj.Push(state);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Converts the given boolean <paramref name="value"/> into a Lua variant.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        public static implicit operator LuaVariant(bool value) => new LuaVariant(value);

        /// <summary>
        /// Converts the given integer <paramref name="value"/> into a Lua variant.
        /// </summary>
        /// <param name="value">The integer value.</param>
        public static implicit operator LuaVariant(long value) => new LuaVariant(value);

        /// <summary>
        /// Converts the given number <paramref name="value"/> into a Lua variant.
        /// </summary>
        /// <param name="value">The number value.</param>
        public static implicit operator LuaVariant(double value) => new LuaVariant(value);

        /// <summary>
        /// Converts the given string <paramref name="value"/> into a Lua variant.
        /// </summary>
        /// <param name="value">The string value.</param>
        public static implicit operator LuaVariant(string? value) => new LuaVariant(value);

        /// <summary>
        /// Converts the given Lua object <paramref name="value"/> into a Lua variant.
        /// </summary>
        /// <param name="value">The Lua object value.</param>
        public static implicit operator LuaVariant(LuaObject? value) => new LuaVariant(value);

        /// <summary>
        /// Converts the given Lua <paramref name="variant"/> into a boolean.
        /// </summary>
        /// <param name="variant">The Lua variant.</param>
        public static explicit operator bool(in LuaVariant variant) => variant.AsBoolean();

        /// <summary>
        /// Converts the given Lua <paramref name="variant"/> into an integer.
        /// </summary>
        /// <param name="variant">The Lua variant.</param>
        public static explicit operator long(in LuaVariant variant) => variant.AsInteger();

        /// <summary>
        /// Converts the given Lua <paramref name="variant"/> into a number.
        /// </summary>
        /// <param name="variant">The Lua variant.</param>
        public static explicit operator double(in LuaVariant variant) => variant.AsNumber();

        /// <summary>
        /// Converts the given Lua <paramref name="variant"/> into a string.
        /// </summary>
        /// <param name="variant">The Lua variant.</param>
        public static explicit operator string?(in LuaVariant variant) => variant.AsString();

        /// <summary>
        /// Converts the given Lua <paramref name="variant"/> into a Lua object.
        /// </summary>
        /// <param name="variant">The Lua variant.</param>
        public static explicit operator LuaObject?(in LuaVariant variant) => variant.AsLuaObject();
    }
}
