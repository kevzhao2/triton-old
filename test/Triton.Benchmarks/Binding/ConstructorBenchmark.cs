using System;

namespace Triton.Benchmarks.Binding {
    public class ConstructorBenchmark : IBenchmark {
        public class TestClass {
            public TestClass() { }
            public TestClass(int x, int y) { }
        }

        public bool Enabled => true;
        public string Name => "Calling constructors";

        public (Action tritonAction, Action nluaAction) Benchmark_Construct(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.ConstructorBenchmark+TestClass')");
            var tritonFunction = triton.CreateFunction("x = TestClass()");
            var nluaFunction = nlua.LoadString("x = TestClass()", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_ConstructArgs(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.ConstructorBenchmark+TestClass')");
            var tritonFunction = triton.CreateFunction("x = TestClass(0, 0)");
            var nluaFunction = nlua.LoadString("x = TestClass(0, 0)", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }
    }
}
