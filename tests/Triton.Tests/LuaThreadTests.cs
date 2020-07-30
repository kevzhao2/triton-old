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
    public class LuaThreadTests
    {
        [Fact]
        public void CanStart_GetEnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => thread.CanStart);
        }

        [Fact]
        public void CanStart_Get_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.True(thread.CanStart);
        }

        [Fact]
        public void CanStart_Get_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("coroutine.yield()");
            var thread = environment.CreateThread();
            thread.Start(function);

            Assert.False(thread.CanStart);
        }

        [Fact]
        public void CanResume_GetEnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => thread.CanResume);
        }

        [Fact]
        public void CanResume_Get_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("coroutine.yield()");
            var thread = environment.CreateThread();
            thread.Start(function);

            Assert.True(thread.CanResume);
        }

        [Fact]
        public void CanResume_Get_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.False(thread.CanResume);
        }

        [Fact]
        public void Start_NullFunction_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.Throws<ArgumentNullException>(() => thread.Start(null!));
        }

        [Fact]
        public void Start_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            environment.Dispose();

            Assert.Throws<ArgumentNullException>(() => thread.Start(null!));
        }

        [Fact]
        public void Start_CannotStart_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("coroutine.yield()");
            var thread = environment.CreateThread();
            thread.Start(function);

            Assert.Throws<InvalidOperationException>(() => thread.Start(function));
        }

        [Fact]
        public void Start_LuaError_ThrowsLuaEvalException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("error('test')");
            var thread = environment.CreateThread();

            Assert.Throws<LuaEvalException>(() => thread.Start(function));
        }

        [Fact]
        public void Start_NoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 0");
            var thread = environment.CreateThread();

            var (result, _) = thread.Start(function);

            Assert.Equal(0, (long)result);
        }

        [Fact]
        public void Start_OneArg()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            var thread = environment.CreateThread();

            var (result, _) = thread.Start(function, 1);

            Assert.Equal(1, (long)result);
        }

        [Fact]
        public void Start_TwoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            var thread = environment.CreateThread();

            var (result, _) = thread.Start(function, 1, 2);

            Assert.Equal(3, (long)result);
        }

        [Fact]
        public void Start_ThreeArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            var thread = environment.CreateThread();

            var (result, _) = thread.Start(function, 1, 2, 3);

            Assert.Equal(6, (long)result);
        }

        [Fact]
        public void Start_ManyArgs_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            var thread = environment.CreateThread();

            Assert.Throws<ArgumentNullException>(() => thread.Start(function, null!));
        }

        [Fact]
        public void Start_ManyArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                return result");
            var thread = environment.CreateThread();

            var (result, _) = thread.Start(function, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.Equal(55, (long)result);
        }

        [Fact]
        public void Resume_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                coroutine.yield()
                error('test')");
            var thread = environment.CreateThread();
            thread.Start(function);
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_CannotResume_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.Throws<InvalidOperationException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_LuaError_ThrowsLuaEvalException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                coroutine.yield()
                error('test')");
            var thread = environment.CreateThread();
            thread.Start(function);

            Assert.Throws<LuaEvalException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_NoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                coroutine.yield()
                return 0");
            var thread = environment.CreateThread();
            thread.Start(function);

            var (result, _) = thread.Resume();

            Assert.Equal(0, (long)result);
        }

        [Fact]
        public void Resume_OneArg()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                arg = coroutine.yield()
                return arg");
            var thread = environment.CreateThread();
            thread.Start(function);

            var (result, _) = thread.Resume(1);

            Assert.Equal(1, (long)result);
        }

        [Fact]
        public void Resume_TwoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                arg, arg2 = coroutine.yield()
                return arg + arg2");
            var thread = environment.CreateThread();
            thread.Start(function);

            var (result, _) = thread.Resume(1, 2);

            Assert.Equal(3, (long)result);
        }

        [Fact]
        public void Resume_ThreeArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                arg, arg2, arg3 = coroutine.yield()
                return arg + arg2 + arg3");
            var thread = environment.CreateThread();
            thread.Start(function);

            var (result, _) = thread.Resume(1, 2, 3);

            Assert.Equal(6, (long)result);
        }

        [Fact]
        public void Resume_ManyArgs_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                arg, arg2, arg3, arg4 = coroutine.yield()
                return arg + arg2 + arg3 + arg4");
            var thread = environment.CreateThread();
            thread.Start(function);

            Assert.Throws<ArgumentNullException>(() => thread.Resume(null!));
        }

        [Fact]
        public void Resume_ManyArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                arg, arg2, arg3, arg4 = coroutine.yield()
                return arg + arg2 + arg3 + arg4");
            var thread = environment.CreateThread();
            thread.Start(function);

            var (result, _) = thread.Resume(1, 2, 3, 4);

            Assert.Equal(10, (long)result);
        }
    }
}
