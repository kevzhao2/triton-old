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

using BenchmarkDotNet.Attributes;

namespace Triton.Benchmarks.Interop
{
    [MemoryDiagnoser]
    public class StaticProperty
    {
        public class TestClass
        {
            public static int Int { get; set; }
            public static string String { get; set; }
        }

        private NLua.Lua _nluaEnvironment;
        private LuaEnvironment _tritonEnvironment;

        [GlobalSetup]
        public void Setup()
        {
            _nluaEnvironment = new NLua.Lua();
            _tritonEnvironment = new LuaEnvironment();

            _nluaEnvironment.LoadCLRPackage();
            _nluaEnvironment.DoString("luanet.load_assembly('Triton.Benchmarks')");
            _nluaEnvironment.DoString(
                "TestClass = luanet.import_type('Triton.Benchmarks.Interop.StaticProperty+TestClass')");
            _tritonEnvironment["TestClass"] = LuaValue.FromClrType(typeof(TestClass));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nluaEnvironment.Dispose();
            _tritonEnvironment.Dispose();
        }

        [Benchmark]
        public void NLua_GetInt() => _nluaEnvironment.DoString(@"
            for i = 1, 10000 do
                _ = TestClass.Int
            end");

        [Benchmark]
        public void Triton_GetInt() => _tritonEnvironment.Eval(@"
            for i = 1, 10000 do
                _ = TestClass.Int
            end");

        [Benchmark]
        public void NLua_GetString() => _nluaEnvironment.DoString(@"
            for i = 1, 10000 do
                _ = TestClass.String
            end");

        [Benchmark]
        public void Triton_GetString() => _tritonEnvironment.Eval(@"
            for i = 1, 10000 do
                _ = TestClass.String
            end");

        [Benchmark]
        public void NLua_SetInt() => _nluaEnvironment.DoString(@"
            for i = 1, 10000 do
                TestClass.Int = 1234
            end");

        [Benchmark]
        public void Triton_SetInt() => _tritonEnvironment.Eval(@"
            for i = 1, 10000 do
                TestClass.Int = 1234
            end");

        [Benchmark]
        public void NLua_SetString() => _nluaEnvironment.DoString(@"
            for i = 1, 10000 do
                TestClass.String = 'test'
            end");

        [Benchmark]
        public void Triton_SetString() => _tritonEnvironment.Eval(@"
            for i = 1, 10000 do
                TestClass.String = 'test'
            end");
    }
}