// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using BenchmarkDotNet.Attributes;

namespace Triton.Benchmarks.Interop.Micro
{
    /// <summary>
    /// Microbenchmarks for delegate calls.
    /// </summary>
    public class DelegateCall
    {
        private NLua.Lua _nluaEnvironment;
        private LuaEnvironment _tritonEnvironment;

        [GlobalSetup]
        public void Setup()
        {
            _nluaEnvironment = new NLua.Lua();
            _tritonEnvironment = new LuaEnvironment();

            _nluaEnvironment["idle"] = new Action(() => { });
            _tritonEnvironment["idle"] = LuaValue.FromClrObject(new Action(() => { }));
            _nluaEnvironment["square"] = new Func<int, int>(x => x * x);
            _tritonEnvironment["square"] = LuaValue.FromClrObject(new Func<int, int>(x => x * x));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nluaEnvironment.Dispose();
            _tritonEnvironment.Dispose();
        }

        [Benchmark]
        public void NLua_Idle() => _nluaEnvironment.DoString(@"
            for i = 1, 1000 do
                idle()
            end");

        [Benchmark]
        public void Triton_Idle() => _tritonEnvironment.Eval(@"
            for i = 1, 1000 do
                idle()
            end");

        [Benchmark]
        public void NLua_Square() => _nluaEnvironment.DoString(@"
            for i = 1, 1000 do
                _ = square(1234)
            end");

        [Benchmark]
        public void Triton_Square() => _tritonEnvironment.Eval(@"
            for i = 1, 1000 do
                _ = square(1234)
            end");
    }
}
