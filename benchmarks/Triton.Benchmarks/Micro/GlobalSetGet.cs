// Copyright (c) 2020 Kevin Zhao
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

using BenchmarkDotNet.Attributes;

namespace Triton.Benchmarks.Micro
{
    [MemoryDiagnoser]
    public class GlobalSetGet
    {
        private NLua.Lua _nlua;
        private LuaEnvironment _triton;

        [GlobalSetup]
        public void Setup()
        {
            _nlua = new NLua.Lua();
            _triton = new LuaEnvironment();

            _nlua["boolean"] = true;
            _nlua["integer"] = 1234;
            _nlua["number"] = 1.234;
            _nlua["string"] = "test";

            _triton["boolean"] = true;
            _triton["integer"] = 1234;
            _triton["number"] = 1.234;
            _triton["string"] = "test";
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nlua.Dispose();
            _triton.Dispose();
        }

        [Benchmark]
        public void Triton_GetNil() => _ = _triton["nil"];

        [Benchmark]
        public void Triton_GetBoolean() => _ = _triton["boolean"];

        [Benchmark]
        public void Triton_GetInteger() => _ = _triton["integer"];

        [Benchmark]
        public void Triton_GetNumber() => _ = _triton["number"];

        [Benchmark]
        public void Triton_GetString() => _ = _triton["string"];
    }
}
