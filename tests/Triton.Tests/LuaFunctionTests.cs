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

namespace Triton
{
    public class LuaFunctionTests
    {
        [Fact]
        public void Call_LuaRuntimeError_ThrowsLuaRuntimeException()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction("error('test')");

            Assert.Throws<LuaRuntimeException>(() => function.Call());
        }

        [Fact]
        public void Call_NoArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call();

            Assert.Equal(0, (long)result);
        }

        [Fact]
        public void Call_OneArgument()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1);

            Assert.Equal(1, (long)result);
        }

        [Fact]
        public void Call_TwoArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2);

            Assert.Equal(3, (long)result);
        }

        [Fact]
        public void Call_ThreeArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2, 3);

            Assert.Equal(6, (long)result);
        }

        [Fact]
        public void Call_FourArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2, 3, 4);

            Assert.Equal(10, (long)result);
        }

        [Fact]
        public void Call_FiveArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2, 3, 4, 5);

            Assert.Equal(15, (long)result);
        }

        [Fact]
        public void Call_SixArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2, 3, 4, 5, 6);

            Assert.Equal(21, (long)result);
        }

        [Fact]
        public void Call_SevenArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2, 3, 4, 5, 6, 7);

            Assert.Equal(28, (long)result);
        }

        [Fact]
        public void Call_EightArguments()
        {
            using var environment = new LuaEnvironment();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var result = function.Call(1, 2, 3, 4, 5, 6, 7, 8);

            Assert.Equal(36, (long)result);
        }
    }
}
