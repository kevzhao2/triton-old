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
        public void Set_Get_Dynamic()
        {
            using var environment = new LuaEnvironment();
            dynamic table = environment.CreateTable();

            table.test = 123;

            Assert.Equal(123L, table.test);
        }

        [Fact]
        public void Item_String_Get_NullField_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[null!]);
        }

        [Fact]
        public void Item_String_Get_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            environment.Dispose();

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
        public void Item_String_Set_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table["test"] = 1234);
        }

        [Fact]
        public void Item_String_Set_Get_Nil()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = null;

            Assert.Null(table["test"]);
        }

        [Fact]
        public void Item_String_Set_Get_Boolean()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = true;

            Assert.Equal(true, table["test"]);
        }

        [Fact]
        public void Item_String_Set_Get_Integer()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = 1234;

            Assert.Equal(1234L, table["test"]);
        }

        [Fact]
        public void Item_String_Set_Get_Number()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = 1.234;

            Assert.Equal(1.234, table["test"]);
        }

        [Fact]
        public void Item_String_Set_Get_String()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = "This is a test string!";

            Assert.Equal("This is a test string!", table["test"]);
        }

        [Fact]
        public void Item_String_Set_Get_Table()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = table;

            Assert.Same(table, table["test"]);
        }

        [Fact]
        public void Item_String_Set_Get_Function()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var function = environment.CreateFunction("return 0");

            table["test"] = function;

            Assert.Same(function, table["test"]);
        }

        [Fact]
        public void Item_Long_Get_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[1]);
        }

        [Fact]
        public void Item_Long_Set_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[1] = 1234);
        }

        [Fact]
        public void Item_Long_Set_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table[1] = 1234;

            Assert.Equal(1234L, table[1]);
        }

        [Fact]
        public void Item_Object_Get_NullField_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[(object)null!]);
        }

        [Fact]
        public void Item_Object_Get_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[true]);
        }

        [Fact]
        public void Item_Object_Set_NullField_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[(object)null!] = 1234);
        }

        [Fact]
        public void Item_Object_Set_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => table[true] = 1234);
        }

        [Fact]
        public void Item_Object_Set_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table[true] = 1234;

            Assert.Equal(1234L, table[true]);
        }
    }
}
