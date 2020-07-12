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
        public void Ctor_NullEncoding_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LuaEnvironment(null!));
        }

        [Fact]
        public void Encoding_Get()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            Assert.Same(Encoding.ASCII, environment.Encoding);
        }

        [Fact]
        public void CreateTable_ObjectDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment(Encoding.ASCII);
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.CreateTable());
        }

        [Fact]
        public void CreateTable_NegativeSequentialCapacity_ThrowsArgumentOutOfRangeException()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            Assert.Throws<ArgumentOutOfRangeException>(() => environment.CreateTable(sequentialCapacity: -1));
        }

        [Fact]
        public void CreateTable_NegativeNonSequentialCapacity_ThrowsArgumentOutOfRangeException()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            Assert.Throws<ArgumentOutOfRangeException>(() => environment.CreateTable(nonSequentialCapacity: -1));
        }

        [Fact]
        public void CreateFunction_NullS_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            Assert.Throws<ArgumentNullException>(() => environment.CreateFunction(null!));
        }

        [Fact]
        public void CreateFunction_ObjectDisposed_ThrowsObjectDisposedException()
        {
            var environment = new LuaEnvironment(Encoding.ASCII);
            environment.Dispose();

            Assert.Throws<ObjectDisposedException>(() => environment.CreateFunction("return 0"));
        }

        [Fact]
        public void CreateFunction()
        {
            using var environment = new LuaEnvironment(Encoding.ASCII);

            var function = environment.CreateFunction("return 0");
        }
    }
}
