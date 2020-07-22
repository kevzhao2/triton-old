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

using System;
using Xunit;

namespace Triton
{
    public class LuaTableTests
    {
        [Fact]
        public void Item_String_Get_NullField_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[null!]);
        }

        [Fact]
        public void Item_String_Get_TableDisposed_ThrowsObjectDisposedException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table["test"]);
        }

        [Fact]
        public void Item_String_Set_NullField_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[null!] = 1234);
        }

        [Fact]
        public void Item_String_Set_TableDisposed_ThrowsObjectDisposedException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table["test"] = 1234);
        }

        [Fact]
        public void Item_String_Set_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = 1234;

            Assert.Equal(1234, (long)table["test"]);
        }

        [Fact]
        public void Item_Long_Get_TableDisposed_ThrowsObjectDisposedException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[1]);
        }

        [Fact]
        public void Item_Long_Set_TableDisposed_ThrowsObjectDisposedException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[1] = 1234);
        }

        [Fact]
        public void Item_Long_Set_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table[1] = 1234;

            Assert.Equal(1234, (long)table[1]);
        }

        [Fact]
        public void Item_LuaValue_Get_TableDisposed_ThrowsObjectDisposedException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[true]);
        }

        [Fact]
        public void Item_LuaValue_Set_TableDisposed_ThrowsObjectDisposedException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[true] = 1234);
        }

        [Fact]
        public void Item_LuaValue_Set_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table[true] = 1234;

            Assert.Equal(1234, (long)table[true]);
        }
    }
}
