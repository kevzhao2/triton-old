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

namespace Triton.Benchmarks.Binding {
    public class ConstructorBenchmark : IBenchmark {
        public class TestClass {
            public TestClass() { }
            public TestClass(int x, int y) { }
        }

        public bool Enabled => true;
        public string Name => "Calling constructors";

        public (Action tritonAction, Action nluaAction) Benchmark_Construct(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.ConstructorBenchmark+TestClass')");
            var tritonFunction = triton.CreateFunction("x = TestClass()");
            var nluaFunction = nlua.LoadString("x = TestClass()", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ConstructArgs(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.ConstructorBenchmark+TestClass')");
            var tritonFunction = triton.CreateFunction("x = TestClass(0, 0)");
            var nluaFunction = nlua.LoadString("x = TestClass(0, 0)", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }
    }
}
