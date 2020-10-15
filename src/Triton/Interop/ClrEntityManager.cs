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

using System;
using System.Runtime.InteropServices;
using static Triton.Lua;

namespace Triton.Interop
{
    /// <summary>
    /// Manages CLR entities. Controls the lifetime of CLR entities and provides methods to manipulate them.
    /// </summary>
    internal sealed unsafe class ClrEntityManager
    {
        private readonly MetatableGenerator _metatableGenerator = new();

        public void Push(lua_State* state, object entity, bool isTypes)
        {
            // Store a strong handle to the entity. This allows us to retrieve the entity and prevent it from being
            // garbage collected. The lowest bit indicates whether the entity is types.

            var handle = GCHandle.Alloc(entity);
            var ptr = lua_newuserdatauv(state, (nuint)IntPtr.Size, 0);
            *(nint*)ptr = ((nint)GCHandle.ToIntPtr(handle)) | Convert.ToInt32(isTypes);

            _metatableGenerator.Push(state, entity, isTypes);
            lua_setmetatable(state, -2);
        }
    }
}
