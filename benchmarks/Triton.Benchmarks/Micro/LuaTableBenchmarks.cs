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
    public class LuaTableBenchmarks
    {
        private static readonly LuaEnvironment s_environment = new();
        private static readonly LuaTable s_table = CreateTable();

        private static LuaTable CreateTable()
        {
            var table = s_environment.CreateTable();
            table.SetValue("a", 1);
            table.SetValue("b", 2);
            table.SetValue("c", 3);
            table.SetValue("d", 4);
            table.SetValue(1, 1);
            table.SetValue(2, 1);
            table.SetValue(3, 1);
            table.SetValue(4, 1);
            table.SetValue(true, 1);
            return table;
        }

        [Benchmark]
        public LuaResult GetValue_String() => s_table.GetValue("a");

        [Benchmark]
        public LuaResult GetValue_Long() => s_table.GetValue(1);

        [Benchmark]
        public LuaResult GetValue_LuaArgument() => s_table.GetValue(true);
    }
}
