﻿// Copyright (c) 2018 Kevin Zhao
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
    public class NestedTypeTests {
        private class TestClass {
            public class NestedClass {
                public int X { get; }

                public static int StaticX => 11266;

                public NestedClass(int x) => X = x;
            }
        }

        [Fact]
        public void ConstructNestedClass() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                lua.DoString("obj = TestClass.NestedClass(167)");

                var obj = lua["obj"] as TestClass.NestedClass;
                Assert.Equal(167, obj.X);
            }
        }
        
        [Fact]
        public void GetNestedStaticProperty() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                lua.DoString("x = TestClass.NestedClass.StaticX");

                Assert.Equal(11266L, lua["x"]);
            }
        }

        [Fact]
        public void SetNestedType_Fails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass.NestedClass = nil"));
            }
        }
    }
}