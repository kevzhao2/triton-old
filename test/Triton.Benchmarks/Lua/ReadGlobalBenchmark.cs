using System;

namespace Triton.Benchmarks.Lua {
    public class ReadGlobalBenchmark : IBenchmark {
        public bool Enabled => true;
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
