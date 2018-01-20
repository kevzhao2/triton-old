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
    public class LuaFunctionTests {
        public static object[][] RuntimeErrors = {
            new object[] { "div = 0//0" },
            new object[] { "error()" }
        };

        [Fact]
        public void CallDynamic() {
			using (var lua = new Lua()) {
				dynamic function = lua.LoadString("return 6");

				dynamic results = function();

				Assert.Single(results);
				Assert.Equal(6L, results[0]);
			}
        }

        [Fact]
        public void Call_NoArgs() {
            using (var lua = new Lua()) {
				var function = lua.LoadString("return 1979");

				var results = function.Call();

                Assert.Single(results);
                Assert.Equal(1979L, results[0]);
            }
        }

        [Fact]
        public void Call_OneArg() {
			using (var lua = new Lua()) {
				var results = lua.DoString("return function(x) return x end");
				var function = results[0] as LuaFunction;

				results = function.Call("test");

				Assert.Single(results);
				Assert.Equal("test", results[0]);
			}
        }

		[Fact]
		public void Call_ManyArgs() {
			using (var lua = new Lua()) {
				var results = lua.DoString("return function(...)\n" +
										   "  result = 0\n" +
										   "  for _, val in ipairs({...}) do\n" +
										   "    result = result + val\n" +
										   "  end\n" +
										   "  return result\n" +
										   "end");
				var function = results[0] as LuaFunction;

				results = function.Call(6, 51, 29, -51, -29, 12);

				Assert.Single(results);
				Assert.Equal(18L, results[0]);
			}
		}

        [Fact]
        public void Call_NoResults() {
            using (var lua = new Lua()) {
				var function = lua.LoadString("return");

				var results = function.Call();

                Assert.Empty(results);
            }
        }

        [Fact]
        public void Call_ManyResults() {
            using (var lua = new Lua()) {
				var function = lua.LoadString("return 0, 1, 4, 9, 16");

				var results = function.Call("test");

                Assert.Equal(5, results.Length);
                Assert.Equal(0L, results[0]);
                Assert.Equal(1L, results[1]);
                Assert.Equal(4L, results[2]);
                Assert.Equal(9L, results[3]);
                Assert.Equal(16L, results[4]);
            }
        }

        [Fact]
        public void Call_NullArgs_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
				var function = lua.LoadString("");

				Assert.Throws<ArgumentNullException>(() => function.Call(null));
            }
        }

        [Fact]
        public void Call_TooManyArgs_ThrowsLuaException() {
            using (var lua = new Lua()) {
				var function = lua.LoadString("");

				Assert.Throws<LuaException>(() => function.Call(new object[10000000]));
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeErrors))]
        public void Call_RuntimeError_ThrowsLuaException(string s) {
            using (var lua = new Lua()) {
				var function = lua.LoadString(s);

				Assert.Throws<LuaException>(() => function.Call());
            }
        }

        [Fact]
        public void Call_ArgWrongLuaEnvironment_ThrowsArgumentException() {
            using (var lua = new Lua())
            using (var lua2 = new Lua()) {
                var function = lua.LoadString("");
                var table = lua2.CreateTable();

                Assert.Throws<ArgumentException>(() => function.Call(table));
            }
        }
    }
}
