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
    public class IOTest {
        private const string TestString = @"
            using 'System'
            using 'System.IO'

            path = Path.GetTempFileName()
            File.WriteAllText(path, 'string1\nstring2\nstring3\nstring4\nstring5\nstring6')

            sr = StreamReader(path)
            pcall(function()
                count = 1
                line = sr:ReadLine()
                while line ~= nil do
                    assert(line == 'string' .. count)
                    count = count + 1
                    line = sr:ReadLine()
                end
            end)
            sr:Dispose()";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                lua.DoString(TestString);
            }
        }
    }
}
