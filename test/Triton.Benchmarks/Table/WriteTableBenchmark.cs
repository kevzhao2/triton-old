using System;

namespace Triton.Benchmarks.Table {
    public class WriteTableBenchmark : IBenchmark {
        public bool Enabled => false;
        public string Name => "Write table";

        public (Action tritonAction, Action nluaAction) Benchmark_WriteNil(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = null;
            void NLua() => nluaTable["test"] = null;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteBoolean(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = false;
            void NLua() => nluaTable["test"] = false;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteInteger(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = 0;
            void NLua() => nluaTable["test"] = 0;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteNumber(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = 0.0;
            void NLua() => nluaTable["test"] = 0.0;
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteString(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = "test";
            void NLua() => nluaTable["test"] = "test";
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteObject(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];

            void Triton() => tritonTable["test"] = new object();
            void NLua() => nluaTable["test"] = new object();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteReference(Triton.Lua triton, NLua.Lua nlua) {
            var tritonTable = triton.CreateTable();
            var nluaTable = (NLua.LuaTable)nlua.DoString("return {}")[0];
            var tritonFunction = triton.LoadString("");
            var nluaFunction = nlua.LoadString("", "test");

            void Triton() => tritonTable["test"] = tritonFunction;
            void NLua() => nluaTable["test"] = nluaFunction;
            return (Triton, NLua);
        }
    }
}
