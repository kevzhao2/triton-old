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
    public class LuaFunctionBenchmarks
    {
        private static readonly LuaEnvironment s_environment = new();
        private static readonly LuaFunction s_function = s_environment.CreateFunction("return 1, 2, 3, 4");

        [Benchmark]
        public LuaResults Call_NoArguments() => s_function.Call();

        [Benchmark]
        public LuaResults Call_OneArgument() => s_function.Call(1);

        [Benchmark]
        public LuaResults Call_TwoArguments() => s_function.Call(1, 2);

        [Benchmark]
        public LuaResults Call_ThreeArguments() => s_function.Call(1, 2, 3);
    }
}
