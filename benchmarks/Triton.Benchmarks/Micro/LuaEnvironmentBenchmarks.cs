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
            _environment.SetGlobal("boolean", true);
            _environment.SetGlobal("integer", 1234);
            _environment.SetGlobal("number ", 1.234);
            _environment.SetGlobal("string ", "test");
        }

        [Benchmark]
        public bool GetGlobal_Boolean() => (bool)_environment.GetGlobal("boolean");

        [Benchmark]
        public long GetGlobal_Integer() => (long)_environment.GetGlobal("integer");

        [Benchmark]
        public double GetGlobal_Number() => (double)_environment.GetGlobal("number ");

        [Benchmark]
        public string GetGlobal_String() => (string)_environment.GetGlobal("string ");

        [Benchmark]
        public void SetGlobal_Boolean() => _environment.SetGlobal("boolean", true);

        [Benchmark]
        public void SetGlobal_Integer() => _environment.SetGlobal("integer", 1234);

        [Benchmark]
        public void SetGlobal_Number() => _environment.SetGlobal("number ", 1.234);

        [Benchmark]
        public void SetGlobal_String() => _environment.SetGlobal("string ", "test");
    }
}
