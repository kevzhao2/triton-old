// Copyright (c) 2018 Kevin Zhao
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

namespace Triton.Benchmarks.Table {
    public class WriteTableBenchmark {
        public (Action tritonAction, Action nluaAction) Benchmark_WriteNil(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = null;
            void NLua() => nluaTable["test"] = null;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteBoolean(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = false;
            void NLua() => nluaTable["test"] = false;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteInteger(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = 0;
            void NLua() => nluaTable["test"] = 0;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteNumber(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = 0.0;
            void NLua() => nluaTable["test"] = 0.0;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteString(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = "test";
            void NLua() => nluaTable["test"] = "test";
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteObject(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = new object();
            void NLua() => nluaTable["test"] = new object();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteReference(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            var tritonFunction = triton.CreateFunction("");
            var nluaFunction = nlua.LoadString("", "test");

            void Triton() => tritonTable["test"] = tritonFunction;
            void NLua() => nluaTable["test"] = nluaFunction;
            return (Triton, NLua);
        }
    }
}
