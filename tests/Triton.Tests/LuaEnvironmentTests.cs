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
    public class LuaEnvironmentTests
    {
        [Fact]
        public void Item_Get_NullGlobal_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment[null!]);
        }

        [Fact]
        public void Item_Get_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment["test"]);
        }

        [Fact]
        public void Item_Set_NullGlobal_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment[null!] = 1234);
        }

        [Fact]
        public void Item_Set_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment["test"] = 1234);
        }

        [Fact]
        public void Item_Set_WrongEnvironment_ThrowsInvalidOperationException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            using var environment2 = new LuaEnvironment();

            Assert.Throws<InvalidOperationException>(() => environment2["test"] = table);
        }

        [Fact]
        public void Item_Set_Get_Nil()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = LuaVariant.Nil;

            Assert.True(environment["test"].IsNil);
        }

        [Fact]
        public void Item_Set_Get_Boolean()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = true;

            Assert.True((bool)environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Integer()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = 1234;

            Assert.Equal(1234, (long)environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_Number()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = 1.234;

            Assert.Equal(1.234, (double)environment["test"]);
        }

        [Fact]
        public void Item_Set_Get_String()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = "This is a test string!";

            Assert.Equal("This is a test string!", (string?)environment["test"]);
        }
        
        [Fact]
        public void CreateTable_EnvironmentDisposed_ThrowsObjectDisposedException()
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
        public void CreateFunction_NullChunk_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.CreateFunction(null!));
        }

        [Fact]
        public void CreateFunction_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.CreateFunction("return 0"));
        }

        [Fact]
        public void CreateFunction_InvalidLua_ThrowsLuaLoadException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaLoadException>(() => environment.CreateFunction("retur 0"));
        }

        [Fact]
        public void CreateThread_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.CreateThread());
        }

        [Fact]
        public void Eval_NullChunk_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.Eval(null!));
        }

        [Fact]
        public void Eval_EnvironmentDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment();
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.Eval("return"));
        }

        [Fact]
        public void Eval_InvalidLua_ThrowsLuaLoadException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaLoadException>(() => environment.Eval("retur 0"));
        }

        [Fact]
        public void Eval_LuaError_ThrowsLuaEvalException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaEvalException>(() => environment.Eval("error('test')"));
        }

        [Fact]
        public void Eval_OneResult()
        {
            using var environment = new LuaEnvironment();

            var (result, _) = environment.Eval("return 0");

            Assert.Equal(0, (long)result);
        }

        [Fact]
        public void Eval_TwoResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, _) = environment.Eval("return 0, 'test'");

            Assert.Equal(0, (long)result);
            Assert.Equal("test", (string?)result2);
        }

        [Fact]
        public void Eval_ThreeResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, _) = environment.Eval("return 0, 'test', 1.234");

            Assert.Equal(0, (long)result);
            Assert.Equal("test", (string?)result2);
            Assert.Equal(1.234, (double)result3);
        }

        [Fact]
        public void Eval_ExtraResults_AreNil()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, _) = environment.Eval("return 0");

            Assert.Equal(0, (long)result);
            Assert.True(result2.IsNil);
            Assert.True(result3.IsNil);
        }
    }
}
