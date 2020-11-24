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

using BenchmarkDotNet.Attributes;

namespace Triton.Benchmarks.Micro
{
    [DisassemblyDiagnoser(2)]
    [MemoryDiagnoser]
    public class LuaValueBenchmarks
    {
        private readonly LuaValue _nil     = default;
        private readonly LuaValue _boolean = true;
        private readonly LuaValue _integer = 1234;
        private readonly LuaValue _number  = 1.234;
        private readonly LuaValue _string  = "test";

        [Benchmark]
        public bool IsNil()     => _nil.IsNil;

        [Benchmark]
        public bool IsBoolean() => _boolean.IsBoolean;

        [Benchmark]
        public bool IsInteger() => _integer.IsInteger;

        [Benchmark]
        public bool IsNumber()  => _number.IsNumber;

        [Benchmark]
        public bool IsString()  => _string.IsString;

        [Benchmark]
        public bool AsBoolean() => _boolean.AsBoolean();

        [Benchmark]
        public long AsInteger() => _integer.AsInteger();

        [Benchmark]
        public double AsNumber() => _number.AsNumber();

        [Benchmark]
        public string AsString() => _string.AsString();
    }
}
