// Copyright (c) 2018 Kevin Zhao
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

namespace Triton.Tests.Integration {
    public class LuaThreadTest {
        private const string TestString = @"
            for i = 1, 10 do
                x = i
                coroutine.yield(i)
            end
            return -1";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                var function = lua.CreateFunction(TestString);
                var thread = lua.CreateThread(function);
                Assert.True(thread.CanResume);

                for (var i = 1; i <= 10; ++i) {
                    var results = thread.Resume();

                    Assert.Single(results);
                    Assert.Equal((long)i, results[0]);
                    Assert.Equal((long)i, lua["x"]);
                    Assert.True(thread.CanResume);
                }

                var results2 = thread.Resume();

                Assert.Single(results2);
                Assert.Equal(-1L, results2[0]);
                Assert.False(thread.CanResume);
            }
        }
    }
}
