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

using Xunit;

namespace Triton.Interop
{
    public class FieldTests
    {
        [Fact]
        public void GetStatic_SByte()
        {
            Static.I1 = sbyte.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.I1");

            Assert.Equal(sbyte.MaxValue, (long)result);
        }

        [Fact]
        public void GetStatic_Int16()
        {
            Static.I2 = short.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.I2");

            Assert.Equal(short.MaxValue, (long)result);
        }

        [Fact]
        public void GetStatic_Int32()
        {
            Static.I4 = int.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.I4");

            Assert.Equal(int.MaxValue, (long)result);
        }

        [Fact]
        public void GetStatic_Int64()
        {
            Static.I8 = long.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.I8");

            Assert.Equal(long.MaxValue, (long)result);
        }
        
        [Fact]
        public void GetStatic_Byte()
        {
            Static.U1 = byte.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.U1");

            Assert.Equal(byte.MaxValue, (long)result);
        }

        [Fact]
        public void GetStatic_UInt16()
        {
            Static.U2 = ushort.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.U2");

            Assert.Equal(ushort.MaxValue, (long)result);
        }

        [Fact]
        public void GetStatic_UInt32()
        {
            Static.U4 = uint.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.U4");

            Assert.Equal(uint.MaxValue, (long)result);
        }

        [Fact]
        public void GetStatic_UInt64()
        {
            Static.U8 = ulong.MaxValue;

            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            var (result, _) = environment.Eval("return Static.U8");

            Assert.Equal(ulong.MaxValue, (ulong)(long)result);
        }

        private static class Static
        {
            public static sbyte I1;
            public static short I2;
            public static int I4;
            public static long I8;

            public static byte U1;
            public static ushort U2;
            public static uint U4;
            public static ulong U8;
        }
    }
}
