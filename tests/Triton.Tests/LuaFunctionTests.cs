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

using System.Text;
using Xunit;

namespace Triton
{
    public class LuaFunctionTests
    {
        [Fact]
        public void Call_NoArgs_OneResult()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction("return 1234");

            var results = function.Call();
            Assert.Collection(results,
                value => Assert.Equal(1234L, value));
        }

        [Fact]
        public void Call_OneArg_OneResult()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction("return ...");

            var results = function.Call(5678);
            Assert.Collection(results,
                value => Assert.Equal(5678L, value));
        }

        [Fact]
        public void Call_TwoArgs_OneResult()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var results = function.Call(1234, 5678);
            Assert.Collection(results,
                value => Assert.Equal(6912L, value));
        }

        [Fact]
        public void Call_ThreeArgs_OneResult()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var results = function.Call(1, 2, 3);
            Assert.Collection(results,
                value => Assert.Equal(6L, value));
        }

        [Fact]
        public void Call_NoArgs_TwoResults()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction("return 1234, 5678");

            var results = function.Call();
            Assert.Collection(results,
                value => Assert.Equal(1234L, value),
                value => Assert.Equal(5678L, value));
        }

        [Fact]
        public void Call_NoArgs_ThreeResults()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction("return 1, 2, 3");

            var results = function.Call();
            Assert.Collection(results,
                value => Assert.Equal(1L, value),
                value => Assert.Equal(2L, value),
                value => Assert.Equal(3L, value));
        }

        [Fact]
        public void Call_LuaError_ThrowsLuaException()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction("error('test')");

            Assert.Throws<LuaException>(() => function.Call());
        }
    }
}
