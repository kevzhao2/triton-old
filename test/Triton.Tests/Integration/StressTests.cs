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
    public class StressTests {
        private class TestClass {
            public int TestProperty { get; set; }

            public void TestMethod() {
            }
            public void TestMethod<T>() {
            }
            public void TestMethod2(int x, int y) {
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void GetProperty(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("x = test.TestProperty");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void SetProperty(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("test.TestProperty = 0");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void CallMethod(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("test:TestMethod()");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void CallMethod_Args(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("test:TestMethod2(0, 0)");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void CallGenericMethod(int n) {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("test:TestMethod(Int32)()");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(10000)]
        public void LotsOfReferencesCleanedUp(int n) {
            using (var lua = new Lua()) {
                var function = lua.CreateFunction("x = 0 + 0");

                for (var i = 0; i < n; ++i) {
                    lua.CreateTable();
                    lua.CreateTable();
                    lua.CreateTable();

                    for (var j = 0; j < 1000; ++j) {
                        function.Call();
                    }
                }
            }
        }
    }
}
