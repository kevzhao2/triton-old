using System;

namespace Triton.Benchmarks.Binding {
    public class MethodBenchmark : IBenchmark {
        public class TestClass {
            public static void X() {
            }

            public void x() {
            }
        }

        public bool Enabled => true;
        public string Name => "Methods";

        public (Action tritonAction, Action nluaAction) Benchmark_CallInstance(Triton.Lua triton, NLua.Lua nlua) {
            triton["test"] = new TestClass();
            nlua["test"] = new TestClass();
            var tritonFunction = triton.LoadString("test:x()");
            var nluaFunction = nlua.LoadString("test:x()", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }

        public (Action tritonAction, Action nluaAction) Benchmark_CallStatic(Triton.Lua triton, NLua.Lua nlua) {
            triton.ImportType(typeof(TestClass));
            nlua.DoString("TestClass = luanet.import_type('Triton.Benchmarks.Binding.MethodBenchmark+TestClass')");
            var tritonFunction = triton.LoadString("TestClass.X()");
            var nluaFunction = nlua.LoadString("TestClass.X()", "test");

            void Triton() => tritonFunction.Call();
            void NLua() => nluaFunction.Call();
            return (Triton, NLua);
        }
    }
}
