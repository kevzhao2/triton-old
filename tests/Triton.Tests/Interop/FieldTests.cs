﻿// Copyright (c) 2020 Kevin Zhao
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
        public void GetStatics()
        {
            using var environment = new LuaEnvironment();
            environment["Static"] = LuaValue.FromClrType(typeof(Static));
            var (table, _) = environment.Eval("table = {} return table");

            Static.I4 = 1234;
            Static.R8 = 1.234;
            Static.String = "Hello, world!";
            Static.LuaObject = (LuaObject?)table;

            environment.Eval("assert(Static.I4 == 1234)");
            environment.Eval("assert(Static.R8 == 1.234)");
            environment.Eval("assert(Static.String == 'Hello, world!')");
            environment.Eval("assert(Static.LuaObject == table)");
        }

        private static class Static
        {
            public static int I4;
            public static double R8;
            public static string? String;
            public static LuaObject? LuaObject;
        }
    }
}