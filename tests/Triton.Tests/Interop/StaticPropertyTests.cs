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
using Xunit;

namespace Triton.Interop
{
    public class StaticPropertyTests
    {
        public unsafe class MyClass
        {
            public static bool BoolProperty;
            public static void* PtrProperty;
            public static IntPtr IntPtrProperty;
            public static UIntPtr UIntPtrProperty;
            public static byte ByteProperty;
            public static sbyte SByteProperty;
            public static short ShortProperty;
            public static ushort UShortProperty;
            public static int IntProperty;
            public static uint UIntProperty;
            public static long LongProperty;
            public static ulong ULongProperty;
            public static float FloatProperty;
            public static double DoubleProperty;
            public static string? StringProperty;
            public static char CharProperty;
            public static LuaObject? LuaObjectProperty;
            public static LuaTable? LuaTableProperty;
            public static LuaFunction? LuaFunctionProperty;
            public static LuaThread? LuaThreadProperty;
        }

        [Fact]
        public unsafe void Property_Get()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));
            environment.Eval("table = {}");
            environment.Eval("func = function() end");
            environment.Eval("thread = coroutine.create(function() end)");

            MyClass.BoolProperty = true;
            MyClass.PtrProperty = (void*)new IntPtr(123456789);
            MyClass.IntPtrProperty = new IntPtr(123456789);
            MyClass.UIntPtrProperty = new UIntPtr(123456789);
            MyClass.ByteProperty = 234;
            MyClass.SByteProperty = -123;
            MyClass.ShortProperty = -12345;
            MyClass.UShortProperty = 34567;
            MyClass.IntProperty = -123456789;
            MyClass.UIntProperty = 2345678910U;
            MyClass.LongProperty = -12345678910;
            MyClass.ULongProperty = 2345678910111213UL;
            MyClass.FloatProperty = 1.234f;
            MyClass.DoubleProperty = 1.23456789;
            MyClass.StringProperty = "test";
            MyClass.CharProperty = 'f';
            MyClass.LuaObjectProperty = (LuaObject)environment["table"];
            MyClass.LuaTableProperty = (LuaTable)environment["table"];
            MyClass.LuaFunctionProperty = (LuaFunction)environment["func"];
            MyClass.LuaThreadProperty = (LuaThread)environment["thread"];

            environment["intptr_value"] = new IntPtr(123456789);

            environment.Eval("assert(MyClass.BoolProperty)");
            environment.Eval("assert(MyClass.PtrProperty == intptr_value)");
            environment.Eval("assert(MyClass.IntPtrProperty == intptr_value)");
            environment.Eval("assert(MyClass.UIntPtrProperty == intptr_value)");
            environment.Eval("assert(MyClass.ByteProperty == 234)");
            environment.Eval("assert(MyClass.SByteProperty == -123)");
            environment.Eval("assert(MyClass.ShortProperty == -12345)");
            environment.Eval("assert(MyClass.UShortProperty == 34567)");
            environment.Eval("assert(MyClass.IntProperty == -123456789)");
            environment.Eval("assert(MyClass.UIntProperty == 2345678910)");
            environment.Eval("assert(MyClass.LongProperty == -12345678910)");
            environment.Eval("assert(MyClass.ULongProperty == 2345678910111213)");
            environment.Eval("assert(MyClass.FloatProperty == 1.2339999675750732)");
            environment.Eval("assert(MyClass.DoubleProperty == 1.23456789)");
            environment.Eval("assert(MyClass.StringProperty == 'test')");
            environment.Eval("assert(MyClass.CharProperty == 'f')");
            environment.Eval("assert(MyClass.LuaObjectProperty == table)");
            environment.Eval("assert(MyClass.LuaTableProperty == table)");
            environment.Eval("assert(MyClass.LuaFunctionProperty == func)");
            environment.Eval("assert(MyClass.LuaThreadProperty == thread)");
        }

        [Fact]
        public unsafe void Property_Set()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));
            environment.Eval("table = {}");
            environment.Eval("func = function() end");
            environment.Eval("thread = coroutine.create(function() end)");

            environment["intptr_value"] = new IntPtr(123456789);

            environment.Eval("MyClass.BoolProperty = true");
            environment.Eval("MyClass.PtrProperty = intptr_value");
            environment.Eval("MyClass.IntPtrProperty = intptr_value");
            environment.Eval("MyClass.UIntPtrProperty = intptr_value");
            environment.Eval("MyClass.ByteProperty = 234");
            environment.Eval("MyClass.SByteProperty = -123");
            environment.Eval("MyClass.ShortProperty = -12345");
            environment.Eval("MyClass.UShortProperty = 34567");
            environment.Eval("MyClass.IntProperty = -123456789");
            environment.Eval("MyClass.UIntProperty = 2345678910");
            environment.Eval("MyClass.LongProperty = -12345678910");
            environment.Eval("MyClass.ULongProperty = 2345678910111213");
            environment.Eval("MyClass.FloatProperty = 1.234");
            environment.Eval("MyClass.DoubleProperty = 1.23456789");
            environment.Eval("MyClass.StringProperty = 'test'");
            environment.Eval("MyClass.CharProperty = 'f'");
            environment.Eval("MyClass.LuaObjectProperty = table");
            environment.Eval("MyClass.LuaTableProperty = table");
            environment.Eval("MyClass.LuaFunctionProperty = func");
            environment.Eval("MyClass.LuaThreadProperty = thread");

            Assert.True(MyClass.BoolProperty);
            Assert.Equal(new IntPtr(123456789), (IntPtr)MyClass.PtrProperty);
            Assert.Equal(new IntPtr(123456789), MyClass.IntPtrProperty);
            Assert.Equal(new UIntPtr(123456789), MyClass.UIntPtrProperty);
            Assert.Equal(234, MyClass.ByteProperty);
            Assert.Equal(-123, MyClass.SByteProperty);
            Assert.Equal(-12345, MyClass.ShortProperty);
            Assert.Equal(34567, MyClass.UShortProperty);
            Assert.Equal(-123456789, MyClass.IntProperty);
            Assert.Equal(2345678910U, MyClass.UIntProperty);
            Assert.Equal(-12345678910, MyClass.LongProperty);
            Assert.Equal(2345678910111213UL, MyClass.ULongProperty);
            Assert.Equal(1.234f, MyClass.FloatProperty);
            Assert.Equal(1.23456789, MyClass.DoubleProperty);
            Assert.Equal("test", MyClass.StringProperty);
            Assert.Equal('f', MyClass.CharProperty);
            Assert.Equal(MyClass.LuaObjectProperty, (LuaObject)environment["table"]);
            Assert.Equal(MyClass.LuaTableProperty, (LuaTable)environment["table"]);
            Assert.Equal(MyClass.LuaFunctionProperty, (LuaFunction)environment["func"]);
            Assert.Equal(MyClass.LuaThreadProperty, (LuaThread)environment["thread"]);
        }

        [Fact]
        public void InvalidProperty_Get_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["MyClass"] = LuaValue.FromClrType(typeof(MyClass));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("_ = MyClass.InvalidProperty"));
            Assert.Contains("attempt to index invalid member 'InvalidProperty'", ex.Message);
        }
    }
}
