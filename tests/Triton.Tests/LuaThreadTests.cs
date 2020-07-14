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
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using Xunit;

namespace Triton
{
    public class LuaThreadTests
    {
        [Fact]
        public void CanStart_Get_EnvironmentDisposed_ThrowsObjectDisposedException()
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
            _ = thread.Start(function);

            Assert.False(thread.CanStart);
        }

        [Fact]
        public void CanResume_Get_EnvironmentDisposed_ThrowsObjectDisposedException()
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
            _ = thread.Start(function);

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
        public void Start_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234");
            var thread = environment.CreateThread();

            Assert.Throws<ArgumentNullException>(() => thread.Start(function, null!));
        }

        [Fact]
        public void Start_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234");
            var thread = environment.CreateThread();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => thread.Start(function));
        }

        [Fact]
        public void Start_CannotStart_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("coroutine.yield()");
            var thread = environment.CreateThread();
            _ = thread.Start(function);

            Assert.Throws<InvalidOperationException>(() => thread.Start(function));
        }

        [Fact]
        public void Start_LuaError_ThrowsLuaEvaluationException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("error('test')");
            var thread = environment.CreateThread();

            Assert.Throws<LuaEvaluationException>(() => thread.Start(function));
        }

        [Fact]
        public void Start_NoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 1234");
            var thread = environment.CreateThread();

            Assert.Collection(thread.Start(function),
                value => Assert.Equal(1234L, value));
        }

        [Fact]
        public void Start_OneArg()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return ...");
            var thread = environment.CreateThread();

            Assert.Collection(thread.Start(function, 5678),
                value => Assert.Equal(5678L, value));
        }

        [Fact]
        public void Resume_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("coroutine.yield()");
            var thread = environment.CreateThread();
            _ = thread.Start(function);

            Assert.Throws<ArgumentNullException>(() => thread.Resume(null!));
        }

        [Fact]
        public void Resume_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var function = environment.CreateFunction("coroutine.yield()");
            var thread = environment.CreateThread();
            _ = thread.Start(function);
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
        public void Resume_LuaError_ThrowsLuaEvaluationException()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                coroutine.yield()
                error('test')");
            var thread = environment.CreateThread();
            _ = thread.Start(function);

            Assert.Throws<LuaEvaluationException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_NoArgs()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                coroutine.yield()
                return 1234");
            var thread = environment.CreateThread();
            _ = thread.Start(function);

            Assert.Collection(thread.Resume(),
                value => Assert.Equal(1234L, value));
        }

        [Fact]
        public void Resume_OneArg()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction(@"
                val = coroutine.yield()
                return 2 * val");
            var thread = environment.CreateThread();
            _ = thread.Start(function);

            Assert.Collection(thread.Resume(1234),
                value => Assert.Equal(2468L, value));
        }
    }
}
