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

using System;
using Xunit;

namespace Triton.Tests.Binding {
    public class DelegateTests {
        public delegate void TestDelegate(out int x, out int y);

        [Fact]
        public void Call_NoArgs() {
            using (var lua = new Lua()) {
                var x = 0;
                void a() => x = 5;
                lua["obj"] = (Action)a;

                lua.DoString("obj()");

                Assert.Equal(5, x);
            }
        }

        [Fact]
        public void Call_ManyArgs() {
            using (var lua = new Lua()) {
                var x = 0;
                void a(int y, int z) => x = y + z;
                lua["obj"] = (Action<int, int>)a;

                lua.DoString("obj(3, 7)");

                Assert.Equal(10, x);
            }
        }

        [Fact]
        public void Call_OneResult() {
            using (var lua = new Lua()) {
                int a(int x) => x * 40;
                lua["obj"] = (Func<int, int>)a;

                lua.DoString("a = obj(5)");

                Assert.Equal(200L, lua["a"]);
            }
        }

        [Fact]
        public void Call_TwoResults() {
            using (var lua = new Lua()) {
                void a(out int x, out int y) {
                    x = 10;
                    y = 20;
                }
                lua["obj"] = (TestDelegate)a;

                lua.DoString("x, y = obj()");

                Assert.Equal(10L, lua["x"]);
                Assert.Equal(20L, lua["y"]);
            }
        }

        [Fact]
        public void Call_NonDelegate() {
            using (var lua = new Lua()) {
                lua["obj"] = new object();

                Assert.Throws<LuaException>(() => lua.DoString("obj()"));
            }
        }

        [Fact]
        public void Call_InvalidArgs() {
            using (var lua = new Lua()) {
                lua["obj"] = new Func<int, int>(a => a);

                Assert.Throws<LuaException>(() => lua.DoString("obj(\"x\")"));
            }
        }

        [Fact]
        public void Call_ThrowsException() {
            using (var lua = new Lua()) {
                lua["obj"] = new Action(() => throw new NotImplementedException());

                Assert.Throws<LuaException>(() => lua.DoString("obj()"));
            }
        }
    }
}
