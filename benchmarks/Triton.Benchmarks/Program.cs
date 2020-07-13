﻿// Copyright (c) 2020 Kevin Zhao
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
using BenchmarkDotNet.Running;
using Triton.Benchmarks.Micro;

namespace Triton.Benchmarks
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Enter the benchmark to run:");
            Console.WriteLine("\tfc = FunctionCall");
            Console.WriteLine("\ttsg = TableSetGet");

            var option = Console.ReadLine();

            var type = option switch
            {
                "fc" => typeof(FunctionCall),
                "tsg" => typeof(TableSetGet),
                _ => throw new InvalidOperationException(),
            };

            BenchmarkRunner.Run(type);
            Console.ReadKey(true);
        }
    }
}