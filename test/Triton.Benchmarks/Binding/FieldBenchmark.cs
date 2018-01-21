using System;

namespace Triton.Benchmarks.Binding {
    public class FieldBenchmark : IBenchmark {
        public class TestClass {
            public static int X;
            public int x;
        }

        public bool Enabled => true;
        public string Name => "Field";

        public (Action tritonAction, Action nluaAction) Benchmark_ReadInstance(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new TestClass();
            nlua["test"] = new TestClass();

            void Triton() => triton.DoString("x = test.x");
            void NLua() => nlua.DoString("x = test.x");
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadStatic(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.FieldBenchmark+TestClass')");
            
            void Triton() => triton.DoString("x = TestClass.X");
            void NLua() => nlua.DoString("x = TestClass.X");
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteInstance(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new TestClass();
            nlua["test"] = new TestClass();

            void Triton() => triton.DoString("test.x = 0");
            void NLua() => nlua.DoString("test.x = 0");
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteStatic(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.FieldBenchmark+TestClass')");

            void Triton() => triton.DoString("TestClass.X = 0");
            void NLua() => nlua.DoString("TestClass.X = 0");
            return (Triton, NLua);
        }
    }
}
