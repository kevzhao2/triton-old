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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Triton.Tests.Binding {
    public class MethodTests {
        private class TestClass {
            public int X { get; set; }

            public void Method1() => X = 100;
            public void Method2(int x) => X = x;
            public int Method3() => X;

            public int Method4(int x, out int y) {
                y = 3 * x;
                return 2 * x;
            }

            public int Method5(BindingFlags flags) => (int)flags;

            public int DefaultParams(int x, int y = -123) => x + y;

            public void Overloaded(int i) => X = i;
            public void Overloaded(string s) => X = int.Parse(s);
            public void Overloaded(int i, int y = 0) => X = i + y + 1;
            
            public void Swap(ref int x, ref int y) {
                int temp = x;
                x = y;
                y = temp;
            }

            public int Params(params int[] s) => s.Sum();
            public void ThrowsException() => throw new NotImplementedException();

            public string Generic<T>() => typeof(T).ToString();
            public string GenericConstraint<T>() where T : struct => typeof(T).ToString();
            public void GenericThrows<T>() => throw new NotImplementedException();
            public void GenericOut<T>(out T t) => t = default;
        }

        private static class TestClass2 {
            public static int X { get; set; }

            public static void Method1() => X = 100;
            public static void Method2(int x) => X = x;
            public static int Method3() => X;

            public static int Method4(int x, out int y) {
                y = 3 * x;
                return 2 * x;
            }

            public static int DefaultParams(int x, int y = -123) => x + y;

            public static void Overloaded(int i) => X = i;
            public static void Overloaded(string s) => X = int.Parse(s);
            public static void Overloaded(int i, int y = 0) => X = i + y + 1;

            public static void Swap(ref int x, ref int y) {
                int temp = x;
                x = y;
                y = temp;
            }

            public static int Params(params int[] s) => s.Sum();
            public static void ThrowsException() => throw new NotImplementedException();

            public static string Generic<T>() => typeof(T).ToString();
            public static string GenericConstraint<T>() where T : struct => typeof(T).ToString();
            public static void GenericThrows<T>() => throw new NotImplementedException();
            public static void GenericOut<T>(out T t) => t = default;
        }

        [Fact]
        public void CallInstanceMethod_NoArgs() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("obj:Method1()");

                Assert.Equal(100, obj.X);
            }
        }

        [Fact]
        public void CallInstanceMethod_OneArg() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("obj:Method2(6)");

                Assert.Equal(6, obj.X);
            }
        }

        [Fact]
        public void CallInstanceMethod_OneResult() {
            using (var lua = new Lua()) {
                var obj = new TestClass { X = 12 };
                lua["obj"] = obj;

                lua.DoString("x = obj:Method3()");

                Assert.Equal(12L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_TwoResults() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x, y = obj:Method4(100)");

                Assert.Equal(200L, lua["x"]);
                Assert.Equal(300L, lua["y"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_Enums() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(BindingFlags));
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:Method5(BindingFlags.Instance | BindingFlags.NonPublic)");

                Assert.Equal(36L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_DefaultParamsNoneSupplied() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:DefaultParams(100)");

                Assert.Equal(-23L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_DefaultParamsSupplied() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:DefaultParams(100, -100)");

                Assert.Equal(0L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_OverloadedInt() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("obj:Overloaded(145)");

                Assert.Equal(145, obj.X);
            }
        }

        [Fact]
        public void CallInstanceMethod_OverloadedString() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("obj:Overloaded(\"214\")");

                Assert.Equal(214, obj.X);
            }
        }

        [Fact]
        public void CallInstanceMethod_Swap() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x, y = obj:Swap(100, 150)");

                Assert.Equal(150L, lua["x"]);
                Assert.Equal(100L, lua["y"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_ParamsNoArgs() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:Params()");

                Assert.Equal(0L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_ParamsOneArg() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:Params(16)");

                Assert.Equal(16L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_ParamsManyArgs() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:Params(16, 129, 156, 123)");

                Assert.Equal(424L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_ParamsArrayArg() {
            using (var lua = new Lua()) {
                lua["arr"] = new[] { 16, 129, 156, 123 };
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:Params(arr)");

                Assert.Equal(424L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_ParamsBadValue() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:Params(16, 129, 156, 123, \"y\")"));
            }
        }

        [Fact]
        public void CallInstanceMethod_ThrowsException() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj:ThrowsException()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_Generic() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["obj"] = new TestClass();

                lua.DoString("s = obj:Generic(Int32)()");

                Assert.Equal("System.Int32", lua["s"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_Generic_TooFewTypes() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:Generic()()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_Generic_TooManyTypes() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:Generic(Int32, Int32)()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_Generic_BadTypeArgs() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:Generic(5)()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_Generic_GenericType() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["List"] = typeof(List<>);
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:Generic(List)()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_Generic_BadArgs() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:Generic(Int32)(51)"));
            }
        }

        [Fact]
        public void CallInstanceMethod_GenericConstraint_BadConstraint() {
            using (var lua = new Lua()) {
                lua["Object"] = typeof(object);
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:GenericConstraint(Object)()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_GenericThrows() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("x = obj:GenericThrows(Int32)()"));
            }
        }

        [Fact]
        public void CallInstanceMethod_GenericOut() {
            using (var lua = new Lua()) {
                lua["Int32"] = typeof(int);
                lua["obj"] = new TestClass();

                lua.DoString("x = obj:GenericOut(Int32)()");

                Assert.Equal(0L, lua["x"]);
            }
        }

        [Fact]
        public void CallInstanceMethod_Invalid() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj:Invalid()"));
            }
        }

        [Fact]
        public void SetInstanceMethod_Fails() {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                Assert.Throws<LuaException>(() => lua.DoString("obj.Method1 = nil"));
            }
        }

        [Fact]
        public void CallStaticMethod_NoArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("TestClass2.Method1()");

                Assert.Equal(100, TestClass2.X);
            }
        }

        [Fact]
        public void CallStaticMethod_OneArg() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("TestClass2.Method2(6)");

                Assert.Equal(6, TestClass2.X);
            }
        }

        [Fact]
        public void CallStaticMethod_OneResult() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));
                TestClass2.X = 12;

                lua.DoString("x = TestClass2.Method3()");

                Assert.Equal(12L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_TwoResults() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x, y = TestClass2.Method4(100)");

                Assert.Equal(200L, lua["x"]);
                Assert.Equal(300L, lua["y"]);
            }
        }

        [Fact]
        public void CallStaticMethod_DefaultParamsNoneSupplied() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x = TestClass2.DefaultParams(100)");

                Assert.Equal(-23L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_DefaultParamsSupplied() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x = TestClass2.DefaultParams(100, -100)");

                Assert.Equal(0L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_OverloadedInt() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("TestClass2.Overloaded(145)");

                Assert.Equal(145, TestClass2.X);
            }
        }

        [Fact]
        public void CallStaticMethod_OverloadedString() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("TestClass2.Overloaded(\"214\")");

                Assert.Equal(214, TestClass2.X);
            }
        }

        [Fact]
        public void CallStaticMethod_Swap() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x, y = TestClass2.Swap(100, 150)");

                Assert.Equal(150L, lua["x"]);
                Assert.Equal(100L, lua["y"]);
            }
        }

        [Fact]
        public void CallStaticMethod_ParamsNoArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x = TestClass2.Params()");

                Assert.Equal(0L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_ParamsOneArg() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x = TestClass2.Params(16)");

                Assert.Equal(16L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_ParamsManyArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x = TestClass2.Params(16, 129, 156, 123)");

                Assert.Equal(424L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_ParamsBadValue() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.Params(16, 129, 156, 123, \"y\")"));
            }
        }

        [Fact]
        public void CallStaticMethod_ThrowsException() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.ThrowsException()"));
            }
        }

        [Fact]
        public void CallStaticMethod_Generic() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(TestClass2));

                lua.DoString("s = TestClass2.Generic(Int32)()");

                Assert.Equal("System.Int32", lua["s"]);
            }
        }

        [Fact]
        public void CallStaticMethod_Generic_TooFewTypes() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.Generic()()"));
            }
        }

        [Fact]
        public void CallStaticMethod_Generic_TooManyTypes() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.Generic(Int32, Int32)()"));
            }
        }

        [Fact]
        public void CallStaticMethod_Generic_BadTypeArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.Generic(5)()"));
            }
        }

        [Fact]
        public void CallStaticMethod_Generic_GenericType() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(List<>));
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.Generic(List)()"));
            }
        }

        [Fact]
        public void CallStaticMethod_Generic_BadArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.Generic(Int32)(51)"));
            }
        }

        [Fact]
        public void CallStaticMethod_GenericConstraint_BadConstraint() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(object));
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.GenericConstraint(Object)()"));
            }
        }

        [Fact]
        public void CallStaticMethod_GenericThrows() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("x = TestClass2.GenericThrows(Int32)()"));
            }
        }

        [Fact]
        public void CallStaticMethod_GenericOut() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(TestClass2));

                lua.DoString("x = TestClass2.GenericOut(Int32)()");

                Assert.Equal(0L, lua["x"]);
            }
        }

        [Fact]
        public void CallStaticMethod_Invalid() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.Invalid()"));
            }
        }

        [Fact]
        public void SetStaticMethod_Fails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.Method1 = nil"));
            }
        }
    }
}
