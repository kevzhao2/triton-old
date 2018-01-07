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

namespace Triton.Tests {
    public class LuaTests {
        public static object[][] SyntaxErrors = {
            new object[] { "var" },
            new object[] { "var = 3 + " }
        };
        
        [Fact]
        public void IsDisposed_Disposed_True() {
            var lua = new Lua();
            lua.Dispose();

            Assert.True(lua.IsDisposed);
        }

        [Fact]
        public void IsDisposed_NotDisposed_False() {
            using (var lua = new Lua()) {
                Assert.False(lua.IsDisposed);
            }
        }

        [Fact]
        public void GetGlobal_NullName_ThrowsArgumentNullException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ArgumentNullException>(() => lua[null]);
        }

        [Fact]
        public void GetGlobal_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua["x"]);
        }

        [Fact]
        public void SetGlobal_NullName_ThrowsArgumentNullException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ArgumentNullException>(() => lua[null] = 120);
        }

        [Fact]
        public void SetGlobal_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua["x"] = 10);
        }

        [Fact]
        public void CreateTable_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua.CreateTable());
        }

        [Fact]
        public void CreateThread_NullFunction_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                Assert.Throws<ArgumentNullException>(() => lua.CreateThread(null));
            }
        }

        [Fact]
        public void CreateThread_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            var function = lua.LoadString("return 0");
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua.CreateThread(function));
        }

        [Fact]
        public void CreateThread_FunctionDisposed_ThrowsObjectDisposedException() {
            using (var lua = new Lua()) {
                var function = lua.LoadString("return 0");
                function.Dispose();

                Assert.Throws<ObjectDisposedException>(() => lua.CreateThread(function));
            }
        }

        [Fact]
        public void Dispose_CalledTwice() {
            var lua = new Lua();
            lua.Dispose();
            lua.Dispose();
        }

        [Fact]
        public void DoString_NullS_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                Assert.Throws<ArgumentNullException>(() => lua.DoString(null));
            }
        }

        [Fact]
        public void DoString_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua.DoString(""));
        }

        [Theory]
        [MemberData(nameof(SyntaxErrors))]
        public void DoString_SyntaxError_ThrowsLuaException(string s) {
            using (var lua = new Lua()) {
                Assert.Throws<LuaException>(() => lua.DoString(s));
            }
        }

        [Fact]
        public void ImportTypeType_NullType_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                Assert.Throws<ArgumentNullException>(() => lua.ImportType((Type)null));
            }
        }

        [Fact]
        public void ImportTypeType_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua.ImportType(typeof(int)));
        }

        [Fact]
        public void ImportTypeString_NullTypeName_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                Assert.Throws<ArgumentNullException>(() => lua.ImportType((string)null));
            }
        }

        [Fact]
        public void ImportTypeString_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua.ImportType("test"));
        }

        [Fact]
        public void ImportTypeString_BadTypeName_ThrowsArgumentException() {
            using (var lua = new Lua()) {
                Assert.Throws<ArgumentException>(() => lua.ImportType("doesntexist"));
            }
        }

        [Fact]
        public void LoadString_NullS_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                Assert.Throws<ArgumentNullException>(() => lua.LoadString(null));
            }
        }

        [Fact]
        public void LoadString_Disposed_ThrowsObjectDisposedException() {
            var lua = new Lua();
            lua.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lua.LoadString(""));
        }

        [Theory]
        [MemberData(nameof(SyntaxErrors))]
        public void LoadString_SyntaxError_ThrowsLuaException(string s) {
            using (var lua = new Lua()) {
                Assert.Throws<LuaException>(() => lua.LoadString(s));
            }
        }
    }
}
