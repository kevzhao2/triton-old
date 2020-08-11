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
    public class ConstTests
    {
        public static class NullConst
        {
            public const string? Value = null;
        }

        public static class BoolConst
        {
            public const bool Value = true;
        }

        public static class ByteConst
        {
            public const byte Value = 123;
        }

        public static class SByteConst
        {
            public const sbyte Value = -123;
        }

        public static class ShortConst
        {
            public const short Value = -12345;
        }

        public static class UShortConst
        {
            public const ushort Value = 12345;
        }

        public static class IntConst
        {
            public const int Value = -123456789;
        }

        public static class UIntConst
        {
            public const uint Value = 123456789;
        }

        public static class LongConst
        {
            public const long Value = -123456789;
        }

        public static class ULongConst
        {
            public const ulong Value = 123456789;
        }

        public static class FloatConst
        {
            public const float Value = 1.234f;
        }

        public static class DoubleConst
        {
            public const double Value = 1.234;
        }

        public static class StringConst
        {
            public const string Value = "test";
        }

        public static class CharConst
        {
            public const char Value = 'f';
        }

        [Fact]
        public void Null_Get()
        {
            using var environment = new LuaEnvironment();
            environment["NullConst"] = LuaValue.FromClrType(typeof(NullConst));

            environment.Eval("assert(NullConst.Value == nil)");
        }

        [Fact]
        public void Bool_Get()
        {
            using var environment = new LuaEnvironment();
            environment["BoolConst"] = LuaValue.FromClrType(typeof(BoolConst));

            environment.Eval("assert(BoolConst.Value)");
        }

        [Fact]
        public void Byte_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ByteConst"] = LuaValue.FromClrType(typeof(ByteConst));

            environment.Eval("assert(ByteConst.Value == 123)");
        }

        [Fact]
        public void SByte_Get()
        {
            using var environment = new LuaEnvironment();
            environment["SByteConst"] = LuaValue.FromClrType(typeof(SByteConst));

            environment.Eval("assert(SByteConst.Value == -123)");
        }

        [Fact]
        public void Short_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ShortConst"] = LuaValue.FromClrType(typeof(ShortConst));

            environment.Eval("assert(ShortConst.Value == -12345)");
        }

        [Fact]
        public void UShort_Get()
        {
            using var environment = new LuaEnvironment();
            environment["UShortConst"] = LuaValue.FromClrType(typeof(UShortConst));

            environment.Eval("assert(UShortConst.Value == 12345)");
        }

        [Fact]
        public void Int_Get()
        {
            using var environment = new LuaEnvironment();
            environment["IntConst"] = LuaValue.FromClrType(typeof(IntConst));

            environment.Eval("assert(IntConst.Value == -123456789)");
        }

        [Fact]
        public void UInt_Get()
        {
            using var environment = new LuaEnvironment();
            environment["UIntConst"] = LuaValue.FromClrType(typeof(UIntConst));

            environment.Eval("assert(UIntConst.Value == 123456789)");
        }

        [Fact]
        public void Long_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LongConst"] = LuaValue.FromClrType(typeof(LongConst));

            environment.Eval("assert(LongConst.Value == -123456789)");
        }

        [Fact]
        public void ULong_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ULongConst"] = LuaValue.FromClrType(typeof(ULongConst));

            environment.Eval("assert(ULongConst.Value == 123456789)");
        }

        [Fact]
        public void Float_Get()
        {
            using var environment = new LuaEnvironment();
            environment["FloatConst"] = LuaValue.FromClrType(typeof(FloatConst));

            environment.Eval("assert(FloatConst.Value == 1.2339999675750732)");
        }

        [Fact]
        public void Double_Get()
        {
            using var environment = new LuaEnvironment();
            environment["DoubleConst"] = LuaValue.FromClrType(typeof(DoubleConst));

            environment.Eval("assert(DoubleConst.Value == 1.234)");
        }

        [Fact]
        public void String_Get()
        {
            using var environment = new LuaEnvironment();
            environment["StringConst"] = LuaValue.FromClrType(typeof(StringConst));

            environment.Eval("assert(StringConst.Value == 'test')");
        }

        [Fact]
        public void Char_Get()
        {
            using var environment = new LuaEnvironment();
            environment["CharConst"] = LuaValue.FromClrType(typeof(CharConst));

            environment.Eval("assert(CharConst.Value == 'f')");
        }
    }
}
