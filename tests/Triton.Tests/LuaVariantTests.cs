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
    public class LuaVariantTests
    {
        [Fact]
        public void IsNil_Get_ReturnsTrue()  // Also tests `LuaVariant.Nil`
        {
            var variant = LuaVariant.Nil;

            Assert.True(variant.IsNil);
        }

        [Fact]
        public void IsBoolean_Get_ReturnsTrue()
        {
            var variant = LuaVariant.FromBoolean(true);

            Assert.True(variant.IsBoolean);
        }

        [Fact]
        public void IsInteger_Get_ReturnsTrue()
        {
            var variant = LuaVariant.FromInteger(1234);

            Assert.True(variant.IsInteger);
        }

        [Fact]
        public void IsNumber_Get_ReturnsTrue()
        {
            var variant = LuaVariant.FromNumber(1.234);

            Assert.True(variant.IsNumber);
        }

        [Fact]
        public void IsString_Get_ReturnsTrue()
        {
            var variant = LuaVariant.FromString("test");

            Assert.True(variant.IsString);
        }

        [Fact]
        public void IsString_Get_ReturnsFalse_Nil()
        {
            var variant = LuaVariant.FromString(null);

            Assert.False(variant.IsString);
        }

        [Fact]
        public void IsString_Get_ReturnsFalse_Integer()
        {
            var variant = LuaVariant.FromInteger(0);

            Assert.False(variant.IsString);
        }

        [Fact]
        public void IsLuaObject_Get_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var variant = LuaVariant.FromLuaObject(table);

            Assert.True(variant.IsLuaObject);
        }

        [Fact]
        public void IsLuaObject_Get_ReturnsFalse_Nil()
        {
            var variant = LuaVariant.FromLuaObject(null);

            Assert.False(variant.IsLuaObject);
        }

        [Fact]
        public void IsLuaObject_Get_ReturnsFalse_Integer()
        {
            var variant = LuaVariant.FromInteger(1);

            Assert.False(variant.IsLuaObject);
        }

        [Fact]
        public void IsClrType_Get_ReturnsTrue()
        {
            var variant = LuaVariant.FromClrType(typeof(List<int>));

            Assert.True(variant.IsClrType);
        }

        [Fact]
        public void IsClrType_Get_ReturnsFalse_Nil()
        {
            var variant = LuaVariant.FromClrType(null);

            Assert.False(variant.IsClrType);
        }

        [Fact]
        public void IsClrType_Get_ReturnsFalse_Integer()
        {
            var variant = LuaVariant.FromInteger(2);

            Assert.False(variant.IsClrType);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsTrue()
        {
            var list = new List<int>();
            var variant = LuaVariant.FromClrObject(list);

            Assert.True(variant.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_Nil()
        {
            var variant = LuaVariant.FromClrObject(null);

            Assert.False(variant.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_Integer()
        {
            var variant = LuaVariant.FromInteger(3);

            Assert.False(variant.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_String()
        {
            var variant = LuaVariant.FromString("test");

            Assert.False(variant.IsClrObject);
        }

        [Fact]
        public void IsClrObject_Get_ReturnsFalse_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var variant = LuaVariant.FromLuaObject(table);

            Assert.False(variant.IsClrObject);
        }

        [Fact]
        public void FromBoolean_AsBoolean()
        {
            var variant = LuaVariant.FromBoolean(true);

            Assert.True(variant.AsBoolean());
        }

        [Fact]
        public void FromInteger_AsInteger()
        {
            var variant = LuaVariant.FromInteger(1234);

            Assert.Equal(1234, variant.AsInteger());
        }

        [Fact]
        public void FromNumber_AsNumber()
        {
            var variant = LuaVariant.FromNumber(1.234);

            Assert.Equal(1.234, variant.AsNumber());
        }

        [Fact]
        public void FromString_NullString()
        {
            var variant = LuaVariant.FromString(null);

            Assert.True(variant.IsNil);
        }

        [Fact]
        public void FromString_AsString()
        {
            var variant = LuaVariant.FromString("test");

            Assert.Equal("test", variant.AsString());
        }

        [Fact]
        public void FromLuaObject_AsLuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var variant = LuaVariant.FromLuaObject(table);

            Assert.Same(table, variant.AsLuaObject());
        }

        [Fact]
        public void FromClrObject_AsClrObject()
        {
            var list = new List<int>();
            var variant = LuaVariant.FromClrObject(list);

            Assert.Same(list, variant.AsClrObject());
        }

        [Fact]
        public void Push_Nil()
        {
            var state = luaL_newstate();

            try
            {
                LuaVariant.Nil.Push(state);

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
                LuaVariant.FromBoolean(true).Push(state);

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
                LuaVariant.FromInteger(1234).Push(state);

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
                LuaVariant.FromNumber(1.234).Push(state);

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
                LuaVariant.FromString("test").Push(state);

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
            LuaVariant variant = true;

            Assert.True(variant.AsBoolean());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_Long()
        {
            LuaVariant variant = 1234;

            Assert.Equal(1234, variant.AsInteger());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_Double()
        {
            LuaVariant variant = 1.234;

            Assert.Equal(1.234, variant.AsNumber());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_String()
        {
            LuaVariant variant = "test";

            Assert.Equal("test", variant.AsString());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Implicit_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            LuaVariant variant = table;

            Assert.Same(table, variant.AsLuaObject());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_Bool()
        {
            var variant = LuaVariant.FromBoolean(true);

            Assert.True((bool)variant);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_Long()
        {
            var variant = LuaVariant.FromInteger(1234);

            Assert.Equal(1234, (long)variant);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_Double()
        {
            var variant = LuaVariant.FromNumber(1.234);

            Assert.Equal(1.234, (double)variant);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_String()
        {
            var variant = LuaVariant.FromString("test");

            Assert.Equal("test", (string?)variant);
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator name")]
        public void op_Explicit_LuaObject()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var variant = LuaVariant.FromLuaObject(table);

            Assert.Same(table, (LuaObject?)variant);
        }
    }
}
