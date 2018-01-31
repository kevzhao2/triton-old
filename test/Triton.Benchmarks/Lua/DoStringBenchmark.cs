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
    public class DoStringBenchmark {
        public (Action tritonAction, Action nluaAction) Benchmark_NoResults(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton.DoString("test = 0");
            void NLua() => nlua.DoString("test = 0");
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_Results(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton.DoString("return 1, 2, 3, 4, 5");
            void NLua() => nlua.DoString("return 1, 2, 3, 4, 5");
            return (Triton, NLua);
        }
    }
}
