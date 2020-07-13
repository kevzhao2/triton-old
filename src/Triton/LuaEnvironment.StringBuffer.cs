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
        // Marshals a string to a string buffer using the Lua environment's encoding.
        internal StringBuffer CreateStringBuffer(string s, bool isNullTerminated)
        {
            Debug.Assert(s != null);

            var maxByteLength = Encoding.GetMaxByteCount(s.Length) + (isNullTerminated ? 1 : 0);

            // If the maximum byte length is small enough, then we can use `_stringBuffer`. Otherwise, we'll have to
            // perform an allocation.
            var isAllocated = maxByteLength > StringBufferSize;

            byte* pointer = isAllocated ? (byte*)Marshal.AllocHGlobal(maxByteLength) : _stringBuffer;

            try
            {
                UIntPtr length;
                fixed (char* sPtr = s)
                {
                    length = (UIntPtr)Encoding.GetBytes(sPtr, s.Length, pointer, maxByteLength);  // May throw
                }

                if (isNullTerminated)
                {
                    pointer[(int)length] = 0;
                }

                return new StringBuffer(pointer, length, isAllocated);
            }
            catch (EncoderFallbackException)
            {
                if (isAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)pointer);
                }

                throw;
            }
        }

        internal ref struct StringBuffer
        {
            private readonly bool _isAllocated;

            public StringBuffer(byte* pointer, UIntPtr length, bool isAllocated)
            {
                Debug.Assert(pointer != null);
                Debug.Assert((int)length >= 0);

                Pointer = pointer;
                Length = length;
                _isAllocated = isAllocated;
            }

            public byte* Pointer { get; }

            public UIntPtr Length { get; }

            public void Dispose()
            {
                if (_isAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)Pointer);
                }
            }
        }
    }
}
