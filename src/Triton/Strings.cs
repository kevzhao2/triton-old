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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Triton
{
    /// <summary>
    /// Stores strings.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Consistency")]
    internal static class Strings
    {
        // These strings are stored directly as data in the assembly, allowing for efficient use in Lua functions.
        //

        internal static readonly IntPtr __call = AddrOf(new byte[]
        {
            0x5f, 0x5f, 0x63, 0x61, 0x6c, 0x6c, 0x00
        });

        internal static readonly IntPtr __gc = AddrOf(new byte[]
        {
            0x5f, 0x5f, 0x67, 0x63, 0x00
        });

        internal static readonly IntPtr __index = AddrOf(new byte[]
        {
            0x5f, 0x5f, 0x69, 0x6e, 0x64, 0x65, 0x78, 0x00
        });

        internal static readonly IntPtr __mode = AddrOf(new byte[]
        {
            0x5f, 0x5f, 0x6d, 0x6f, 0x64, 0x65, 0x00
        });

        internal static readonly IntPtr __newindex = AddrOf(new byte[]
        {
            0x5f, 0x5f, 0x6e, 0x65, 0x77, 0x69, 0x6e, 0x64, 0x65, 0x78, 0x00
        });

        internal static readonly IntPtr __tostring = AddrOf(new byte[]
        {
            0x5f, 0x5f, 0x7f, 0x6f, 0x73, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x00
        });

        internal static readonly IntPtr v = AddrOf(new byte[]
        {
            0x76, 0x00
        });

        private static unsafe IntPtr AddrOf(ReadOnlySpan<byte> bytes) =>
            (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes));
    }
}
