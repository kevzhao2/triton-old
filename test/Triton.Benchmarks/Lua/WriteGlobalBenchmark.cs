using System;

namespace Triton.Benchmarks.Lua {
    public class WriteGlobalBenchmark : IBenchmark {
        public bool Enabled => true;
        public string Name => "Write globals";

        public (Action tritonAction, Action nluaAction) Benchmark_WriteNil(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton["test"] = null;
            void NLua() => nlua["test"] = null;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteBoolean(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton["test"] = false;
            void NLua() => nlua["test"] = false;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteInteger(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton["test"] = 0;
            void NLua() => nlua["test"] = 0;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteNumber(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton["test"] = 0.0;
            void NLua() => nlua["test"] = 0.0;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteString(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton["test"] = "test";
            void NLua() => nlua["test"] = "test";
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteObject(Triton.Lua triton, NLua.Lua nlua) {
            void Triton() => triton["test"] = new object();
            void NLua() => nlua["test"] = new object();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteReference(Triton.Lua triton, NLua.Lua nlua) {
            var tritonFunction = triton.LoadString("");
            var nluaFunction = nlua.LoadString("", "test");

            void Triton() => triton["test"] = tritonFunction;
            void NLua() => nlua["test"] = nluaFunction;
            return (Triton, NLua);
        }
    }
}
