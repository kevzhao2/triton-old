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
using static Triton.NativeMethods;

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
            var value = LuaValue.FromInteger(0);

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
            var value = LuaValue.FromInteger(1);

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
            var value = LuaValue.FromInteger(2);

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
            var value = LuaValue.FromInteger(3);

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
        public void FromLuaObject_AsLuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var value = LuaValue.FromLuaObject(table);

            Assert.Same(table, value.AsLuaObject());
        }

        [Fact]
        public void FromClrObject_AsClrObject()
        {
            var list = new List<int>();
            var value = LuaValue.FromClrObject(list);

            Assert.Same(list, value.AsClrObject());
        }

        [Fact]
        public void Push_Nil()
        {
            var state = luaL_newstate();

            try
            {
                LuaValue.Nil.Push(state);

                Assert.Equal(LuaType.Nil, lua_type(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Push_Boolean()
        {
            var state = luaL_newstate();

            try
            {
                LuaValue.FromBoolean(true).Push(state);

                Assert.Equal(LuaType.Boolean, lua_type(state, -1));
                Assert.True(lua_toboolean(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Push_Integer()
        {
            var state = luaL_newstate();

            try
            {
                LuaValue.FromInteger(1234).Push(state);

                Assert.Equal(LuaType.Number, lua_type(state, -1));
                Assert.True(lua_isinteger(state, -1));
                Assert.Equal(1234, lua_tointeger(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Push_Number()
        {
            var state = luaL_newstate();

            try
            {
                LuaValue.FromNumber(1.234).Push(state);

                Assert.Equal(LuaType.Number, lua_type(state, -1));
                Assert.False(lua_isinteger(state, -1));
                Assert.Equal(1.234, lua_tonumber(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Push_String()
        {
            var state = luaL_newstate();

            try
            {
                LuaValue.FromString("test").Push(state);

                Assert.Equal(LuaType.String, lua_type(state, -1));
                Assert.Equal("test", lua_tostring(state, -1));
            }
            finally
            {
                lua_close(state);
            }
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
