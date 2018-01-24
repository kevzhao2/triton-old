using System;

namespace Triton.Benchmarks.Table {
    public class ReadTableBenchmark : IBenchmark {
        public bool Enabled => true;
        public string Name => "Read table";

        public (Action tritonAction, Action nluaAction) Benchmark_ReadNil(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() {
                var t = triton["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadBoolean(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            tritonTable["test"] = false;
            nluaTable["test"] = false;

            void Triton() {
                var t = tritonTable["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadInteger(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            tritonTable["test"] = 0;
            nluaTable["test"] = 0;

            void Triton() {
                var t = tritonTable["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadNumber(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            tritonTable["test"] = 0.0;
            nluaTable["test"] = 0.0;

            void Triton() {
                var t = tritonTable["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadString(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            tritonTable["test"] = "test";
            nluaTable["test"] = "test";

            void Triton() {
                var t = tritonTable["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadObject(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            tritonTable["test"] = new object();
            nluaTable["test"] = new object();

            void Triton() {
                var t = tritonTable["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadReference(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            tritonTable["test"] = triton.LoadString("");
            nluaTable["test"] = nlua.LoadString("", "test");

            void Triton() {
                var t = tritonTable["test"];
            }
            void NLua() {
                var t = nluaTable["test"];
            }
            return (Triton, NLua);
        }
    }
}
