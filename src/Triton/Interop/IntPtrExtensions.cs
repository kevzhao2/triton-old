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

namespace Triton.Interop
{
    /// <summary>
    /// Provides extensions for the <see cref="IntPtr"/> structure.
    /// </summary>
    internal static class IntPtrExtensions
    {
        /// <summary>
        /// Returns a marked pointer.
        /// </summary>
        /// <param name="ptr">The pointer.</param>
        /// <returns>The marked pointer.</returns>
        public static IntPtr Marked(this IntPtr ptr) => new IntPtr(ptr.ToInt64() | 1);

        /// <summary>
        /// Returns an unmarked pointer.
        /// </summary>
        /// <param name="ptr">The pointer.</param>
        /// <returns>The unmarked pointer.</returns>
        public static IntPtr Unmarked(this IntPtr ptr) => new IntPtr(ptr.ToInt64() & ~1);

        /// <summary>
        /// Returns a value indicating whether the pointer is marked.
        /// </summary>
        /// <param name="ptr">The pointer.</param>
        /// <returns><see langword="true"/> if the pointer is marked; otherwise, <see langword="false"/>.</returns>
        public static bool IsMarked(this IntPtr ptr) => (ptr.ToInt64() & 1) != 0;
    }
}
