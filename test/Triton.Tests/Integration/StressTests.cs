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

namespace Triton.Tests.Integration {
    public class StressTests {
        private class TestClass {
            public int field = 0;
            public int Property { get; set; }
            public int Method() => 0;
            public T GenericMethod<T>(T t) => t;
        }

        [Theory]
        [InlineData(1000000)]
        public void GetField(int n) {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                var function = lua.LoadString("field = obj.field");
                for (var i = 0; i < n; i++) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void GetProperty(int n) {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                var function = lua.LoadString("prop = obj.Property");
                for (var i = 0; i < n; i++) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void CallInstanceMethod(int n) {
            using (var lua = new Lua()) {
                lua["obj"] = new TestClass();

                var function = lua.LoadString("return obj:Method()");
                for (var i = 0; i < n; i++) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void GetGlobals(int n) {
            using (var lua = new Lua()) {
                lua["int"] = 1;
                lua["double"] = 12.0;
                lua["str"] = "str";
                lua["bool"] = true;

                for (var i = 0; i < n; i++) {
                    var j = lua["int"];
                    var d = lua["double"];
                    var s = lua["str"];
                    var b = lua["bool"];
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void SetGlobals(int n) {
            using (var lua = new Lua()) {
                for (var i = 0; i < n; i++) {
                    lua["int"] = 1;
                    lua["double"] = 12.0;
                    lua["str"] = "str";
                    lua["bool"] = true;
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void GarbageGeneration(int n) {
            using (var lua = new Lua()) {
                lua["function"] = lua.LoadString("");
                lua["table"] = lua.CreateTable();
                
                for (var i = 0; i < n; i++) {
                    var a = lua["function"];
                    var b = lua["table"];
                }

                GC.Collect();
                GC.WaitForFullGCComplete();
                GC.WaitForPendingFinalizers();
                lua.DoString("");
            }
        }
    }
}
