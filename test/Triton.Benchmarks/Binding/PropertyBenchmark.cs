using System;

namespace Triton.Benchmarks.Binding {
    public class PropertyBenchmark : IBenchmark {
        public class TestClass {
            public static int X { get; set; }
            public int x { get; set; }
        }

        public bool Enabled => true;
        public string Name => "Properties";

        public (Action tritonAction, Action nluaAction) Benchmark_ReadInstance(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new TestClass();
            nlua["test"] = new TestClass();
            var tritonFunction = triton.CreateFunction("x = test.x");
            var nluaFunction = nlua.LoadString("x = test.x", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ReadStatic(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.PropertyBenchmark+TestClass')");
            var tritonFunction = triton.CreateFunction("x = TestClass.X");
            var nluaFunction = nlua.LoadString("x = TestClass.X", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteInstance(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new TestClass();
            nlua["test"] = new TestClass();
            var tritonFunction = triton.CreateFunction("test.x = 0");
            var nluaFunction = nlua.LoadString("test.x = 0", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_WriteStatic(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.PropertyBenchmark+TestClass')");
            var tritonFunction = triton.CreateFunction("TestClass.X = 0");
            var nluaFunction = nlua.LoadString("TestClass.X = 0", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }
    }
}
