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
            _environment.Eval(@"
                boolean = true
                integer = 1234
                number_ = 1.234
                string_ = 'test'
                table__ = {}
                func___ = function() end
                thread_ = coroutine.create(function() end)");
        }

        [Benchmark]
        public bool GetGlobal_Boolean() => (bool)_environment.GetGlobal("boolean");

        [Benchmark]
        public long GetGlobal_Integer() => (long)_environment.GetGlobal("integer");

        [Benchmark]
        public double GetGlobal_Number() => (double)_environment.GetGlobal("number_");

        [Benchmark]
        public string GetGlobal_String() => (string)_environment.GetGlobal("string_");

        [Benchmark]
        public LuaTable GetGlobal_Table()
        {
            var table = (LuaTable)_environment.GetGlobal("table__");
            table.Dispose();
            return table;
        }

        [Benchmark]
        public LuaFunction GetGlobal_Function()
        {
            var function = (LuaFunction)_environment.GetGlobal("func___");
            function.Dispose();
            return function;
        }

        [Benchmark]
        public LuaThread GetGlobal_Thread()
        {
            var thread = (LuaThread)_environment.GetGlobal("thread_");
            thread.Dispose();
            return thread;
        }
    }
}
