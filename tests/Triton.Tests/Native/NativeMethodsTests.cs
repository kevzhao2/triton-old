// Copyright (c) 2020 Kevin Zhao
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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Xunit;
using static Triton.Native.NativeMethods;

using size_t = System.UIntPtr;

namespace Triton.Native
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public unsafe class NativeMethodsTests
    {
        [Fact]
        public void AbsIndex()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushinteger(state, 1);
                lua_pushinteger(state, 2);

                Assert.Equal(2, lua_absindex(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Arith()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushinteger(state, 1);
                lua_pushinteger(state, 2);
                lua_arith(state, LuaArithmeticOp.Add);

                Assert.Equal(3, lua_tointeger(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void CheckStack()
        {
            var state = luaL_newstate();

            try
            {
                Assert.True(lua_checkstack(state, 20));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Compare()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushinteger(state, 1);
                lua_pushinteger(state, 2);

                Assert.True(lua_compare(state, 1, 2, LuaComparisonOp.Lt));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void Concat()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushinteger(state, 1);
                lua_pushinteger(state, 2);
                lua_pushinteger(state, 3);

                lua_concat(state, 3);

                size_t len;
                var str = lua_tolstring(state, -1, &len);
                Assert.Equal("123", Encoding.UTF8.GetString(str, (int)len));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void GetTop()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushinteger(state, 1);
                lua_pushinteger(state, 2);
                lua_pushinteger(state, 3);

                Assert.Equal(3, lua_gettop(state));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void PushBoolean_ToBoolean()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushboolean(state, true);

                Assert.True(lua_toboolean(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }

        [Fact]
        public void PushGlobalTable()
        {
            var state = luaL_newstate();

            try
            {
                lua_pushboolean(state, true);

                Assert.True(lua_toboolean(state, -1));
            }
            finally
            {
                lua_close(state);
            }
        }
    }
}
