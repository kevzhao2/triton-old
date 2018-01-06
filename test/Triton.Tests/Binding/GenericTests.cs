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

using System.Collections.Generic;
using Xunit;

namespace Triton.Tests.Binding {
    public class GenericTests {
        public class Constraint<T> where T : struct {
            public static int X { get; set; } = 0;
        }

        [Fact]
        public void ConstructGeneric() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(List<>));

                lua.DoString("listint = List(Int32)()");

                Assert.IsType<List<int>>(lua["listint"]);
            }
        }

        [Fact]
        public void ConstructNestedGeneric() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(List<>));

                lua.DoString("listlistint = List(List(Int32))()");

                Assert.IsType<List<List<int>>>(lua["listlistint"]);
            }
        }

        [Fact]
        public void ConstructGeneric_TooFewTypeArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(List<>));

                Assert.Throws<LuaException>(() => lua.DoString("List()()"));
            }
        }

        [Fact]
        public void ConstructGeneric_TooManyTypeArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(int));
                lua.ImportType(typeof(List<>));

                Assert.Throws<LuaException>(() => lua.DoString("List(Int32, Int32)()"));
            }
        }

        [Fact]
        public void ConstructGeneric_BadConstraint() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(object));
                lua.ImportType(typeof(Constraint<>));

                Assert.Throws<LuaException>(() => lua.DoString("Constraint(Object)()"));
            }
        }

        [Fact]
        public void ConstructGeneric_GenericTypeArg() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(List<>));

                Assert.Throws<LuaException>(() => lua.DoString("List(List)()"));
            }
        }

        [Fact]
        public void ConstructGeneric_InvalidTypeArgs() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(List<>));

                Assert.Throws<LuaException>(() => lua.DoString("List(5)()"));
            }
        }

        [Fact]
        public void GetGeneric_Property_Fails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(Constraint<>));

                Assert.Throws<LuaException>(() => lua.DoString("x = Constraint.X"));
            }
        }

        [Fact]
        public void SetGeneric_Property_Fails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(Constraint<>));

                Assert.Throws<LuaException>(() => lua.DoString("Constraint.X = 10"));
            }
        }
    }
}
