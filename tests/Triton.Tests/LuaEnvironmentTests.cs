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
        public void GetGlobal_NullName_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.GetGlobal(null!));
        }

        [Fact]
        public void SetGlobal_NullName_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<ArgumentNullException>(() => environment.SetGlobal(null!, 1234));
        }

        [Fact]
        public void SetGlobal_GetGlobal_Boolean()
        {
            using var environment = new LuaEnvironment();

            environment.SetGlobal("boolean", true);

            Assert.True((bool)environment.GetGlobal("boolean"));
        }

        [Fact]
        public void SetGlobal_GetGlobal_Integer()
        {
            using var environment = new LuaEnvironment();

            environment.SetGlobal("integer", 1234);

            Assert.Equal(1234, (long)environment.GetGlobal("integer"));
        }

        [Fact]
        public void SetGlobal_GetGlobal_Number()
        {
            using var environment = new LuaEnvironment();

            environment.SetGlobal("number", 1.234);

            Assert.Equal(1.234, (double)environment.GetGlobal("number"));
        }

        [Fact]
        public void SetGlobal_GetGlobal_String()
        {
            using var environment = new LuaEnvironment();

            environment.SetGlobal("string", "test");

            Assert.Equal("test", (string)environment.GetGlobal("string"));
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

            Assert.Throws<LuaLoadException>(() => environment.Eval("retur"));
        }

        [Fact]
        public void Eval_LuaRuntimeError_ThrowsLuaRuntimeException()
        {
            using var environment = new LuaEnvironment();

            Assert.Throws<LuaRuntimeException>(() => environment.Eval("error('test')"));
        }

        [Fact]
        public void Eval_NoResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return");

            Assert.True(result.IsNil);
            Assert.False(result.IsBoolean);
            Assert.False(result.IsInteger);
            Assert.False(result.IsFloat);
            Assert.False(result.IsString);
            Assert.False(result.IsTable);
            Assert.False(result.IsFunction);
            Assert.False(result.IsThread);
            Assert.False(result.IsClrObject);
            Assert.False(result.IsClrTypes);
        }

        [Fact]
        public void Eval_NilResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return nil");

            Assert.True(result.IsNil);
            Assert.False(result.IsBoolean);
            Assert.False(result.IsInteger);
            Assert.False(result.IsFloat);
            Assert.False(result.IsString);
            Assert.False(result.IsTable);
            Assert.False(result.IsFunction);
            Assert.False(result.IsThread);
            Assert.False(result.IsClrObject);
            Assert.False(result.IsClrTypes);
        }

        [Fact]
        public void Eval_BooleanResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return true");

            Assert.False(result.IsNil);
            Assert.True(result.IsBoolean);
            Assert.False(result.IsInteger);
            Assert.False(result.IsFloat);
            Assert.False(result.IsString);
            Assert.False(result.IsTable);
            Assert.False(result.IsFunction);
            Assert.False(result.IsThread);
            Assert.False(result.IsClrObject);
            Assert.False(result.IsClrTypes);
            Assert.True((bool)result);
        }

        [Fact]
        public void Eval_IntegerResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return 1234");

            Assert.False(result.IsNil);
            Assert.False(result.IsBoolean);
            Assert.True(result.IsInteger);
            Assert.False(result.IsFloat);
            Assert.False(result.IsString);
            Assert.False(result.IsTable);
            Assert.False(result.IsFunction);
            Assert.False(result.IsThread);
            Assert.False(result.IsClrObject);
            Assert.False(result.IsClrTypes);
            Assert.Equal(1234, (long)result);
        }

        [Fact]
        public void Eval_NumberResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return 1.234");

            Assert.False(result.IsNil);
            Assert.False(result.IsBoolean);
            Assert.False(result.IsInteger);
            Assert.True(result.IsFloat);
            Assert.False(result.IsString);
            Assert.False(result.IsTable);
            Assert.False(result.IsFunction);
            Assert.False(result.IsThread);
            Assert.False(result.IsClrObject);
            Assert.False(result.IsClrTypes);
            Assert.Equal(1.234, (double)result);
        }

        [Fact]
        public void Eval_StringResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return 'test'");

            Assert.False(result.IsNil);
            Assert.False(result.IsBoolean);
            Assert.False(result.IsInteger);
            Assert.False(result.IsFloat);
            Assert.True(result.IsString);
            Assert.False(result.IsTable);
            Assert.False(result.IsFunction);
            Assert.False(result.IsThread);
            Assert.False(result.IsClrObject);
            Assert.False(result.IsClrTypes);
            Assert.Equal("test", (string)result);
        }

        [Fact]
        public void Eval_OneResult()
        {
            using var environment = new LuaEnvironment();

            var result = environment.Eval("return 1");

            Assert.Equal(1, (long)result);
        }

        [Fact]
        public void Eval_TwoResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2) = environment.Eval("return 1, 2");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
        }

        [Fact]
        public void Eval_ThreeResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3) = environment.Eval("return 1, 2, 3");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
            Assert.Equal(3, (long)result3);
        }

        [Fact]
        public void Eval_FourResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, result4) = environment.Eval("return 1, 2, 3, 4");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
            Assert.Equal(3, (long)result3);
            Assert.Equal(4, (long)result4);
        }

        [Fact]
        public void Eval_FiveResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, result4, result5) = environment.Eval("return 1, 2, 3, 4, 5");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
            Assert.Equal(3, (long)result3);
            Assert.Equal(4, (long)result4);
            Assert.Equal(5, (long)result5);
        }

        [Fact]
        public void Eval_SixResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, result4, result5, result6) = environment.Eval("return 1, 2, 3, 4, 5, 6");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
            Assert.Equal(3, (long)result3);
            Assert.Equal(4, (long)result4);
            Assert.Equal(5, (long)result5);
            Assert.Equal(6, (long)result6);
        }

        [Fact]
        public void Eval_SevenResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, result4, result5, result6, result7) =
                environment.Eval("return 1, 2, 3, 4, 5, 6, 7");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
            Assert.Equal(3, (long)result3);
            Assert.Equal(4, (long)result4);
            Assert.Equal(5, (long)result5);
            Assert.Equal(6, (long)result6);
            Assert.Equal(7, (long)result7);
        }

        [Fact]
        public void Eval_EightResults()
        {
            using var environment = new LuaEnvironment();

            var (result, result2, result3, result4, result5, result6, result7, result8) =
                environment.Eval("return 1, 2, 3, 4, 5, 6, 7, 8");

            Assert.Equal(1, (long)result);
            Assert.Equal(2, (long)result2);
            Assert.Equal(3, (long)result3);
            Assert.Equal(4, (long)result4);
            Assert.Equal(5, (long)result5);
            Assert.Equal(6, (long)result6);
            Assert.Equal(7, (long)result7);
            Assert.Equal(8, (long)result8);
        }
    }
}
