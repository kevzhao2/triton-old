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
    public class InstancePropertyTests
    {
        public class TestClass
        {
            public int IntValue { get; set; }

            public double DoubleValue { get; set; }

            public string? StringValue { get; set; }
        }

        public class TestClassByRef
        {
            private int _intValue;

            public ref int IntValue => ref _intValue;
        }

        public class TestClassNonAssignable
        {
            [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Testing")]
            public int NoSetter => 1234;

            public int PrivateSetter { get; private set; }
        }

        [Theory]
        [InlineData(1234)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Get_Int(int value)
        {
            var testClass = new TestClass { IntValue = value };

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval($"assert(test_class.IntValue == {value})");
        }

        [Theory]
        [InlineData(1.234)]
        [InlineData(-1.234)]
        public void Get_Double(double value)
        {
            var testClass = new TestClass { DoubleValue = value };

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval($"assert(test_class.DoubleValue == {value})");
        }

        [Theory]
        [InlineData("test")]
        [InlineData(null)]
        public void Get_String(string? value)
        {
            var testClass = new TestClass { StringValue = value };

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval($"assert(test_class.StringValue == {(value is null ? "nil" : $"'{value}'")})");
        }

        [Fact]
        public void Set_NonAssignable_NoSetter_RaisesLuaError()
        {
            var testClass = new TestClassNonAssignable();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("test_class.NoSetter = 1234"));
            Assert.Contains("attempt to set non-assignable property 'TestClassNonAssignable.NoSetter'", ex.Message);
        }

        [Fact]
        public void Set_NonAssignable_PrivateSetter_RaisesLuaError()
        {
            var testClass = new TestClassNonAssignable();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("test_class.PrivateSetter = 1234"));
            Assert.Contains("attempt to set non-assignable property 'TestClassNonAssignable.PrivateSetter'", ex.Message);
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
            Assert.Contains("attempt to set property 'TestClass.IntValue' with an invalid value", ex.Message);
        }

        [Theory]
        [InlineData(1.234)]
        [InlineData(-1.234)]
        public void Set_Double(int value)
        {
            var testClass = new TestClass();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval($"test_class.DoubleValue = {value}");

            Assert.Equal(value, testClass.DoubleValue);
        }

        [Theory]
        [InlineData("'test'")]
        public void Set_Double_InvalidValue_RaisesLuaError(string value)
        {
            var testClass = new TestClass();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"test_class.DoubleValue = {value}"));
            Assert.Contains("attempt to set property 'TestClass.DoubleValue' with an invalid value", ex.Message);
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

        [Theory]
        [InlineData("1.234")]
        [InlineData("1234")]
        public void Set_String_InvalidValue_RaisesLuaError(string value)
        {
            var testClass = new TestClass();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"test_class.StringValue = {value}"));
            Assert.Contains("attempt to set property 'TestClass.StringValue' with an invalid value", ex.Message);
        }

        [Fact]
        public void Set_ByRef()
        {
            var testClass = new TestClassByRef();

            using var environment = new LuaEnvironment();
            environment["test_class"] = LuaValue.FromClrObject(testClass);

            environment.Eval("test_class.IntValue = 1234");

            Assert.Equal(1234, testClass.IntValue);
        }
    }
}
