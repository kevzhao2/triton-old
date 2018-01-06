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
    public class PropertyTests {
        private class TestClass {
            public int A { get; set; } = 105;
            public string B { get; set; } = "abcdefgh";
            public char C { get; set; } = '3';
            public object D { get; set; } = null;
            public double E { get; set; } = 16.7;
            public ulong F { get; set; } = 11111111;
            public decimal G { get; set; } = .51m;

            public int NoGetter { set { } }
            public int NoSetter => 10;

            public int ThrowsException {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public int this[int a] {
                get {
                    if (a == -567) {
                        throw new NotImplementedException();
                    }
                    return a;
                }
                set {
                    if (a == -567) {
                        throw new NotImplementedException();
                    }
                    A = a * value;
                }
            }
        }

        private static class TestClass2 {
            public static int A { get; set; } = 105;
            public static string B { get; set; } = "abcdefgh";
            public static char C { get; set; } = '3';
            public static object D { get; set; } = null;
            public static double E { get; set; } = 16.7;
            public static ulong F { get; set; } = 11111111;
            public static decimal G { get; set; } = .51m;

            public static int NoGetter { set { } }
            public static int NoSetter => 10;

            public static int ThrowsException {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
        }

        private class TestClass3 {
            public int this[int a] => a;
        }

        private class TestClass4 {
            public int this[int a] {
                set { }
            }
        }

        [Fact]
        public void GetInstanceProperties() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("a, b, c, d, e, f, g = obj.A, obj.B, obj.C, obj.D, obj.E, obj.F, obj.G");

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
        public void GetInstanceProperty_IndexedGetter() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("a = obj.Item:Get(6)");

                Assert.Equal(6L, lua["a"]);
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedGetter_None() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass4();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.Item:Get(-567)"));
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedGetter_InvalidIndex() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.Item:Get(\"w\")"));
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedGetter_ThrowsException() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.Item:Get(-567)"));
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedSetter() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("obj.Item:Set(10, 40)");

                Assert.Equal(400L, obj.A);
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedSetter_None() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass3();

                Assert.Throws<LuaException>(() => lua.DoString("obj.Item:Set(5, -567)"));
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedSetter_InvalidValue() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.Item:Set(\"a\", 10)"));
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedSetter_InvalidIndex() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.Item:Set(5, \"w\")"));
            }
        }

        [Fact]
        public void GetInstanceProperty_IndexedSetter_ThrowsException() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.Item:Set(5, -567)"));
            }
        }

        [Fact]
        public void GetInstanceProperty_NoGetter() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.NoGetter"));
            }
        }

        [Fact]
        public void GetInstanceProperty_ThrowsException() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.ThrowsException"));
            }
        }

        [Fact]
        public void GetInstanceProperty_InvalidProperty() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("a = obj.X"));
            }
        }

        [Fact]
        public void SetInstanceProperties() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                var obj2 = new object();
                lua["obj"] = obj;
                lua["obj2"] = obj2;

                lua.DoString("obj.A, obj.B, obj.C, obj.D, obj.E, obj.F, obj.G = -51, \"test\", \"=\", obj2, -777.1, -1, -1.5");

                Assert.Equal(-51, obj.A);
                Assert.Equal("test", obj.B);
                Assert.Equal('=', obj.C);
                Assert.Same(obj2, obj.D);
                Assert.Equal(-777.1, obj.E);
                Assert.Equal(ulong.MaxValue, obj.F);
                Assert.Equal((decimal)(-1.5), obj.G);
            }
        }

        [Fact]
        public void SetInstanceProperty_NoSetter() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.NoSetter = 15"));
            }
        }

        [Fact]
        public void SetInstanceProperty_Indexed() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.Item = 15"));
            }
        }

        [Fact]
        public void SetInstanceProperty_Overflow() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.A = 3000000000"));
            }
        }

        [Fact]
        public void SetInstanceProperty_InvalidValue() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.A = \"b\""));
            }
        }

        [Fact]
        public void SetInstanceProperty_ThrowsException() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.ThrowsException = 10"));
            }
        }

        [Fact]
        public void SetInstanceProperty_InvalidProperty() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.X = \"b\""));
            }
        }

        [Fact]
        public void GetStaticProperties() {
            using (var lua = new Lua()) {
                TestClass2.A = 105;
                TestClass2.B = "abcdefgh";
                TestClass2.C = '3';
                TestClass2.D = null;
                TestClass2.E = 16.7;
                TestClass2.F = 11111111;
                TestClass2.G = .51m;

                lua.ImportType(typeof(TestClass2));

                lua.DoString("a, b, c, d, e = TestClass2.A, TestClass2.B, TestClass2.C, TestClass2.D, TestClass2.E");
                lua.DoString("f, g = TestClass2.F, TestClass2.G");

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
        public void GetStaticProperty_NoGetter() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("a = TestClass2.NoGetter"));
            }
        }

        [Fact]
        public void GetStaticProperty_ThrowsException() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("a = TestClass2.ThrowsException"));
            }
        }

        [Fact]
        public void GetStaticProperty_InvalidProperty() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("a = TestClass2.X"));
            }
        }

        [Fact]
        public void SetStaticProperties() {
            using (var lua = new Lua()) {
                var obj2 = new object();
                lua.ImportType(typeof(TestClass2));
                lua["obj2"] = obj2;

                lua.DoString("TestClass2.A, TestClass2.B, TestClass2.C, TestClass2.D = -51, \"test\", \"=\", obj2");
                lua.DoString("TestClass2.E, TestClass2.F, TestClass2.G = -777.1, -1, -1.5");

                Assert.Equal(-51, TestClass2.A);
                Assert.Equal("test", TestClass2.B);
                Assert.Equal('=', TestClass2.C);
                Assert.Same(obj2, TestClass2.D);
                Assert.Equal(-777.1, TestClass2.E);
                Assert.Equal(ulong.MaxValue, TestClass2.F);
                Assert.Equal((decimal)(-1.5), TestClass2.G);
            }
        }

        [Fact]
        public void SetStaticProperty_NoSetter() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.NoSetter = 15"));
            }
        }

        [Fact]
        public void SetStaticProperty_Overflow() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.A = 3000000000"));
            }
        }

        [Fact]
        public void SetStaticProperty_InvalidValue() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.A = \"b\""));
            }
        }

        [Fact]
        public void SetStaticProperty_ThrowsException() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.ThrowsException = 10"));
            }
        }

        [Fact]
        public void SetStaticProperty_InvalidProperty() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.X = \"b\""));
            }
        }
    }
}
