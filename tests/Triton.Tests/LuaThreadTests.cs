﻿// Copyright (c) 2020 Kevin Zhao
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
        public void IsReady_Get()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.True(thread.IsReady);

            thread.SetFunction(environment.CreateFunction("coroutine.yield()"));
            thread.Resume();

            Assert.False(thread.IsReady);
        }

        [Fact]
        public void Resume_NullArgs_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.Throws<ArgumentNullException>(() => thread.Resume(null!));
        }

        [Fact]
        public void Resume_NoFunction_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.Throws<InvalidOperationException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_LuaRuntimeError_ThrowsLuaRuntimeException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction("error('test')"));

            Assert.Throws<LuaRuntimeException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_NoArguments()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)"));

            var (result, _) = thread.Resume();

            Assert.Equal(0, result);
        }

        [Fact]
        public void Resume_OneArgument()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)"));

            var (result, _) = thread.Resume(1);

            Assert.Equal(1, result);
        }

        [Fact]
        public void Resume_TwoArguments()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)"));

            var (result, _) = thread.Resume(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void Resume_ThreeArguments()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)"));

            var (result, _) = thread.Resume(1, 2, 3);

            Assert.Equal(6, result);
        }

        [Fact]
        public void Resume_ManyArguments()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)"));

            var (result, _) = thread.Resume(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.Equal(55, result);
        }

        [Fact]
        public void SetFunction_NullFunction_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();

            Assert.Throws<ArgumentNullException>(() => thread.SetFunction(null!));
        }

        [Fact]
        public void SetFunction_IsNotReady_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction("coroutine.yield()"));
            thread.Resume();

            Assert.Throws<InvalidOperationException>(() => thread.SetFunction(environment.CreateFunction("")));
        }
    }
}
