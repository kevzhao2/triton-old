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
        private static readonly TypeTag _booleanTag = new TypeTag();
        private static readonly TypeTag _integerTag = new TypeTag();
        private static readonly TypeTag _numberTag = new TypeTag();

        // `LuaVariant` consists of an 8-byte value followed by an 8-byte object or type tag. Primitives (such as `nil`,
        // booleans, integers, and numbers) are represented using the value and a specific type tag, and objects are
        // represented using an object type and the actual object.

        [FieldOffset(0)] private readonly bool _boolean;
        [FieldOffset(0)] private readonly long _integer;
        [FieldOffset(0)] private readonly double _number;
        [FieldOffset(0)] private readonly ObjectType _objectType;

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

        private LuaVariant(ObjectType objectType, object? value) : this()
        {
            _objectType = objectType;
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
        public bool IsString => _objectType == ObjectType.String && !IsNil && !(_objectOrTag is TypeTag);

        /// <summary>
        /// Gets a value indicating whether the variant is a Lua object.
        /// </summary>
        /// <value><see langword="true"/> if the variant is a Lua object; otherwise, <see langword="false"/>.</value>
        public bool IsLuaObject => _objectType == ObjectType.LuaObject && !IsNil && !(_objectOrTag is TypeTag);

        /// <summary>
        /// Gets a value indicating whether the variant is a CLR type.
        /// </summary>
        /// <value><see langword="true"/> if the variant is a CLR type; otherwise, <see langword="false"/>.</value>
        public bool IsClrType => _objectType == ObjectType.ClrType && !IsNil && !(_objectOrTag is TypeTag);

        /// <summary>
        /// Gets a value indicating whether the variant is a CLR object.
        /// </summary>
        /// <value><see langword="true"/> if the variant is a CLR object; otherwise, <see langword="false"/>.</value>
        public bool IsClrObject => _objectType == ObjectType.ClrObject && !IsNil && !(_objectOrTag is TypeTag);

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
        public static LuaVariant FromString(string? value) => new LuaVariant(ObjectType.String, value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given Lua object
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The Lua object value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromLuaObject(LuaObject? value) => new LuaVariant(ObjectType.LuaObject, value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given CLR type
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The CLR type value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromClrType(Type? value) => new LuaVariant(ObjectType.ClrType, value);

        /// <summary>
        /// Creates a new instance of the <see cref="LuaVariant"/> structure from the given CLR object
        /// <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The CLR object value.</param>
        /// <returns>A new instance of the <see cref="LuaVariant"/> structure.</returns>
        public static LuaVariant FromClrObject(object? value) => new LuaVariant(ObjectType.ClrObject, value);

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
            Debug.Assert(IsBoolean);

            return _boolean;
        }

        /// <summary>
        /// Converts the variant into an integer.
        /// </summary>
        /// <returns>The variant as an integer.</returns>
        public long AsInteger()
        {
            Debug.Assert(IsInteger);

            return _integer;
        }

        /// <summary>
        /// Converts the variant into a number.
        /// </summary>
        /// <returns>The variant as a number.</returns>
        public double AsNumber()
        {
            Debug.Assert(IsNumber);

            return _number;
        }

        /// <summary>
        /// Converts the variant into a string.
        /// </summary>
        /// <returns>The variant as a string.</returns>
        public string? AsString()
        {
            Debug.Assert(IsString);

            return _objectOrTag as string;
        }

        /// <summary>
        /// Converts the variant into a Lua object.
        /// </summary>
        /// <returns>The variant as a Lua object.</returns>
        public LuaObject? AsLuaObject()
        {
            Debug.Assert(IsLuaObject);

            return _objectOrTag as LuaObject;
        }

        /// <summary>
        /// Converts the variant into a CLR type.
        /// </summary>
        /// <returns>The variant as a CLR type.</returns>
        public Type? AsClrType()
        {
            Debug.Assert(IsClrType);

            return _objectOrTag as Type;
        }

        /// <summary>
        /// Converts the variant into a CLR object.
        /// </summary>
        /// <returns>The variant as a CLR object.</returns>
        public object? AsClrObject()
        {
            Debug.Assert(IsClrObject);

            return _objectOrTag;
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
            else if (_objectOrTag is TypeTag)
            {
                if (_objectOrTag == _booleanTag)
                {
                    lua_pushboolean(state, _boolean);
                }
                else if (_objectOrTag == _integerTag)
                {
                    lua_pushinteger(state, _integer);
                }
                else
                {
                    lua_pushnumber(state, _number);
                }
            }
            else
            {
                if (_objectType == ObjectType.String)
                {
                    lua_pushstring(state, (string)_objectOrTag);
                }
                else if (_objectType == ObjectType.LuaObject)
                {
                    ((LuaObject)_objectOrTag).Push(state);
                }
                else if (_objectType == ObjectType.ClrType)
                {
                    var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(lua_getextraspace(state)));
                    if (!(handle.Target is LuaEnvironment environment))
                    {
                        return;
                    }

                    environment.PushClrType(state, (Type)_objectOrTag);
                }
                else
                {
                    var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(lua_getextraspace(state)));
                    if (!(handle.Target is LuaEnvironment environment))
                    {
                        return;
                    }

                    environment.PushClrObject(state, _objectOrTag);
                }
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
        public static implicit operator LuaVariant(string? value) => new LuaVariant(ObjectType.String, value);

        /// <summary>
        /// Converts the given Lua object <paramref name="value"/> into a Lua variant.
        /// </summary>
        /// <param name="value">The Lua object value.</param>
        public static implicit operator LuaVariant(LuaObject? value) => new LuaVariant(ObjectType.LuaObject, value);

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

        private class TypeTag
        {
        }

        private enum ObjectType
        {
            String,
            LuaObject,
            ClrType,
            ClrObject
        }
    }
}
