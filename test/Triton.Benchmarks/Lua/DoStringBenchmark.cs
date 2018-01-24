using System;

namespace Triton.Benchmarks.Lua {
    public class DoStringBenchmark : IBenchmark {
        public bool Enabled => false;
        public string Name => "DoString";

        public (Action tritonAction, Action nluaAction) Benchmark_NoResults(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton.DoString("test = 0");
            void NLua() => nlua.DoString("test = 0");
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ManyResults(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton.DoString("return 1, 2, 3, 4, 5");
            void NLua() => nlua.DoString("return 1, 2, 3, 4, 5");
            return (Triton, NLua);
        }
    }
}
