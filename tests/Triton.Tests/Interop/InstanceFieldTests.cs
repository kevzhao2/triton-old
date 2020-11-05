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
    public class InstanceFieldTests
    {
        public class TestClass
        {
            public int IntValue;

            public string? StringValue;
        }

        public class TestClassNonAssignable
        {
            public readonly int ReadOnly = 1234;
        }

        [Fact]
        public void Set_NonAssignable_ReadOnly_RaisesLuaError()
        {
            var testClass = new TestClassNonAssignable();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("test_class.ReadOnly = 1234"));
            Assert.Contains("attempt to set non-assignable field `TestClassNonAssignable.ReadOnly`", ex.Message);
        }

        [Theory]
        [InlineData(1234)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Set_Int(int value)
        {
            var testClass = new TestClass();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval($"test_class.IntValue = {value}");

            Assert.Equal(value, testClass.IntValue);
        }

        [Theory]
        [InlineData("1.234")]
        [InlineData("-2147483649")]
        [InlineData("2147483648")]
        [InlineData("'test'")]
        public void Set_Int_InvalidValue_RaisesLuaError(string value)
        {
            var testClass = new TestClass();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"test_class.IntValue = {value}"));
            Assert.Contains("attempt to set field `TestClass.IntValue` with invalid value", ex.Message);
        }

        [Theory]
        [InlineData("test")]
        [InlineData(null)]
        public void Set_String(string? value)
        {
            var testClass = new TestClass();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval($"test_class.StringValue = {(value is null ? "nil" : $"'{value}'")}");

            Assert.Equal(value, testClass.StringValue);
        }
    }
}
