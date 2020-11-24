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
    public class LuaEnvironmentBenchmarks
    {
        private readonly LuaEnvironment _environment = new();

        [GlobalSetup]
        public void Setup()
        {
            // Pad the global names so that the string sizes match.
            //
            _environment["boolean"] = true;
            _environment["integer"] = 1234;
            _environment["number "] = 1.234;
            _environment["string "] = "test";
        }

        [Benchmark]
        public LuaValue GetGlobal_Boolean() => _environment["boolean"];

        [Benchmark]
        public LuaValue GetGlobal_Integer() => _environment["integer"];

        [Benchmark]
        public LuaValue GetGlobal_Number() => _environment["number "];

        [Benchmark]
        public LuaValue GetGlobal_String() => _environment["string "];

        [Benchmark]
        public void SetGlobal_Boolean() => _environment["boolean"] = true;

        [Benchmark]
        public void SetGlobal_Integer() => _environment["integer"] = 1234;

        [Benchmark]
        public void SetGlobal_Number() => _environment["number "] = 1.234;

        [Benchmark]
        public void SetGlobal_String() => _environment["string "] = "test";
    }
}
