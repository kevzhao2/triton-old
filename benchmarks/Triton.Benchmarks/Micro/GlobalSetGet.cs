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
    [MemoryDiagnoser]
    public class GlobalSetGet
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
            _triton = new LuaEnvironment();

            _nluaFunction = _nlua.LoadString("return", "test");
            _tritonFunction = _triton.CreateFunction("return");

            _nluaTable = (NLua.LuaTable)_nlua.DoString("return {}")[0];
            _tritonTable = _triton.CreateTable();

            _nlua["boolean"] = true;
            _nlua["integer"] = 1234;
            _nlua["number"] = 1.234;
            _nlua["string"] = "test";
            _nlua["table"] = _nluaTable;
            _nlua["function"] = _nluaFunction;

            _triton["boolean"] = true;
            _triton["integer"] = 1234;
            _triton["number"] = 1.234;
            _triton["string"] = "test";
            _triton["table"] = _tritonTable;
            _triton["function"] = _tritonFunction;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nlua.Dispose();
            _triton.Dispose();
        }

        [Benchmark]
        public void NLua_SetNil() => _nlua["nil"] = null;

        [Benchmark]
        public void Triton_SetNil() => _triton["nil"] = LuaVariant.Nil;

        [Benchmark]
        public void NLua_GetNil() => _ = _nlua["nil"];

        [Benchmark]
        public void Triton_GetNil() => _ = _triton["nil"];

        [Benchmark]
        public void NLua_SetBoolean() => _nlua["boolean"] = true;

        [Benchmark]
        public void Triton_SetBoolean() => _triton["boolean"] = true;

        [Benchmark]
        public void NLua_GetBoolean() => _ = _nlua["boolean"];

        [Benchmark]
        public void Triton_GetBoolean() => _ = _triton["boolean"];

        [Benchmark]
        public void NLua_SetInteger() => _nlua["integer"] = 1234;

        [Benchmark]
        public void Triton_SetInteger() => _triton["integer"] = 1234;

        [Benchmark]
        public void NLua_GetInteger() => _ = _nlua["integer"];

        [Benchmark]
        public void Triton_GetInteger() => _ = _triton["integer"];

        [Benchmark]
        public void NLua_SetNumber() => _nlua["number"] = 1.234;

        [Benchmark]
        public void Triton_SetNumber() => _triton["number"] = 1.234;

        [Benchmark]
        public void NLua_GetNumber() => _ = _nlua["number"];

        [Benchmark]
        public void Triton_GetNumber() => _ = _triton["number"];

        [Benchmark]
        public void NLua_SetString() => _nlua["string"] = "test";

        [Benchmark]
        public void Triton_SetString() => _triton["string"] = "test";

        [Benchmark]
        public void NLua_GetString() => _ = _nlua["string"];

        [Benchmark]
        public void Triton_GetString() => _ = _triton["string"];

        [Benchmark]
        public void NLua_SetTable() => _nlua["table"] = _nluaTable;

        [Benchmark]
        public void Triton_SetTable() => _triton["table"] = _tritonTable;

        [Benchmark]
        public void NLua_GetTable() => _ = _nlua["table"];

        [Benchmark]
        public void Triton_GetTable() => _ = _triton["table"];

        [Benchmark]
        public void NLua_SetFunction() => _nlua["function"] = _nluaFunction;

        [Benchmark]
        public void Triton_SetFunction() => _triton["function"] = _tritonFunction;

        [Benchmark]
        public void NLua_GetFunction() => _ = _nlua["function"];

        [Benchmark]
        public void Triton_GetFunction() => _ = _triton["function"];
    }
}
