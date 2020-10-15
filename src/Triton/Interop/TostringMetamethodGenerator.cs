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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Triton.Lua;

namespace Triton.Interop
{
    /// <summary>
    /// Generates the <c>__tostring</c> metamethod for CLR entities.
    /// </summary>
    internal sealed unsafe class TostringMetamethodGenerator : StaticMetamethodGenerator
    {
        public override string Name => "__tostring";

        protected override unsafe delegate* unmanaged[Cdecl]<lua_State*, int> Metamethod => &TostringMetamethod;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static int TostringMetamethod(lua_State* state)
        {
            var ptr = *(nint*)lua_topointer(state, 1);
            var handle = GCHandle.FromIntPtr(ptr & ~1);

            lua_pushstring(state, handle.Target!.ToString()!);  // Assume no exceptions
            return 1;
        }
    }
}
