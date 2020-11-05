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

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Triton.Interop
{
    public class StaticFieldTests
    {
        public class TestClass
        {
            [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Testing")]
            public static int IntValue;

            [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Testing")]
            public static string? StringValue;
        }

        public class TestClassNonAssignable
        {
            public const int Const = 1234;

            public static readonly int ReadOnly = 1234;
        }

        [Fact]
        public void Set_NonAssignable_Const_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClassNonAssignable) });

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("TestClass.Const = 1234"));
            Assert.Contains("attempt to set non-assignable field `TestClassNonAssignable.Const`", ex.Message);
        }

        [Fact]
        public void Set_NonAssignable_ReadOnly_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClassNonAssignable) });

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("TestClass.ReadOnly = 1234"));
            Assert.Contains("attempt to set non-assignable field `TestClassNonAssignable.ReadOnly`", ex.Message);
        }

        [Theory]
        [InlineData(1234)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Set_Int(int value)
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClass) });

            environment.Eval($"TestClass.IntValue = {value}");

            Assert.Equal(value, TestClass.IntValue);
        }

        [Theory]
        [InlineData("1.234")]
        [InlineData("-2147483649")]
        [InlineData("2147483648")]
        [InlineData("'test'")]
        public void Set_Int_InvalidValue_RaisesLuaError(string value)
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClass) });

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"TestClass.IntValue = {value}"));
            Assert.Contains("attempt to set field `TestClass.IntValue` with invalid value", ex.Message);
        }

        [Theory]
        [InlineData("test")]
        [InlineData(null)]
        public void Set_String(string? value)
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClass) });

            environment.Eval($"TestClass.StringValue = {(value is null ? "nil" : $"'{value}'")}");

            Assert.Equal(value, TestClass.StringValue);
        }
    }
}
