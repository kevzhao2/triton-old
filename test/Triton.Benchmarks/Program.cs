// Copyright (c) 2018 Kevin Zhao
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

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
            var tritonIterations = GetIterations(tritonAction);
            var nluaIterations = GetIterations(nluaAction);
            var maxIterations = Math.Max(tritonIterations, nluaIterations);
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {name}:");
            Console.ForegroundColor = tritonIterations == maxIterations ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"    Triton:  {tritonIterations,9}it/s");
            Console.ForegroundColor = nluaIterations == maxIterations ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"      NLua:  {nluaIterations,9}it/s");

            int GetIterations(Action action) {
                var sw = new Stopwatch();
                sw.Start();

                var iterations = 0;
                while (sw.ElapsedMilliseconds < 1000) {
                    action();
                    ++iterations;
                }
                return iterations;
            }
        }
    }
}
