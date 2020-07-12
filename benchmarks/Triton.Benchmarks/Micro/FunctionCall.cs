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

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Triton.Benchmarks.Micro
{
    [MemoryDiagnoser]
    public class FunctionCall
    {
        private NLua.Lua _nlua;
        private LuaEnvironment _triton;

        private NLua.LuaFunction _nluaFunction;
        private LuaFunction _tritonFunction;

        private NLua.LuaTable _nluaTable;
        private LuaTable _tritonTable;

        [GlobalSetup]
        public void Setup()
        {
            _nlua = new NLua.Lua();
            _triton = new LuaEnvironment(Encoding.ASCII);

            _nluaFunction = _nlua.LoadString("return", "test");
            _tritonFunction = _triton.CreateFunction("return");

            _nluaTable = (NLua.LuaTable)_nlua.DoString("return {}")[0];
            _tritonTable = _triton.CreateTable();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nlua.Dispose();
            _triton.Dispose();
        }

        [Benchmark]
        public void NLua_ZeroArguments()
        {
            _ = _nluaFunction.Call();
        }

        [Benchmark]
        public void Triton_ZeroArguments()
        {
            _ = _tritonFunction.Call();
        }

        [Benchmark]
        public void NLua_IntArgument()
        {
            _ = _nluaFunction.Call(1234);
        }

        [Benchmark]
        public void Triton_IntArgument()
        {
            _ = _tritonFunction.Call(1234);
        }

        [Benchmark]
        public void NLua_DoubleArgument()
        {
            _ = _nluaFunction.Call(1.234);
        }

        [Benchmark]
        public void Triton_DoubleArgument()
        {
            _ = _tritonFunction.Call(1.234);
        }

        [Benchmark]
        public void NLua_StringArgument()
        {
            _ = _nluaFunction.Call("test");
        }

        [Benchmark]
        public void Triton_StringArgument()
        {
            _ = _tritonFunction.Call("test");
        }

        [Benchmark]
        public void NLua_TableArgument()
        {
            _ = _nluaFunction.Call(_nluaTable);
        }

        [Benchmark]
        public void Triton_TableArgument()
        {
            _ = _tritonFunction.Call(_tritonTable);
        }

        [Benchmark]
        public void NLua_FunctionArgument()
        {
            _ = _nluaFunction.Call(_nluaFunction);
        }

        [Benchmark]
        public void Triton_FunctionArgument()
        {
            _ = _tritonFunction.Call(_tritonFunction);
        }
    }
}
