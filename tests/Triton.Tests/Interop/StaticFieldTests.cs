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
using System.Collections.Generic;
using Xunit;

namespace Triton.Interop
{
    public class StaticFieldTests
    {
        public unsafe class MyClass
        {
            public static readonly int ReadOnlyField;

            public static bool BoolField;
            public static void* PtrField;
            public static IntPtr IntPtrField;
            public static UIntPtr UIntPtrField;
            public static byte ByteField;
            public static sbyte SByteField;
            public static short ShortField;
            public static ushort UShortField;
            public static int IntField;
            public static uint UIntField;
            public static long LongField;
            public static ulong ULongField;
            public static float FloatField;
            public static double DoubleField;
            public static string? StringField;
            public static char CharField;
            public static LuaObject? LuaObjectField;
            public static LuaTable? LuaTableField;
            public static LuaFunction? LuaFunctionField;
            public static LuaThread? LuaThreadField;

            public static List<int>? ListField;
            public static DateTime DateTimeField;
        }

        [Fact]
        public void Get_InvalidField_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("_ = MyClass.InvalidField"));
            Assert.Contains("attempt to index invalid member 'InvalidField'", ex.Message);
        }

        [Fact]
        public unsafe void Get()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));
            environment.Eval("table = {}");
            environment.Eval("func = function() end");
            environment.Eval("thread = coroutine.create(function() end)");

            MyClass.BoolField = true;
            MyClass.PtrField = (void*)new IntPtr(123456789);
            MyClass.IntPtrField = new IntPtr(123456789);
            MyClass.UIntPtrField = new UIntPtr(123456789);
            MyClass.ByteField = 234;
            MyClass.SByteField = -123;
            MyClass.ShortField = -12345;
            MyClass.UShortField = 34567;
            MyClass.IntField = -123456789;
            MyClass.UIntField = 2345678910U;
            MyClass.LongField = -12345678910;
            MyClass.ULongField = 2345678910111213UL;
            MyClass.FloatField = 1.234f;
            MyClass.DoubleField = 1.23456789;
            MyClass.StringField = "test";
            MyClass.CharField = 'f';
            MyClass.LuaObjectField = (LuaObject)environment["table"];
            MyClass.LuaTableField = (LuaTable)environment["table"];
            MyClass.LuaFunctionField = (LuaFunction)environment["func"];
            MyClass.LuaThreadField = (LuaThread)environment["thread"];

            environment["intptr_value"] = new IntPtr(123456789);

            environment.Eval("assert(MyClass.BoolField)");
            environment.Eval("assert(MyClass.PtrField == intptr_value)");
            environment.Eval("assert(MyClass.IntPtrField == intptr_value)");
            environment.Eval("assert(MyClass.UIntPtrField == intptr_value)");
            environment.Eval("assert(MyClass.ByteField == 234)");
            environment.Eval("assert(MyClass.SByteField == -123)");
            environment.Eval("assert(MyClass.ShortField == -12345)");
            environment.Eval("assert(MyClass.UShortField == 34567)");
            environment.Eval("assert(MyClass.IntField == -123456789)");
            environment.Eval("assert(MyClass.UIntField == 2345678910)");
            environment.Eval("assert(MyClass.LongField == -12345678910)");
            environment.Eval("assert(MyClass.ULongField == 2345678910111213)");
            environment.Eval("assert(MyClass.FloatField == 1.2339999675750732)");
            environment.Eval("assert(MyClass.DoubleField == 1.23456789)");
            environment.Eval("assert(MyClass.StringField == 'test')");
            environment.Eval("assert(MyClass.CharField == 'f')");
            environment.Eval("assert(MyClass.LuaObjectField == table)");
            environment.Eval("assert(MyClass.LuaTableField == table)");
            environment.Eval("assert(MyClass.LuaFunctionField == func)");
            environment.Eval("assert(MyClass.LuaThreadField == thread)");
        }

        [Fact]
        public void Set_ReadOnlyField_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("MyClass.ReadOnlyField = 1234"));
            Assert.Contains("attempt to set read-only field 'ReadOnlyField'", ex.Message);
        }

        [Fact]
        public void Set_Char_NotLengthOne_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("MyClass.CharField = 'test'"));
            Assert.Contains("attempt to set field 'CharField' with invalid value", ex.Message);
        }

        [Fact]
        public unsafe void Set()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));
            environment.Eval("table = {}");
            environment.Eval("func = function() end");
            environment.Eval("thread = coroutine.create(function() end)");

            environment["intptr_value"] = new IntPtr(123456789);
            environment["list"] = LuaValue.FromClrObject(new List<int>());
            environment["datetime"] = LuaValue.FromClrObject(new DateTime());

            environment.Eval("MyClass.BoolField = true");
            environment.Eval("MyClass.PtrField = intptr_value");
            environment.Eval("MyClass.IntPtrField = intptr_value");
            environment.Eval("MyClass.UIntPtrField = intptr_value");
            environment.Eval("MyClass.ByteField = 234");
            environment.Eval("MyClass.SByteField = -123");
            environment.Eval("MyClass.ShortField = -12345");
            environment.Eval("MyClass.UShortField = 34567");
            environment.Eval("MyClass.IntField = -123456789");
            environment.Eval("MyClass.UIntField = 2345678910");
            environment.Eval("MyClass.LongField = -12345678910");
            environment.Eval("MyClass.ULongField = 2345678910111213");
            environment.Eval("MyClass.FloatField = 1.234");
            environment.Eval("MyClass.DoubleField = 1.23456789");
            environment.Eval("MyClass.StringField = 'test'");
            environment.Eval("MyClass.CharField = 'f'");
            environment.Eval("MyClass.LuaObjectField = table");
            environment.Eval("MyClass.LuaTableField = table");
            environment.Eval("MyClass.LuaFunctionField = func");
            environment.Eval("MyClass.LuaThreadField = thread");
            environment.Eval("MyClass.ListField = list");
            environment.Eval("MyClass.DateTimeField = datetime");

            Assert.True(MyClass.BoolField);
            Assert.Equal(new IntPtr(123456789), (IntPtr)MyClass.PtrField);
            Assert.Equal(new IntPtr(123456789), MyClass.IntPtrField);
            Assert.Equal(new UIntPtr(123456789), MyClass.UIntPtrField);
            Assert.Equal(234, MyClass.ByteField);
            Assert.Equal(-123, MyClass.SByteField);
            Assert.Equal(-12345, MyClass.ShortField);
            Assert.Equal(34567, MyClass.UShortField);
            Assert.Equal(-123456789, MyClass.IntField);
            Assert.Equal(2345678910U, MyClass.UIntField);
            Assert.Equal(-12345678910, MyClass.LongField);
            Assert.Equal(2345678910111213UL, MyClass.ULongField);
            Assert.Equal(1.234f, MyClass.FloatField);
            Assert.Equal(1.23456789, MyClass.DoubleField);
            Assert.Equal("test", MyClass.StringField);
            Assert.Equal('f', MyClass.CharField);
            Assert.Equal(MyClass.LuaObjectField, (LuaObject)environment["table"]);
            Assert.Equal(MyClass.LuaTableField, (LuaTable)environment["table"]);
            Assert.Equal(MyClass.LuaFunctionField, (LuaFunction)environment["func"]);
            Assert.Equal(MyClass.LuaThreadField, (LuaThread)environment["thread"]);
        }
    }
}
