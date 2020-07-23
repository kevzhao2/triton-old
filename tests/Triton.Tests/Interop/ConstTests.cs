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
        [Fact]
        public void GetConsts()
        {
            using var environment = new LuaEnvironment();
            environment["Consts"] = LuaValue.FromClrType(typeof(Consts));

            environment.Eval("assert(Consts.I4 == 2147483647)");
            environment.Eval("assert(Consts.String == 'Hello, world!')");
        }

        [Fact]
        public void GetInheritedConsts()
        {
            using var environment = new LuaEnvironment();
            environment["InheritedConsts"] = LuaValue.FromClrType(typeof(InheritedConsts));

            environment.Eval("assert(InheritedConsts.I4 == 2147483647)");
            environment.Eval("assert(InheritedConsts.String == 'Hello, world!')");
        }

        private class Consts
        {
            public const int I4 = int.MaxValue;
            public const string String = "Hello, world!";
        }

        private class InheritedConsts : Consts
        {
        }
    }
}
