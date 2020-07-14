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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Triton
{
    public unsafe partial class LuaEnvironment
    {
        private const int StringBufferSize = 1 << 16;

        private readonly byte* _stringBuffer;

        /// <summary>
        /// Creates a <see cref="StringBuffer"/> instance from the given string <paramref name="s"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The resulting <see cref="StringBuffer"/> instance.</returns>
        internal StringBuffer CreateStringBuffer(string s)
        {
            Debug.Assert(s != null);

            // If the maximum byte length is small enough, then we can use `_stringBuffer`. Otherwise, we'll have to
            // perform an allocation.

            var maxByteLength = Encoding.UTF8.GetMaxByteCount(s.Length) + 1;  // Include space for null terminator
            var isAllocated = maxByteLength > StringBufferSize;
            var ptr = isAllocated ? (byte*)Marshal.AllocHGlobal(maxByteLength) : _stringBuffer;

            fixed (char* sPtr = s)
            {
                var length = Encoding.UTF8.GetBytes(sPtr, s.Length, ptr, maxByteLength);
                ptr[length] = 0;  // Null terminator

                return new StringBuffer(ptr, (UIntPtr)length, isAllocated);
            }
        }

        /// <summary>
        /// Represents a string buffer (either located in the Lua environment's string buffer or on the heap).
        /// </summary>
        internal ref struct StringBuffer
        {
            private readonly byte* _ptr;
            private readonly bool _isAllocated;

            public StringBuffer(byte* ptr, UIntPtr length, bool isAllocated)
            {
                Debug.Assert(ptr != null);
                Debug.Assert((int)length >= 0);

                _ptr = ptr;
                Length = length;
                _isAllocated = isAllocated;
            }

            public UIntPtr Length { get; }

            public void Dispose()
            {
                if (_isAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)_ptr);
                }
            }

            public static implicit operator byte*(StringBuffer buffer) => buffer._ptr;
        }
    }
}
