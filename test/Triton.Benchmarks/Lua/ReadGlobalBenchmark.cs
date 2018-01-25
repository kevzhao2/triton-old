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

namespace Triton.Benchmarks.Lua {
    public class ReadGlobalBenchmark : IBenchmark {
        public bool Enabled => false;
        public string Name => "Read globals";

        public (Action tritonAction, Action nluaAction) Benchmark_ReadNil(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadBoolean(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = false;
            nlua["test"] = false;

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadInteger(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = 0;
            nlua["test"] = 0;

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadNumber(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = 0.0;
            nlua["test"] = 0.0;

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadString(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = "test";
            nlua["test"] = "test";

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadObject(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new object();
            nlua["test"] = new object();

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadReference(Triton.Lua triton, NLua.Lua nlua) {
            triton.DoString("test = function() end");
            nlua.DoString("test = function() end");

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nlua["test"];
            }
            return (Triton, NLua);
        }
    }
}
