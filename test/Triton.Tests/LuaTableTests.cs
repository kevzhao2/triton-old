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
    public class LuaTableTests {
        private enum TestEnum {
            A, B, C, D
        }

		[Fact]
		public void GetSetDynamic() {
			using (var lua = new Lua()) {
				dynamic table = lua.CreateTable();

				table.x = 567;

				Assert.Equal(567L, table.x);
			}
		}

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetSet_Boolean(bool b) {
            using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = b;

                Assert.Equal(b, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Enum() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = TestEnum.C;

                Assert.Equal((long)TestEnum.C, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Function() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();
				var function = lua.LoadString("return 0");

                table["test"] = function;

				Assert.Same(function, table["test"]);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        public void GetSet_Integer(long i) {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = i;

                Assert.Equal(i, table["test"]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(ulong.MaxValue)]
        public void GetSet_UInt64(ulong u) {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = u;

                Assert.Equal((long)u, table["test"]);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void GetSet_IntegerKey(int i) {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table[i] = "test";

                Assert.Equal("test", table[i]);
            }
        }

        [Theory]
        [InlineData(3.14159)]
        [InlineData(Double.NaN)]
        [InlineData(Double.PositiveInfinity)]
        public void GetSet_Number(double d) {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = d;

                Assert.Equal(d, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Object() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();
				var obj = new object();

                table["test"] = obj;

                Assert.Same(obj, table["test"]);
            }
        }

        [Theory]
        [InlineData("str\n")]
        [InlineData("s\x88ff\n")]
        public void GetSet_String(string s) {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = s;

                Assert.Equal(s, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Struct() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();
				var dateTime = DateTime.Now;

                table["test"] = dateTime;

                Assert.Equal(dateTime, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Table() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				table["test"] = table;

				Assert.Same(table, table["test"]);
            }
        }

		[Fact]
		public void GetSet_Thread() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();
				var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread;

				table["test"] = thread;
				Assert.Same(thread, table["test"]);
			}
		}

        [Fact]
        public void Get_NullKey_ThrowsArgumentNullException() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				Assert.Throws<ArgumentNullException>(() => table[null]);
            }
        }

        [Fact]
        public void Set_NullKey_ThrowsArgumentNullException() {
			using (var lua = new Lua()) {
				var table = lua.CreateTable();

				Assert.Throws<ArgumentNullException>(() => table[null] = 0);
            }
        }

        [Fact]
        public void Set_ValueWrongLuaEnvironment_ThrowsArgumentException() {
            using (var lua = new Lua())
            using (var lua2 = new Lua()) {
                var table = lua.CreateTable();
                var table2 = lua2.CreateTable();

                Assert.Throws<ArgumentException>(() => table["x"] = table2);
            }
        }
    }
}
