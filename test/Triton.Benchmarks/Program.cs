using System;
using System.Diagnostics;
using System.Linq;

namespace Triton.Benchmarks {
    class Program {
        static void Main(string[] args) {
            var benchmarkTypes = typeof(Program).Assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && typeof(IBenchmark).IsAssignableFrom(t));
            foreach (var benchmarkType in benchmarkTypes) {
                var benchmark = (IBenchmark)Activator.CreateInstance(benchmarkType);
                if (!benchmark.Enabled) {
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{benchmark.Name}:");
                foreach (var benchmarkMethod in benchmarkType.GetMethods().Where(m => m.Name.StartsWith("Benchmark_"))) {
                    using (var triton = new Triton.Lua())
                    using (var nlua = new NLua.Lua()) {
                        nlua.DoString("luanet.load_assembly('Triton.Benchmarks')");

                        var (tritonAction, nluaAction) =
                            ((Action tritonAction, Action nluaAction))benchmarkMethod.Invoke(benchmark, new object[] { triton, nlua });
                        Test(benchmarkMethod.Name, tritonAction, nluaAction);
                    }

                }
            }

            Console.ReadKey(true);
        }

        private static void Test(string name, Action tritonAction, Action nluaAction) {
            // Warm up the JIT.
            for (var i = 0; i < 1000; ++i) {
                tritonAction();
                nluaAction();
            }

            // Run benchmarks.
            var sw = new Stopwatch();
            var tritonIterations = 0;
            sw.Start();
            while (sw.ElapsedMilliseconds < 1000) {
                tritonAction();
                ++tritonIterations;
            }

            sw.Reset();
            var nluaIterations = 0;
            sw.Start();
            while (sw.ElapsedMilliseconds < 1000) {
                nluaAction();
                ++nluaIterations;
            }
            sw.Stop();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {name}:");

            if (tritonIterations > nluaIterations) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    Triton:  {tritonIterations,9}it/s");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"      NLua:  {nluaIterations,9}it/s");
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    Triton:  {tritonIterations,9}it/s");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"      NLua:  {nluaIterations,9}it/s");
            }
        }
    }
}
