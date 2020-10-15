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
using System.Threading.Tasks;
using Xunit;

namespace Triton
{
    public class LuaValueTests
    {
        [Fact]
        public void Nil_Get()
        {
            var value = LuaValue.Nil;

            Assert.True(value.IsNil);
        }

        [Fact]
        public void IsNil_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.True(LuaValue.Nil.IsNil);
            Assert.True(LuaValue.FromLuaObject(null).IsNil);
            Assert.True(LuaValue.FromString(null).IsNil);
            Assert.False(LuaValue.FromBoolean(true).IsNil);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsNil);
            Assert.False(LuaValue.FromInteger(1234).IsNil);
            Assert.False(LuaValue.FromNumber(1.234).IsNil);
            Assert.False(LuaValue.FromString("test").IsNil);
            Assert.False(LuaValue.FromLuaObject(table).IsNil);
            Assert.False(LuaValue.FromClrTypes(types).IsNil);
            Assert.False(LuaValue.FromClrObject(obj).IsNil);
        }

        [Fact]
        public void IsBoolean_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsBoolean);
            Assert.True(LuaValue.FromBoolean(true).IsBoolean);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsBoolean);
            Assert.False(LuaValue.FromInteger(1234).IsBoolean);
            Assert.False(LuaValue.FromNumber(1.234).IsBoolean);
            Assert.False(LuaValue.FromString("test").IsBoolean);
            Assert.False(LuaValue.FromLuaObject(table).IsBoolean);
            Assert.False(LuaValue.FromClrTypes(types).IsBoolean);
            Assert.False(LuaValue.FromClrObject(obj).IsBoolean);
        }

        [Fact]
        public void IsPointer_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsPointer);
            Assert.False(LuaValue.FromBoolean(true).IsPointer);
            Assert.True(LuaValue.FromPointer((IntPtr)0x12345678).IsPointer);
            Assert.False(LuaValue.FromInteger(1234).IsPointer);
            Assert.False(LuaValue.FromNumber(1.234).IsPointer);
            Assert.False(LuaValue.FromString("test").IsPointer);
            Assert.False(LuaValue.FromLuaObject(table).IsPointer);
            Assert.False(LuaValue.FromClrTypes(types).IsPointer);
            Assert.False(LuaValue.FromClrObject(obj).IsPointer);
        }

        [Fact]
        public void IsInteger_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsInteger);
            Assert.False(LuaValue.FromBoolean(true).IsInteger);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsInteger);
            Assert.True(LuaValue.FromInteger(1234).IsInteger);
            Assert.False(LuaValue.FromNumber(1.234).IsInteger);
            Assert.False(LuaValue.FromString("test").IsInteger);
            Assert.False(LuaValue.FromLuaObject(table).IsInteger);
            Assert.False(LuaValue.FromClrTypes(types).IsInteger);
            Assert.False(LuaValue.FromClrObject(obj).IsInteger);
        }

        [Fact]
        public void IsNumber_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsNumber);
            Assert.False(LuaValue.FromBoolean(true).IsNumber);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsNumber);
            Assert.False(LuaValue.FromInteger(1234).IsNumber);
            Assert.True(LuaValue.FromNumber(1.234).IsNumber);
            Assert.False(LuaValue.FromString("test").IsNumber);
            Assert.False(LuaValue.FromLuaObject(table).IsNumber);
            Assert.False(LuaValue.FromClrTypes(types).IsNumber);
            Assert.False(LuaValue.FromClrObject(obj).IsNumber);
        }

        [Fact]
        public void IsString_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsString);
            Assert.False(LuaValue.FromBoolean(true).IsString);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsString);
            Assert.False(LuaValue.FromInteger(1234).IsString);
            Assert.False(LuaValue.FromNumber(1.234).IsString);
            Assert.True(LuaValue.FromString("test").IsString);
            Assert.False(LuaValue.FromLuaObject(table).IsString);
            Assert.False(LuaValue.FromClrTypes(types).IsString);
            Assert.False(LuaValue.FromClrObject(obj).IsString);

            // Special case:

            Assert.False(LuaValue.FromClrObject("test").IsString);
        }

        [Fact]
        public void IsLuaObject_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsLuaObject);
            Assert.False(LuaValue.FromBoolean(true).IsLuaObject);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsLuaObject);
            Assert.False(LuaValue.FromInteger(1234).IsLuaObject);
            Assert.False(LuaValue.FromNumber(1.234).IsLuaObject);
            Assert.False(LuaValue.FromString("test").IsLuaObject);
            Assert.True(LuaValue.FromLuaObject(table).IsLuaObject);
            Assert.False(LuaValue.FromClrTypes(types).IsLuaObject);
            Assert.False(LuaValue.FromClrObject(obj).IsLuaObject);

            // Special case:

            Assert.False(LuaValue.FromClrObject((LuaTable)environment["table"]).IsLuaObject);
        }

        [Fact]
        public void IsClrTypes_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsClrTypes);
            Assert.False(LuaValue.FromBoolean(true).IsClrTypes);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsClrTypes);
            Assert.False(LuaValue.FromInteger(1234).IsClrTypes);
            Assert.False(LuaValue.FromNumber(1.234).IsClrTypes);
            Assert.False(LuaValue.FromString("test").IsClrTypes);
            Assert.False(LuaValue.FromLuaObject(table).IsClrTypes);
            Assert.True(LuaValue.FromClrTypes(types).IsClrTypes);
            Assert.False(LuaValue.FromClrObject(obj).IsClrTypes);

            // Special case:

            Assert.False(LuaValue.FromClrObject(new[] { typeof(int) }).IsClrTypes);
        }

        [Fact]
        public void IsClrObject_Get()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.False(LuaValue.Nil.IsClrObject);
            Assert.False(LuaValue.FromBoolean(true).IsClrObject);
            Assert.False(LuaValue.FromPointer((IntPtr)0x12345678).IsClrObject);
            Assert.False(LuaValue.FromInteger(1234).IsClrObject);
            Assert.False(LuaValue.FromNumber(1.234).IsClrObject);
            Assert.False(LuaValue.FromString("test").IsClrObject);
            Assert.False(LuaValue.FromLuaObject(table).IsClrObject);
            Assert.False(LuaValue.FromClrTypes(types).IsClrObject);
            Assert.True(LuaValue.FromClrObject(obj).IsClrObject);
        }

        [Fact]
        public void FromObject()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var obj = new object();

            Assert.Equal(LuaValue.Nil, LuaValue.FromObject(null));
            Assert.Equal(true, LuaValue.FromObject(true));
            Assert.Equal((IntPtr)0x12345678, LuaValue.FromObject((IntPtr)0x12345678));
            Assert.Equal(123, LuaValue.FromObject((sbyte)123));
            Assert.Equal(123, LuaValue.FromObject((byte)123));
            Assert.Equal(1234, LuaValue.FromObject((short)1234));
            Assert.Equal(1234, LuaValue.FromObject((ushort)1234));
            Assert.Equal(1234, LuaValue.FromObject(1234));
            Assert.Equal(1234, LuaValue.FromObject(1234U));
            Assert.Equal(1234, LuaValue.FromObject(1234L));
            Assert.Equal(1234, LuaValue.FromObject(1234UL));
            Assert.Equal(1.2339999675750732, LuaValue.FromObject(1.234f));
            Assert.Equal(1.234, LuaValue.FromObject(1.234));
            Assert.Equal("t", LuaValue.FromObject('t'));
            Assert.Equal("test", LuaValue.FromObject("test"));
            Assert.Equal(table, LuaValue.FromObject(table));
            Assert.Same(obj, LuaValue.FromObject(obj).ToClrObject());
        }

        [Fact]
        public void FromBoolean_ToBoolean()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.True(value.ToBoolean());
        }

        [Fact]
        public void FromBoolean_op_Explicit_Boolean()
        {
            var value = LuaValue.FromBoolean(true);

            Assert.True(value.ToBoolean());
        }

        [Fact]
        public void FromPointer_ToPointer()
        {
            var value = LuaValue.FromPointer((IntPtr)0x12345678);

            Assert.Equal((IntPtr)0x12345678, value.ToPointer());
        }

        [Fact]
        public void FromBoolean_op_Explicit_IntPtr()
        {
            var value = LuaValue.FromPointer((IntPtr)0x12345678);

            Assert.Equal((IntPtr)0x12345678, (IntPtr)value);
        }

        [Fact]
        public void FromInteger_ToInteger()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.Equal(1234, value.ToInteger());
        }

        [Fact]
        public void FromInteger_op_Explicit_Long()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.Equal(1234, (long)value);
        }

        [Fact]
        public void FromNumber_ToNumber()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.Equal(1.234, value.ToNumber());
        }

        [Fact]
        public void FromNumber_op_Explicit_Double()
        {
            var value = LuaValue.FromNumber(1.234);

            Assert.Equal(1.234, (double)value);
        }

        [Fact]
        public void FromString_ToString()
        {
            var value = LuaValue.FromString("test");

            Assert.Equal("test", value.ToString());
        }

        [Fact]
        public void FromString_op_Explicit_String()
        {
            var value = LuaValue.FromString("test");

            Assert.Equal("test", (string)value);
        }

        [Fact]
        public void FromLuaObject_ToLuaObject()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];

            var value = LuaValue.FromLuaObject(table);

            Assert.Same(table, value.ToLuaObject());
        }

        [Fact]
        public void FromLuaObject_op_Explicit_LuaObject()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];

            var value = LuaValue.FromLuaObject(table);

            Assert.Same(table, (LuaTable)value);
        }

        [Fact]
        public void FromClrTypes_NullTypes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaValue.FromClrTypes(null!));
        }

        [Fact]
        public void FromClrTypes_TypesContainsNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LuaValue.FromClrTypes(new Type[] { null! }));
        }

        [Fact]
        public void FromClrTypes_TypesContainsTwoTypesWithSameGenericArity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LuaValue.FromClrTypes(new[] { typeof(int), typeof(int) }));
            Assert.Throws<ArgumentException>(() => LuaValue.FromClrTypes(new[] { typeof(Task<>), typeof(Task<>) }));
        }

        [Fact]
        public void FromClrTypes_ToClrTypes()
        {
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var value = LuaValue.FromClrTypes(types);

            Assert.Same(types, value.ToClrTypes());
        }

        [Fact]
        public void FromClrObject_NullObj_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaValue.FromClrObject(null!));
        }

        [Fact]
        public void FromClrObject_ToClrObject()
        {
            var obj = new object();
            var value = LuaValue.FromClrObject(obj);

            Assert.Same(obj, value.ToClrObject());
        }

        [Fact]
        public void Equals_Object()
        {
            var value = LuaValue.FromInteger(1234);

            Assert.False(value.Equals((object?)null));
            Assert.False(value.Equals((object?)1234));
            Assert.True(value.Equals((object?)value));
        }

        [Fact]
        public void Equals_LuaValue()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();
            var representatives = new LuaValue[]
            {
                default, true, 1234, 1.234, "test", table, LuaValue.FromClrTypes(types), LuaValue.FromClrObject(obj)
            };

            for (var i = 0; i < representatives.Length; ++i)
            {
                for (var j = 0; j < representatives.Length; ++j)
                {
                    Assert.Equal(i == j, representatives[i].Equals(representatives[j]));
                    Assert.Equal(i == j, representatives[i] == representatives[j]);
                    Assert.Equal(i != j, representatives[i] != representatives[j]);
                }
            }
        }

        [Fact]
        public void GetHashCode_IsConsistent()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();
            var representatives = new LuaValue[]
            {
                default, true, 1234, 1.234, "test", table, LuaValue.FromClrTypes(types), LuaValue.FromClrObject(obj)
            };

            for (var i = 0; i < representatives.Length; ++i)
            {
                Assert.Equal(representatives[i].GetHashCode(), representatives[i].GetHashCode());
            }
        }

        [Fact]
        public void ToBoolean_IsNotBoolean_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToBoolean());
            Assert.Throws<InvalidCastException>(() => (bool)LuaValue.Nil);
        }

        [Fact]
        public void ToPointer_IsNotPointer_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToPointer());
            Assert.Throws<InvalidCastException>(() => (IntPtr)LuaValue.Nil);
        }

        [Fact]
        public void ToInteger_IsNotInteger_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToInteger());
            Assert.Throws<InvalidCastException>(() => (long)LuaValue.Nil);
        }

        [Fact]
        public void ToNumber_IsNotNumber_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToNumber());
            Assert.Throws<InvalidCastException>(() => (double)LuaValue.Nil);
        }

        [Fact]
        public void ToString_IsNotString_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToString());
            Assert.Throws<InvalidCastException>(() => (string)LuaValue.Nil);
        }

        [Fact]
        public void ToLuaObject_IsNotLuaObject_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToLuaObject());
            Assert.Throws<InvalidCastException>(() => (LuaObject)LuaValue.Nil);
        }

        [Fact]
        public void ToClrTypes_IsNotClrTypes_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToClrTypes());
        }

        [Fact]
        public void ToClrObject_IsNotClrObject_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => LuaValue.Nil.ToClrObject());
        }

        [Fact]
        public void ToObject()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();

            Assert.Equal(true, LuaValue.FromBoolean(true).ToObject());
            Assert.Equal((IntPtr)0x12345678, LuaValue.FromPointer((IntPtr)0x12345678).ToObject());
            Assert.Equal(1234L, LuaValue.FromInteger(1234).ToObject());
            Assert.Equal(1.234, LuaValue.FromNumber(1.234).ToObject());
            Assert.Equal("test", LuaValue.FromString("test").ToObject());
            Assert.Same(table, LuaValue.FromLuaObject(table).ToObject());
            Assert.Same(types, LuaValue.FromClrTypes(types).ToObject());
            Assert.Same(obj, LuaValue.FromClrObject(obj).ToObject());
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator")]
        public void op_Equality()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();
            var representatives = new LuaValue[]
            {
                default, true, 1234, 1.234, "test", table, LuaValue.FromClrTypes(types), LuaValue.FromClrObject(obj)
            };

            for (var i = 0; i < representatives.Length; ++i)
            {
                for (var j = 0; j < representatives.Length; ++j)
                {
                    Assert.Equal(i == j, representatives[i] == representatives[j]);
                }
            }
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator")]
        public void op_Inequality()
        {
            using var environment = new LuaEnvironment();
            environment.Eval("table = {}");

            var luaObj = (LuaTable)environment["table"];
            var types = new Type[] { typeof(Task), typeof(Task<>) };
            var obj = new object();
            var representatives = new LuaValue[]
            {
                default, true, 1234, 1.234, "test", luaObj, LuaValue.FromClrTypes(types), LuaValue.FromClrObject(obj)
            };

            for (var i = 0; i < representatives.Length; ++i)
            {
                for (var j = 0; j < representatives.Length; ++j)
                {
                    Assert.Equal(i != j, representatives[i] != representatives[j]);
                }
            }
        }
    }
}
