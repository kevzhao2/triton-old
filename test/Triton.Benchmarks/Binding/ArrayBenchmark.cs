using System;

namespace Triton.Benchmarks.Binding {
    public class ArrayBenchmark : IBenchmark {
        public class TestClass {
            public static int X;
            public int x;
        }

        public bool Enabled => false;
        public string Name => "Arrays";

        public (Action tritonAction, Action nluaAction) Benchmark_Read(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new int[10];
            nlua["test"] = new int[10];
            var tritonFunction = triton.CreateFunction("x = test[0]");
            var nluaFunction = nlua.LoadString("x = test[0]", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_Write(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new int[10];
            nlua["test"] = new int[10];
            var tritonFunction = triton.CreateFunction("test[0] = 10");
            var nluaFunction = nlua.LoadString("test[0] = 10", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }
    }
}
