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
    public class EventTests {
        private class TestClass {
            public event EventHandler Event;

            public event EventHandler EventThrows {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            public event EventHandler EventThrows2 {
                add {
                }
                remove => throw new NotImplementedException();
            }

#pragma warning disable 0067
            public event Action EventWrongType;
#pragma warning restore 0067

            public void InvokeEvent() => Event?.Invoke(this, EventArgs.Empty);
        }

        private static class TestClass2 {
            public static event EventHandler Event;

            public static event EventHandler EventThrows {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            public static event EventHandler EventThrows2 {
                add {
                }
                remove => throw new NotImplementedException();
            }

#pragma warning disable 0067
            public static event Action EventWrongType;
#pragma warning restore 0067

            public static void InvokeEvent() => Event?.Invoke(null, EventArgs.Empty);
        }

        [Fact]
        public void GetInstanceEvent_AddRemove() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("func = function(obj, args) x = 6 end");
                lua.DoString("obj.Event:Add(func)");

                obj.InvokeEvent();

                Assert.Equal(6L, lua["x"]);

                lua["x"] = 0;

                lua.DoString("obj.Event:Remove(func)");

                obj.InvokeEvent();

                Assert.NotEqual(6L, lua["x"]);
            }
        }

        [Fact]
        public void GetInstanceEvent_Add_BadType() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                Assert.Throws<LuaException>(() => lua.DoString("obj.EventWrongType:Add(function(obj, args) end)"));
            }
        }

        [Fact]
        public void GetInstanceEvent_Add_NilFails() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                Assert.Throws<LuaException>(() => lua.DoString("obj.Event:Add(nil)"));
            }
        }

        [Fact]
        public void GetInstanceEvent_Add_Throws() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                Assert.Throws<LuaException>(() => lua.DoString("obj.EventThrows:Add(function(obj, args) end)"));
            }
        }

        [Fact]
        public void GetInstanceEvent_Remove_NilFails() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                Assert.Throws<LuaException>(() => lua.DoString("obj.Event:Remove(nil)"));
            }
        }

        [Fact]
        public void GetInstanceEvent_Remove_Throws() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                lua.DoString("func = function(obj, args) end");
                lua.DoString("obj.EventThrows2:Add(func)");

                Assert.Throws<LuaException>(() => lua.DoString("obj.EventThrows2:Remove(func)"));
            }
        }

        [Fact]
        public void SetInstanceEvent_Fails() {
            using (var lua = new Lua()) {
                var obj = new TestClass();
                lua["obj"] = obj;

                Assert.Throws<LuaException>(() => lua.DoString("obj.Event = nil"));
            }
        }

        [Fact]
        public void GetStaticEvent_AddRemove() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("func = function(obj, args) x = 6 end");
                lua.DoString("TestClass2.Event:Add(func)");

                TestClass2.InvokeEvent();

                Assert.Equal(6L, lua["x"]);

                lua["x"] = 0;

                lua.DoString("TestClass2.Event:Remove(func)");

                TestClass2.InvokeEvent();

                Assert.NotEqual(6L, lua["x"]);
            }
        }

        [Fact]
        public void GetStaticEvent_Add_BadType() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.EventWrongType:Add(function(obj, args) end)"));
            }
        }

        [Fact]
        public void GetStaticEvent_Add_NilFails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.Event:Add(nil)"));
            }
        }

        [Fact]
        public void GetStaticEvent_Add_Throws() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.EventThrows:Add(function(obj, args) end)"));
            }
        }

        [Fact]
        public void GetStaticEvent_Remove_NilFails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.Event:Remove(nil)"));
            }
        }

        [Fact]
        public void GetStaticEvent_Remove_Throws() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                lua.DoString("func = function(obj, args) end");
                lua.DoString("TestClass2.EventThrows2:Add(func)");

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.EventThrows2:Remove(func)"));
            }
        }

        [Fact]
        public void SetStaticEvent_Fails() {
            using (var lua = new Lua()) {
                lua.ImportType(typeof(TestClass2));

                Assert.Throws<LuaException>(() => lua.DoString("TestClass2.Event = nil"));
            }
        }
    }
}
