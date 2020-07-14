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
    public class TableSetGet
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

            _nluaTable["boolean"] = true;
            _nluaTable["integer"] = 1234;
            _nluaTable["number"] = 1.234;
            _nluaTable["string"] = "test";
            _nluaTable["table"] = _nluaTable;
            _nluaTable["function"] = _nluaFunction;

            _tritonTable["boolean"] = true;
            _tritonTable["integer"] = 1234;
            _tritonTable["number"] = 1.234;
            _tritonTable["string"] = "test";
            _tritonTable["table"] = _tritonTable;
            _tritonTable["function"] = _tritonFunction;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nlua.Dispose();
            _triton.Dispose();
        }

        [Benchmark]
        public void NLua_SetNil() => _ = _nluaTable["nil"] = null;

        [Benchmark]
        public void Triton_SetNil() => _ = _tritonTable["nil"] = null;

        [Benchmark]
        public void NLua_GetNil() => _ = _nluaTable["nil"];

        [Benchmark]
        public void Triton_GetNil() => _ = _tritonTable["nil"];

        [Benchmark]
        public void NLua_SetBoolean() => _ = _nluaTable["boolean"] = true;

        [Benchmark]
        public void Triton_SetBoolean() => _ = _tritonTable["boolean"] = true;

        [Benchmark]
        public void NLua_GetBoolean() => _ = _nluaTable["boolean"];

        [Benchmark]
        public void Triton_GetBoolean() => _ = _tritonTable["boolean"];

        [Benchmark]
        public void NLua_SetInteger() => _ = _nluaTable["integer"] = 1234;

        [Benchmark]
        public void Triton_SetInteger() => _ = _tritonTable["integer"] = 1234;

        [Benchmark]
        public void NLua_GetInteger() => _ = _nluaTable["integer"];

        [Benchmark]
        public void Triton_GetInteger() => _ = _tritonTable["integer"];

        [Benchmark]
        public void NLua_SetNumber() => _ = _nluaTable["number"] = 1.234;

        [Benchmark]
        public void Triton_SetNumber() => _ = _tritonTable["number"] = 1.234;

        [Benchmark]
        public void NLua_GetNumber() => _ = _nluaTable["number"];

        [Benchmark]
        public void Triton_GetNumber() => _ = _tritonTable["number"];

        [Benchmark]
        public void NLua_SetString() => _ = _nluaTable["string"] = "test";

        [Benchmark]
        public void Triton_SetString() => _ = _tritonTable["string"] = "test";

        [Benchmark]
        public void NLua_GetString() => _ = _nluaTable["string"];

        [Benchmark]
        public void Triton_GetString() => _ = _tritonTable["string"];

        [Benchmark]
        public void NLua_SetTable() => _ = _nluaTable["table"] = _nluaTable;

        [Benchmark]
        public void Triton_SetTable() => _ = _tritonTable["table"] = _tritonTable;

        [Benchmark]
        public void NLua_GetTable() => _ = _nluaTable["table"];

        [Benchmark]
        public void Triton_GetTable() => _ = _tritonTable["table"];

        [Benchmark]
        public void NLua_SetFunction() => _ = _nluaTable["function"] = _nluaFunction;

        [Benchmark]
        public void Triton_SetFunction() => _ = _tritonTable["function"] = _tritonFunction;

        [Benchmark]
        public void NLua_GetFunction() => _ = _nluaTable["function"];

        [Benchmark]
        public void Triton_GetFunction() => _ = _tritonTable["function"];
    }
}
