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
    public class ArrayTests {
        [Fact]
        public void GetArray() {
            using (var lua = new Lua()) {
                lua["arr"] = new[] { 3, 1, 4 };

                lua.DoString("x = arr[1]");

                Assert.Equal(1L, lua["x"]);
            }
        }

        [Fact]
        public void GetArray_NotArray() {
            using (var lua = new Lua()) {
                lua["arr"] = new object();

                Assert.Throws<LuaException>(() => lua.DoString("x = arr[1]"));
            }
        }

        [Fact]
        public void GetArray_Not1D() {
            using (var lua = new Lua()) {
                lua["arr"] = new int[2, 2];

                Assert.Throws<LuaException>(() => lua.DoString("x = arr[1]"));
            }
        }

        [Fact]
        public void GetArray_OutOfBounds() {
            using (var lua = new Lua()) {
                lua["arr"] = new int[3];

                Assert.Throws<LuaException>(() => lua.DoString("x = arr[-1]"));
            }
        }

        [Fact]
        public void SetArray() {
            using (var lua = new Lua()) {
                var arr = new int[3];
                lua["arr"] = arr;

                lua.DoString("arr[0] = 3");

                Assert.Equal(3, arr[0]);
            }
        }

        [Fact]
        public void SetArray_NotArray() {
            using (var lua = new Lua()) {
                lua["arr"] = new object();

                Assert.Throws<LuaException>(() => lua.DoString("arr[1] = 3"));
            }
        }

        [Fact]
        public void SetArray_Not1D() {
            using (var lua = new Lua()) {
                lua["arr"] = new int[2, 2];

                Assert.Throws<LuaException>(() => lua.DoString("arr[1] = 3"));
            }
        }

        [Fact]
        public void SetArray_OutOfBounds() {
            using (var lua = new Lua()) {
                lua["arr"] = new int[3];

                Assert.Throws<LuaException>(() => lua.DoString("arr[-1] = 3"));
            }
        }

        [Fact]
        public void SetArray_InvalidValue() {
            using (var lua = new Lua()) {
                lua["arr"] = new string[3];

                Assert.Throws<LuaException>(() => lua.DoString("arr[1] = 300"));
            }
        }
    }
}
