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

namespace Triton.Tests.Binding {
    public class EnumTests {
        private enum TestEnum {
            A, B, C, D, E, F, G
        }

        [Fact]
        public void GetValue() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestEnum));

                lua.DoString("f = TestEnum.F");

                Assert.Equal((long)TestEnum.F, lua["f"]);
            }
        }

        [Fact]
        public void SetValue_Fails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestEnum));

                Assert.Throws<LuaException>(() => lua.DoString("TestEnum.G = 0"));
            }
        }
    }
}
