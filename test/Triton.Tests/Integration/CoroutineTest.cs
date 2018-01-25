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
    public class CoroutineTest {
        private const string TestString = @"
            using 'System'
            using 'System.Collections.Generic'

            list = List(String)()
            co = coroutine.create(function()
                list:Add('checkpoint 1')
                coroutine.yield()
                list:Add('checkpoint 2')
                coroutine.yield()
                list:Add('checkpoint 3')
                coroutine.yield()
                list = List(String)()
                list:Add('checkpoint 4')
                coroutine.yield()
                list:Clear()
            end)

            coroutine.resume(co)
            assert(list.Count == 1 and list.Item:Get(0) == 'checkpoint 1')
            coroutine.resume(co)
            assert(list.Count == 2 and list.Item:Get(1) == 'checkpoint 2')
            coroutine.resume(co)
            assert(list.Count == 3 and list.Item:Get(2) == 'checkpoint 3')
            coroutine.resume(co)
            assert(list.Count == 1 and list.Item:Get(0) == 'checkpoint 4')
            coroutine.resume(co)
            assert(list.Count == 0)";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                lua.DoString(TestString);
            }
        }
    }
}
