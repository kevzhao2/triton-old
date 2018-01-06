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
    public class FieldTests {
        private class TestClass {
            public int a = 105;
            public string b = "abcdefgh";
            public char c = '3';
            public object d = null;
            public double e = 16.7;
            public ulong f = 11111111;
            public decimal g = .51m;
            public float h = 0.516f;
            public object i = new object();
        }

        private static class TestClass2 {
            public static int a = 105;
            public static string b = "abcdefgh";
            public static char c = '3';
            public static object d = null;
            public static double e = 16.7;
            public static ulong f = 11111111;
            public static decimal g = .51m;

            public const int Constant = 0;
        }

        [Fact]
        public void GetInstanceFields() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("a, b, c, d, e, f, g = obj.a, obj.b, obj.c, obj.d, obj.e, obj.f, obj.g");

                Assert.Equal(105L, lua["a"]);
                Assert.Equal("abcdefgh", lua["b"]);
                Assert.Equal("3", lua["c"]);
                Assert.Null(lua["d"]);
                Assert.Equal(16.7, lua["e"]);
                Assert.Equal(11111111L, lua["f"]);
                Assert.Equal((double)(.51m), lua["g"]);
            }
        }

        [Fact]
        public void GetInstanceField_InvalidField() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.x"));
            }
        }

        [Fact]
        public void SetInstanceFields() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                var obj2 = new object();
                lua["obj"] = obj;
                lua["obj2"] = obj2;

                lua.DoString("obj.a, obj.b, obj.c, obj.d, obj.e, obj.f, obj.g, obj.h = -51, \"test\", \"=\", obj2, -7.1, -1, -1.5, 1.3");
                lua.DoString("obj.i = nil");

                Assert.Equal(-51, obj.a);
                Assert.Equal("test", obj.b);
                Assert.Equal('=', obj.c);
                Assert.Same(obj2, obj.d);
                Assert.Equal(-7.1, obj.e);
                Assert.Equal(ulong.MaxValue, obj.f);
                Assert.Equal((decimal)(-1.5), obj.g);
                Assert.Equal((double)(1.3f), obj.h);
                Assert.Null(obj.i);
            }
        }

        [Fact]
        public void SetInstanceField_Overflow() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.a = 3000000000"));
            }
        }

        [Fact]
        public void SetInstanceField_InvalidValue() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.a = \"b\""));
            }
        }

        [Fact]
        public void SetInstanceField_InvalidField() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.x = \"b\""));
            }
        }

        [Fact]
        public void GetStaticFields() {
            using (var lua = new Lua()) {
                TestClass2.a = 105;
                TestClass2.b = "abcdefgh";
                TestClass2.c = '3';
                TestClass2.d = null;
                TestClass2.e = 16.7;
                TestClass2.f = 11111111;
                TestClass2.g = .51m;

                lua.ImportType(typeof(TestClass2));

                lua.DoString("a, b, c, d, e = TestClass2.a, TestClass2.b, TestClass2.c, TestClass2.d, TestClass2.e");
                lua.DoString("f, g = TestClass2.f, TestClass2.g");

                Assert.Equal(105L, lua["a"]);
                Assert.Equal("abcdefgh", lua["b"]);
                Assert.Equal("3", lua["c"]);
                Assert.Null(lua["d"]);
                Assert.Equal(16.7, lua["e"]);
                Assert.Equal(11111111L, lua["f"]);
                Assert.Equal((double)(.51m), lua["g"]);
            }
        }

        [Fact]
        public void GetStaticField_InvalidField() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("a = TestClass2.x"));
            }
        }

        [Fact]
        public void SetStaticFields() {
            using (var lua = new Lua()) {
                var obj2 = new object();
                lua.ImportType(typeof(TestClass2));
                lua["obj2"] = obj2;

                lua.DoString("TestClass2.a, TestClass2.b, TestClass2.c, TestClass2.d = -51, \"test\", \"=\", obj2");
                lua.DoString("TestClass2.e, TestClass2.f, TestClass2.g = -777.1, -1, -1.5");

                Assert.Equal(-51, TestClass2.a);
                Assert.Equal("test", TestClass2.b);
                Assert.Equal('=', TestClass2.c);
                Assert.Same(obj2, TestClass2.d);
                Assert.Equal(-777.1, TestClass2.e);
                Assert.Equal(ulong.MaxValue, TestClass2.f);
                Assert.Equal((decimal)(-1.5), TestClass2.g);
            }
        }

        [Fact]
        public void SetStaticField_Constant() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.Constant = 156"));
            }
        }

        [Fact]
        public void SetStaticField_Overflow() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.a = 3000000000"));
            }
        }

        [Fact]
        public void SetStaticField_InvalidValue() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.a = \"b\""));
            }
        }

        [Fact]
        public void SetStaticField_InvalidField() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.x = \"b\""));
            }
        }
    }
}
