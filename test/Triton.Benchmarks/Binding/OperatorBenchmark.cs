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
    public class OperatorBenchmark : IBenchmark {
        public class Complex {
            public double R { get; }
            public double I { get; }

            public Complex(double r, double i) {
                R = r;
                I = i;
            }

            public static Complex operator +(Complex c1, Complex c2) => new Complex(c1.R + c2.R, c1.I + c2.I);
            public static Complex operator -(Complex c1, Complex c2) => new Complex(c1.R - c2.R, c1.I - c2.I);
            public static Complex operator -(Complex c) => new Complex(-c.R, -c.I);
        }

        public bool Enabled => false;
        public string Name => "Calling operators";

        public (Action tritonAction, Action nluaAction) Benchmark_BinaryOperator(Triton.Lua triton, NLua.Lua nlua) {
            triton["c1"] = new Complex(1, 1);
            triton["c2"] = new Complex(-1, -1);
            nlua["c1"] = new Complex(1, 1);
            nlua["c2"] = new Complex(-1, -1);
            var tritonFunction = triton.CreateFunction("x = c1 + c2");
            var nluaFunction = nlua.LoadString("x = c1 + c2", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_UnaryOperator(Triton.Lua triton, NLua.Lua nlua) {
            triton["c"] = new Complex(1, 1);
            nlua["c"] = new Complex(1, 1);
            var tritonFunction = triton.CreateFunction("x = -c");
            var nluaFunction = nlua.LoadString("x = -c", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }
    }
}
