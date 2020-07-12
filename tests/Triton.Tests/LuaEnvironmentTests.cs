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
using System.Text;
using Xunit;

namespace Triton
{
    public unsafe class LuaEnvironmentTests
    {
        [Fact]
        public void Ctor()
        {
            using var environment = new LuaEnvironment();

            Assert.Same(Encoding.ASCII, environment.Encoding);
        }

        [Fact]
        public void Ctor_NullEncoding_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LuaEnvironment(null!));
        }

        [Fact]
        public void Encoding_Get()
        {
            using var environment = new LuaEnvironment(Encoding.UTF8);

            Assert.Same(Encoding.UTF8, environment.Encoding);
        }

        [Fact]
        public void CreateTable_ObjectDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.CreateTable());
        }

        [Fact]
        public void CreateTable_NegativeSequentialCapacity_ThrowsArgumentOutOfRangeException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentOutOfRangeException>(() => environment.CreateTable(sequentialCapacity: -1));
        }

        [Fact]
        public void CreateTable_NegativeNonSequentialCapacity_ThrowsArgumentOutOfRangeException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentOutOfRangeException>(() => environment.CreateTable(nonSequentialCapacity: -1));
        }

        [Fact]
        public void CreateFunction_NullS_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.CreateFunction(null!));
        }

        [Fact]
        public void CreateFunction_ObjectDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.CreateFunction("return 0"));
        }

        [Fact]
        public void CreateFunction_InvalidLuaCode_ThrowsLuaLoadException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaLoadException>(() => environment.CreateFunction("retur 0"));
        }

        [Fact]
        public void CreateFunction()
        {
            using var environment = new LuaEnvironment();

            var function = environment.CreateFunction("return 0");
        }

        [Fact]
        public void Item_Get_NullS_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment[null!]);
        }

        [Fact]
        public void Item_Get_ObjectDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment["test"]);
        }

        [Fact]
        public void Item_Set_NullS_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment[null!] = 1234);
        }

        [Fact]
        public void Item_Set_ObjectDisposed_ThrowsArgumentNullException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Nil()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = null;

            Assert.Null(environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Boolean()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = true;

            Assert.Equal(true, environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Integer()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = 1234;

            Assert.Equal(1234L, environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Number()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = 1.234;

            Assert.Equal(1.234, environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_String()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = "This is a test string!";

            Assert.Equal("This is a test string!", environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Table()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            environment["test"] = table;

            Assert.Same(table, environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Function()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 0");

            environment["test"] = function;

            Assert.Same(function, environment["test"]);
        }
    }
}
