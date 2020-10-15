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
        public void Globals_Get()
        {
            using var environment = new LuaEnvironment();
            environment["test"] = 1234;

            var globals = environment.Globals;

            Assert.Equal(1234, globals["test"]);
        }

        [Fact]
        public void Item_Get_NullName_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment[null!]);
        }

        [Fact]
        public void Item_Set_NullName_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment[null!] = 1234);
        }

        [Fact]
        public void Item_Set_Get()
        {
            using var environment = new LuaEnvironment();

            environment["test"] = 1234;

            Assert.Equal(1234, environment["test"]);
        }

        [Fact]
        public void CreateTable_NegativeArrayCapacity_ThrowsArgumentOutOfRangeException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentOutOfRangeException>(() => environment.CreateTable(-1, 0));
        }

        [Fact]
        public void CreateTable_NegativeRecordCapacity_ThrowsArgumentOutOfRangeException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentOutOfRangeException>(() => environment.CreateTable(0, -1));
        }

        [Fact]
        public void CreateTable()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = 1234;

            Assert.Equal(1234, table["test"]);
        }

        [Fact]
        public void CreateFunction_NullChunk_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.CreateFunction(null!));
        }

        [Fact]
        public void CreateFunction()
        {
            using var environment = new LuaEnvironment();
            var function = environment.CreateFunction("return 0");

            var (result, _) = function.Call();

            Assert.Equal(0, result);
        }

        [Fact]
        public void CreateThread()
        {
            using var environment = new LuaEnvironment();
            var thread = environment.CreateThread();
            thread.SetFunction(environment.CreateFunction("return 0"));

            var (result, _) = thread.Resume();

            Assert.Equal(0, result);
        }

        [Fact]
        public void Eval_NullChunk_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.Eval(null!));
        }

        [Fact]
        public void Eval_LuaLoadError_ThrowsLuaLoadException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaLoadException>(() => environment.Eval("retur 0"));
        }

        [Fact]
        public void Eval_LuaRuntimeError_ThrowsLuaRuntimeException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaRuntimeException>(() => environment.Eval("error('test')"));
        }

        [Fact]
        public void Eval_OneResult()
        {
            using var environment = new LuaEnvironment();

            var (result, _) = environment.Eval("return 1");

            Assert.Equal(1, result);
        }

        [Fact]
        public void Eval_TwoResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, _) = environment.Eval("return 1, 2");

            Assert.Equal(1, result);
            Assert.Equal(2, result2);
        }

        [Fact]
        public void Eval_ThreeResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, _) = environment.Eval("return 1, 2, 3");

            Assert.Equal(1, result);
            Assert.Equal(2, result2);
            Assert.Equal(3, result3);
        }

        [Fact]
        public void Eval_ManyResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, (result4, result5, result6, _)) =
                environment.Eval("return 1, 2, 3, 4, 5, 6");

            Assert.Equal(1, result);
            Assert.Equal(2, result2);
            Assert.Equal(3, result3);
            Assert.Equal(4, result4);
            Assert.Equal(5, result5);
            Assert.Equal(6, result6);
        }

        [Fact]
        public void ImportTypes_NullAssembly_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.ImportTypes(null!));
        }

        [Fact]
        public void ImportTypes_NonGenericTypes()
        {
            using var environment = new LuaEnvironment();

            environment.ImportTypes(typeof(DateTime).Assembly);

            var (dateTime, _) = environment.Eval("return System.DateTime");

            Assert.Equal(new[] { typeof(DateTime) }, dateTime.ToClrTypes());
        }

        [Fact]
        public void ImportTypes_GenericTypes()
        {
            using var environment = new LuaEnvironment();

            environment.ImportTypes(typeof(Action).Assembly);

            var (action, _) = environment.Eval("return System.Action");

            Assert.Equal(
                new[]
                {
                    typeof(Action),
                    typeof(Action<>),
                    typeof(Action<,>),
                    typeof(Action<,,>),
                    typeof(Action<,,,>),
                    typeof(Action<,,,,>),
                    typeof(Action<,,,,,>),
                    typeof(Action<,,,,,,>),
                    typeof(Action<,,,,,,,>),
                    typeof(Action<,,,,,,,,>),
                    typeof(Action<,,,,,,,,,>),
                    typeof(Action<,,,,,,,,,,>),
                    typeof(Action<,,,,,,,,,,,>),
                    typeof(Action<,,,,,,,,,,,,>),
                    typeof(Action<,,,,,,,,,,,,,>),
                    typeof(Action<,,,,,,,,,,,,,,>),
                    typeof(Action<,,,,,,,,,,,,,,,>)
                },
                action.ToClrTypes());
        }

        [Fact]
        public void Test()
        {
            using var environment = new LuaEnvironment();

            environment.ImportTypes(typeof(Action).Assembly);

            environment.Eval("x = System.Type.Delimiter");

            var x = environment["x"];
        }
    }
}
