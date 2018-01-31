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
    public class ConstructorTests {
        private class TestClass {
            public int X { get; set; }

            public TestClass() : this(10) { }

            public TestClass(int x) {
                X = x;
            }

            public TestClass(string x) => throw new NotImplementedException();
        }

        private class TestClass2 {
            private TestClass2() { }
        }

        private abstract class TestClass3 { }
        private interface ITest { }

        [Fact]
        public void Constructor_NoArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                lua.DoString("obj = TestClass()");

                Assert.IsType<TestClass>(lua["obj"]);
                Assert.Equal(10L, ((TestClass)lua["obj"]).X);
            }
        }

        [Fact]
        public void Constructor_OneArg() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                lua.DoString("obj = TestClass(6178)");

                Assert.IsType<TestClass>(lua["obj"]);
                Assert.Equal(6178L, ((TestClass)lua["obj"]).X);
            }
        }

        [Fact]
        public void Constructor_AbstractType() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass3));

                Assert.Throws<LuaException>(() => lua.DoString("obj = TestClass3(\"what\")"));
            }
        }

        [Fact]
        public void Constructor_Interface() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(ITest));

                Assert.Throws<LuaException>(() => lua.DoString("obj = ITest(\"what\")"));
            }
        }

        [Fact]
        public void Constructor_NotAvailable() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("obj = TestClass2(\"what\")"));
            }
        }

        [Fact]
        public void Constructor_InvalidArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                Assert.Throws<LuaException>(() => lua.DoString("obj = TestClass({})"));
            }
        }

        [Fact]
        public void Constructor_ThrowsException() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                Assert.Throws<LuaException>(() => lua.DoString("obj = TestClass(\"what\")"));
            }
        }
    }
}
