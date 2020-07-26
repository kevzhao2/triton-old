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

namespace Triton
{
    public class LuaFunctionTests
    {
        [Fact]
        public void Call_LuaError_ThrowsLuaEvalException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("error('test')");

            Assert.Throws<LuaEvalException>(() => function.Call());
        }

        [Fact]
        public void Call_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 0");
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => function.Call());
        }

        [Fact]
        public void Call_NoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 0");

            var (result, _) = function.Call();

            Assert.Equal(0, (long)result);
        }

        [Fact]
        public void Call_OneArg()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var (result, _) = function.Call(1);

            Assert.Equal(1, (long)result);
        }

        [Fact]
        public void Call_TwoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var (result, _) = function.Call(1, 2);

            Assert.Equal(3, (long)result);
        }

        [Fact]
        public void Call_ThreeArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var (result, _) = function.Call(1, 2, 3);

            Assert.Equal(6, (long)result);
        }

        [Fact]
        public void Call_ManyArgs_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            Assert.Throws<ArgumentNullException>(() => function.Call(null!));
        }

        [Fact]
        public void Call_ManyArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            var (result, _) = function.Call(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.Equal(55, (long)result);
        }
    }
}
