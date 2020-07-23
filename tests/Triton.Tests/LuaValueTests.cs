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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Triton
{
    public class LuaValueTests
    {
        [Fact]
        public void IsNil_Get_ReturnsTrue()  // Also tests `LuaValue.Nil`
        {
            var value = LuaValue.Nil;

            Assert.True(value.IsNil);
        }

        [Fact]
        public void IsBoolean_Get_ReturnsTrue()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.True(value.IsBoolean);
        }

        [Fact]
        public void IsInteger_Get_ReturnsTrue()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.True(value.IsInteger);
        }

        [Fact]
        public void IsNumber_Get_ReturnsTrue()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.True(value.IsNumber);
        }

        [Fact]
        public void IsString_Get_ReturnsTrue()
        {
            var value = LuaValue.FromString("test");

            Assert.True(value.IsString);
        }

        [Fact]
        public void IsString_Get_ReturnsFalse_Nil()
        {
            var value = LuaValue.FromString(null);

            Assert.False(value.IsString);
        }

        [Fact]
        public void IsString_Get_ReturnsFalse_Integer()
        {
            var value = LuaValue.FromInteger(1);

            Assert.False(value.IsString);
        }

        [Fact]
        public void IsLuaObject_Get_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.True(value.IsLuaObject);
        }

        [Fact]
        public void IsLuaObject_Get_ReturnsFalse_Nil()
        {
            var value = LuaValue.FromLuaObject(null);

            Assert.False(value.IsLuaObject);
        }

        [Fact]
        public void IsLuaObject_Get_ReturnsFalse_Integer()
        {
            var value = LuaValue.FromInteger(2);

            Assert.False(value.IsLuaObject);
        }

        [Fact]
        public void IsClrType_Get_ReturnsTrue()
        {
            var value = LuaValue.FromClrType(typeof(List<int>));

            Assert.True(value.IsClrType);
        }

        [Fact]
        public void IsClrType_Get_ReturnsFalse_Nil()
        {
            var value = LuaValue.FromClrType(null);

            Assert.False(value.IsClrType);
        }

        [Fact]
        public void IsClrType_Get_ReturnsFalse_Integer()
        {
            var value = LuaValue.FromInteger(3);

            Assert.False(value.IsClrType);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsTrue()
        {
            var list = new List<int>();
            var value = LuaValue.FromClrObject(list);

            Assert.True(value.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_Nil()
        {
            var value = LuaValue.FromClrObject(null);

            Assert.False(value.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_Integer()
        {
            var value = LuaValue.FromInteger(4);

            Assert.False(value.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_String()
        {
            var value = LuaValue.FromString("test");

            Assert.False(value.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.False(value.IsClrObject);
        }

        [Fact]
        public void FromObject_Null()
        {
            var value = LuaValue.FromObject(null);

            Assert.True(value.IsNil);
        }

        [Fact]
        public void FromObject_Bool()
        {
            var value = LuaValue.FromObject(true);

            Assert.True(value.IsBoolean);
            Assert.True((bool)value);
        }

        [Fact]
        public void FromObject_Sbyte()
        {
            var value = LuaValue.FromObject(sbyte.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(sbyte.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Byte()
        {
            var value = LuaValue.FromObject(byte.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(byte.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Short()
        {
            var value = LuaValue.FromObject(short.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(short.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Ushort()
        {
            var value = LuaValue.FromObject(ushort.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(ushort.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Int()
        {
            var value = LuaValue.FromObject(int.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(int.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Uint()
        {
            var value = LuaValue.FromObject(uint.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(uint.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Long()
        {
            var value = LuaValue.FromObject(long.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(long.MaxValue, (long)value);
        }

        [Fact]
        public void FromObject_Ulong()
        {
            var value = LuaValue.FromObject(ulong.MaxValue);

            Assert.True(value.IsInteger);
            Assert.Equal(ulong.MaxValue, (ulong)(long)value);
        }

        [Fact]
        public void FromObject_Float()
        {
            var value = LuaValue.FromObject(1.234f);

            Assert.True(value.IsNumber);
            Assert.Equal(1.234f, (double)value);
        }

        [Fact]
        public void FromObject_Double()
        {
            var value = LuaValue.FromObject(1.234);

            Assert.True(value.IsNumber);
            Assert.Equal(1.234, (double)value);
        }

        [Fact]
        public void FromObject_String()
        {
            var value = LuaValue.FromObject("test");

            Assert.True(value.IsString);
            Assert.Equal("test", (string?)value);
        }

        [Fact]
        public void FromObject_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromObject(table);

            Assert.True(value.IsLuaObject);
            Assert.Same(table, (LuaObject?)value);
        }

        [Fact]
        public void FromObject_Object()
        {
            var list = new List<int>();
            var value = LuaValue.FromObject(list);

            Assert.True(value.IsClrObject);
            Assert.Same(list, value.AsClrObject());
        }

        [Fact]
        public void FromBoolean_AsBoolean()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.True(value.AsBoolean());
        }

        [Fact]
        public void FromInteger_AsInteger()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.Equal(1234, value.AsInteger());
        }

        [Fact]
        public void FromNumber_AsNumber()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.Equal(1.234, value.AsNumber());
        }

        [Fact]
        public void FromString_NullString()
        {
            var value = LuaValue.FromString(null);

            Assert.True(value.IsNil);
        }

        [Fact]
        public void FromString_AsString()
        {
            var value = LuaValue.FromString("test");

            Assert.Equal("test", value.AsString());
        }

        [Fact]
        public void FromLuaObject_NullLuaObject()
        {
            var value = LuaValue.FromLuaObject(null);

            Assert.True(value.IsNil);
        }

        [Fact]
        public void FromLuaObject_AsLuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.Same(table, value.AsLuaObject());
        }

        [Fact]
        public void FromClrType_NullClrType()
        {
            var value = LuaValue.FromClrType(null);

            Assert.True(value.IsNil);
        }

        [Fact]
        public void FromClrType_AsClrObject()
        {
            var value = LuaValue.FromClrType(typeof(List<int>));

            Assert.Same(typeof(List<int>), value.AsClrType());
        }

        [Fact]
        public void FromClrObject_NullClrObject()
        {
            var value = LuaValue.FromClrObject(null);

            Assert.True(value.IsNil);
        }

        [Fact]
        public void FromClrObject_AsClrObject()
        {
            var list = new List<int>();
            var value = LuaValue.FromClrObject(list);

            Assert.Same(list, value.AsClrObject());
        }

        [Fact]
        public void Equals_Object_WrongType_ReturnsFalse()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.False(value.Equals((object)1));
        }

        [Fact]
        public void Equals_Object_ReturnsTrue()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.True(value.Equals((object)LuaValue.FromInteger(1234)));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_Nil()
        {
            var value = LuaValue.Nil;

            Assert.True(value.Equals(LuaValue.Nil));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_Nil()
        {
            var value = LuaValue.Nil;

            Assert.False(value.Equals(1234));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_Boolean()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.True(value.Equals(true));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_Boolean()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.False(value.Equals(false));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_Integer()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.True(value.Equals(1234));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_Integer()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.False(value.Equals(0));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_Number()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.True(value.Equals(1.234));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_Number()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.False(value.Equals(0.0));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_String()
        {
            var value = LuaValue.FromString("test");

            Assert.True(value.Equals("test"));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_String()
        {
            var value = LuaValue.FromString("test");

            Assert.False(value.Equals("asdf"));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.True(value.Equals(table));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var table2 = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.False(value.Equals(table2));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_ClrType()
        {
            var value = LuaValue.FromClrType(typeof(List<int>));

            Assert.True(value.Equals(LuaValue.FromClrType(typeof(List<int>))));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_ClrType()
        {
            var value = LuaValue.FromClrType(typeof(List<int>));

            Assert.False(value.Equals(LuaValue.FromClrType(typeof(List<string>))));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsTrue_ClrObject()
        {
            var list = new List<int>();
            var value = LuaValue.FromClrObject(list);

            Assert.True(value.Equals(LuaValue.FromClrObject(list)));
        }

        [Fact]
        public void Equals_LuaValue_ReturnsFalse_ClrObject()
        {
            var list = new List<int>();
            var list2 = new List<int>();
            var value = LuaValue.FromClrObject(list);

            Assert.False(value.Equals(LuaValue.FromClrObject(list2)));
        }

        [Fact]
        public void GetHashCode_Equals_Nil_AreSame()
        {
            var value = LuaValue.Nil;
            var value2 = LuaValue.Nil;

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_Boolean_AreSame()
        {
            var value = LuaValue.FromBoolean(true);
            var value2 = LuaValue.FromBoolean(true);

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_Integer_AreSame()
        {
            var value = LuaValue.FromInteger(1234);
            var value2 = LuaValue.FromInteger(1234);

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_Number_AreSame()
        {
            var value = LuaValue.FromNumber(1.234);
            var value2 = LuaValue.FromNumber(1.234);

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_String_AreSame()
        {
            var value = LuaValue.FromString("test");
            var value2 = LuaValue.FromString("test");

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_LuaObject_AreSame()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);
            var value2 = LuaValue.FromLuaObject(table);

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_ClrType_AreSame()
        {
            var value = LuaValue.FromClrType(typeof(List<int>));
            var value2 = LuaValue.FromClrType(typeof(List<int>));

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_Equals_ClrObject_AreSame()
        {
            var list = new List<int>();
            var value = LuaValue.FromClrObject(list);
            var value2 = LuaValue.FromClrObject(list);

            Assert.Equal(value.GetHashCode(), value2.GetHashCode());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_Bool()
        {
            LuaValue value = true;

            Assert.True(value.AsBoolean());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_Long()
        {
            LuaValue value = 1234;

            Assert.Equal(1234, value.AsInteger());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_Double()
        {
            LuaValue value = 1.234;

            Assert.Equal(1.234, value.AsNumber());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_String()
        {
            LuaValue value = "test";

            Assert.Equal("test", value.AsString());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            LuaValue value = table;

            Assert.Same(table, value.AsLuaObject());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_Bool()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.True((bool)value);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_Long()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.Equal(1234, (long)value);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_Double()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.Equal(1.234, (double)value);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_String()
        {
            var value = LuaValue.FromString("test");

            Assert.Equal("test", (string?)value);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.Same(table, (LuaObject?)value);
        }
    }
}
