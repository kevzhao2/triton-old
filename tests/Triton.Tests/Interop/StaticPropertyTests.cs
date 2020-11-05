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

using Xunit;

namespace Triton.Interop
{
    public class StaticPropertyTests
    {
        public class TestClass
        {
            public static int IntValue { get; set; }

            public static string? StringValue { get; set; }
        }

        public class TestClassByRef
        {
            private static int _intValue;

            public static ref int IntValue => ref _intValue;
        }

        public class TestClassNonAssignable
        {
            public static int NoSetter => 1234;

            public static int PrivateSetter { get; private set; }
        }

        [Fact]
        public void Set_NonAssignable_NoSetter_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClassNonAssignable) });

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("TestClass.NoSetter = 1234"));
            Assert.Contains("attempt to set non-assignable property `TestClassNonAssignable.NoSetter`", ex.Message);
        }

        [Fact]
        public void Set_NonAssignable_PrivateSetter_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClassNonAssignable) });

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("TestClass.PrivateSetter = 1234"));
            Assert.Contains("attempt to set non-assignable property `TestClassNonAssignable.PrivateSetter`", ex.Message);
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
            Assert.Contains("attempt to set property `TestClass.IntValue` with invalid value", ex.Message);
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

        [Fact]
        public void Set_ByRef()
        {
            using var environment = new LuaEnvironment();
            environment["TestClass"] = LuaValue.FromClrTypes(new[] { typeof(TestClass) });

            environment.Eval("TestClass.IntValue = 1234");

            Assert.Equal(1234, TestClass.IntValue);
        }
    }
}
