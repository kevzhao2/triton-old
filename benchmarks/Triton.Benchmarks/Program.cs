using System;
using BenchmarkDotNet.Running;
using Triton.Benchmarks.Micro;

namespace Triton.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<LuaEnvironmentBenchmarks>();
            Console.ReadKey(true);
        }
    }
}
