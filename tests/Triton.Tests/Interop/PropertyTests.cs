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
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Triton.Interop
{
    public class PropertyTests
    {
        [Fact]
        public void GetStatics()
        {
            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));

            environment.Eval("assert(Static.I4 == 1234)");
            environment.Eval("assert(Static.R8 == 1.234)");
            environment.Eval("assert(Static.String == 'Hello, world!')");
        }

        [Fact]
        public void GetStaticsByRef()
        {
            using var environment = new LuaEnvironment();
            environment["StaticByRef"] = LuaValue.FromClrType(typeof(StaticByRef));

            StaticByRef.I4 = 1234;
            StaticByRef.R8 = 1.234;
            StaticByRef.String = "Hello, world!";

            environment.Eval("assert(StaticByRef.I4 == 1234)");
            environment.Eval("assert(StaticByRef.R8 == 1.234)");
            environment.Eval("assert(StaticByRef.String == 'Hello, world!')");
        }

        [Fact]
        public void GetStaticNonReadable_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["StaticNonReadable"] = LuaValue.FromClrType(typeof(StaticNonReadable));

            var exception = Assert.Throws<LuaEvalException>(() => environment.Eval("i4 = StaticNonReadable.I4"));
            Assert.Contains("non-readable property", exception.Message);
        }

        [Fact]
        public void GetStaticByRefLike_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["StaticByRefLike"] = LuaValue.FromClrType(typeof(StaticByRefLike));

            var exception = Assert.Throws<LuaEvalException>(() => environment.Eval("span = StaticByRefLike.Span"));
            Assert.Contains("byref-like property", exception.Message);
        }

        private static class Static
        {
            public static int I4 => 1234;
            public static double R8 => 1.234;
            public static string String => "Hello, world!";
        }

        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Ref returns")]
        private static class StaticByRef
        {
            private static int _i4;
            private static double _r8;
            private static string? _string;

            public static ref int I4 => ref _i4;
            public static ref double R8 => ref _r8;
            public static ref string? String => ref _string;
        }

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Testing")]
        private static class StaticNonReadable
        {
            private static int _i4;

            public static int I4 { set => _i4 = value; }
        }

        private static class StaticByRefLike
        {
            public static Span<byte> Span => Span<byte>.Empty;
        }
    }
}
