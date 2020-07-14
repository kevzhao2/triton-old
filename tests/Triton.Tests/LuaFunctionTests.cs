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
        public void Call_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234");

            Assert.Throws<ArgumentNullException>(() => function.Call(null!));
        }

        [Fact]
        public void Call_NoArgs_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234");
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => function.Call());
        }

        [Fact]
        public void Call_NoArgs_OneResult()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234");

            Assert.Collection(function.Call(),
                value => Assert.Equal(1234L, value));
        }

        [Fact]
        public void Call_OneArg_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return ...");
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => function.Call(5678));
        }

        [Fact]
        public void Call_OneArg_OneResult()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return ...");

            Assert.Collection(function.Call(5678),
                value => Assert.Equal(5678L, value));
        }

        [Fact]
        public void Call_TwoArgs_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => function.Call(1234, 5678));
        }

        [Fact]
        public void Call_TwoArgs_OneResult()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            Assert.Collection(function.Call(1234, 5678),
                value => Assert.Equal(6912L, value));
        }

        [Fact]
        public void Call_ThreeArgs_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => function.Call(1, 2, 3));
        }

        [Fact]
        public void Call_ThreeArgs_OneResult()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            Assert.Collection(function.Call(1, 2, 3),
                value => Assert.Equal(6L, value));
        }

        [Fact]
        public void Call_ManyArgs_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => function.Call(1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
        }

        [Fact]
        public void Call_ManyArgs_OneResult()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");

            Assert.Collection(function.Call(1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
                value => Assert.Equal(55L, value));
        }

        [Fact]
        public void Call_NoArgs_TwoResults()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234, 5678");

            Assert.Collection(function.Call(),
                value => Assert.Equal(1234L, value),
                value => Assert.Equal(5678L, value));
        }

        [Fact]
        public void Call_NoArgs_ThreeResults()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1, 2, 3");

            Assert.Collection(function.Call(),
                value => Assert.Equal(1L, value),
                value => Assert.Equal(2L, value),
                value => Assert.Equal(3L, value));
        }

        [Fact]
        public void Call_LuaError_ThrowsLuaExecutionException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("error('test')");

            Assert.Throws<LuaExecutionException>(() => function.Call());
        }
    }
}
