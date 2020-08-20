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

namespace Triton.Benchmarks.Interop
{
    [MemoryDiagnoser]
    public class InstanceMethod
    {
        public class TestClass
        {
            public void VoidMethod()
            {
            }

            public void VoidMethod(int x)
            {
            }
        }

        private NLua.Lua _nluaEnvironment;
        private LuaEnvironment _tritonEnvironment;

        [GlobalSetup]
        public void Setup()
        {
            _nluaEnvironment = new NLua.Lua();
            _tritonEnvironment = new LuaEnvironment();

            _nluaEnvironment["obj"] = new TestClass();
            _tritonEnvironment["obj"] = LuaValue.FromClrObject(new TestClass());
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _nluaEnvironment.Dispose();
            _tritonEnvironment.Dispose();
        }

        [Benchmark]
        public void NLua_VoidMethod_NoArgs() => _nluaEnvironment.DoString(@"
            for i = 1, 10000 do
                obj:VoidMethod()
            end");

        [Benchmark]
        public void Triton_VoidMethod_NoArgs() => _tritonEnvironment.Eval(@"
            for i = 1, 10000 do
                obj:VoidMethod()
            end");

        [Benchmark]
        public void NLua_VoidMethod_OneArg() => _nluaEnvironment.DoString(@"
            for i = 1, 10000 do
                obj:VoidMethod(1234)
            end");

        [Benchmark]
        public void Triton_VoidMethod_OneArg() => _tritonEnvironment.Eval(@"
            for i = 1, 10000 do
                obj:VoidMethod(1234)
            end");
    }
}
