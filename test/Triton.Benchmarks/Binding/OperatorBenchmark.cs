using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public bool Enabled => true;
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
