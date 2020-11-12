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
    public class ArrayTests
    {
        [Fact]
        public void Get_SzArray()
        {
            var array = new int[10];
            array[1] = 1234;

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("assert(array[1] == 1234)");
        }

        [Theory]
        [InlineData("1.234")]
        [InlineData("test")]
        public void Get_SzArray_InvalidIndex_RaisesLuaError(string index)
        {
            var array = new int[10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"_ = array[{index}]"));
            Assert.Contains("attempt to index an array with an invalid index", ex.Message);
        }

        [Fact]
        public void Get_NdArray()
        {
            var array = new int[10, 10];
            array[1, 2] = 1234;

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("assert(array[{1, 2}] == 1234)");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("{1, 1.234}")]
        [InlineData("{1, 'test'}")]
        [InlineData("{1, 2, 3}")]
        public void Get_NdArray_InvalidIndices_RaisesLuaError(string indices)
        {
            var array = new int[10, 10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"_ = array[{indices}]"));
            Assert.Contains("attempt to index a multi-dimensional array with invalid indices", ex.Message);
        }

        [Fact]
        public void Set_SzArray()
        {
            var array = new int[10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[1] = 1234");

            Assert.Equal(1234, array[1]);
        }

        [Theory]
        [InlineData("1.234")]
        [InlineData("test")]
        public void Set_SzArray_InvalidIndex_RaisesLuaError(string index)
        {
            var array = new int[10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"array[{index}] = 1234"));
            Assert.Contains("attempt to index an array with an invalid index", ex.Message);
        }

        [Fact]
        public void Set_SzArray_InvalidValue_RaisesLuaError()
        {
            var array = new int[10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("array[1] = 1.234"));
            Assert.Contains("attempt to set an array with an invalid value", ex.Message);
        }

        [Fact]
        public void Set_NdArray()
        {
            var array = new int[10, 10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[{1, 2}] = 1234");

            Assert.Equal(1234, array[1, 2]);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("{1, 1.234}")]
        [InlineData("{1, 'test'}")]
        [InlineData("{1, 2, 3}")]
        public void Set_NdArray_InvalidIndices_RaisesLuaError(string indices)
        {
            var array = new int[10, 10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval($"array[{indices}] = 1234"));
            Assert.Contains("attempt to index a multi-dimensional array with invalid indices", ex.Message);
        }

        [Fact]
        public void Set_NdArray_InvalidValue_RaisesLuaError()
        {
            var array = new int[10, 10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("array[{1, 2}] = 1.234"));
            Assert.Contains("attempt to set a multi-dimensional array with an invalid value", ex.Message);
        }
    }
}
