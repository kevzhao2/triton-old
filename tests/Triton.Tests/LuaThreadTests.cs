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
        public void IsReady_Get_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();

            Assert.True(thread.IsReady);
        }

        [Fact]
        public void IsReady_Get_Yielded_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction("coroutine.yield()");
            thread.SetFunction(function);
            thread.Resume();

            Assert.False(thread.IsReady);
        }

        [Fact]
        public void IsReady_Get_Errored_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction("error('test')");
            thread.SetFunction(function);
            try
            {
                thread.Resume();
            }
            catch (LuaRuntimeException)
            {
            }

            Assert.True(thread.IsReady);
        }

        [Fact]
        public void Resume_NoFunction_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();

            Assert.Throws<InvalidOperationException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_LuaRuntimeError_ThrowsLuaRuntimeException()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction("error('test')");
            thread.SetFunction(function);

            Assert.Throws<LuaRuntimeException>(() => thread.Resume());
        }

        [Fact]
        public void Resume_NoArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(0, (long)thread.Resume());
        }

        [Fact]
        public void Resume_OneArgument()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(1, (long)thread.Resume(1));
        }

        [Fact]
        public void Resume_TwoArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(3, (long)thread.Resume(1, 2));
        }

        [Fact]
        public void Resume_ThreeArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(6, (long)thread.Resume(1, 2, 3));
        }

        [Fact]
        public void Resume_FourArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(10, (long)thread.Resume(1, 2, 3, 4));
        }

        [Fact]
        public void Resume_FiveArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(15, (long)thread.Resume(1, 2, 3, 4, 5));
        }

        [Fact]
        public void Resume_SixArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(21, (long)thread.Resume(1, 2, 3, 4, 5, 6));
        }

        [Fact]
        public void Resume_SevenArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(28, (long)thread.Resume(1, 2, 3, 4, 5, 6, 7));
        }

        [Fact]
        public void Resume_EightArguments()
        {
            using var environment = new LuaEnvironment();
            using var thread = environment.CreateThread();
            using var function = environment.CreateFunction(@"
                result = 0
                for _, val in ipairs({...}) do
                    result = result + val
                end
                coroutine.yield(result)");
            thread.SetFunction(function);

            Assert.Equal(36, (long)thread.Resume(1, 2, 3, 4, 5, 6, 7, 8));
        }
    }
}
