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

using System.Collections.Generic;

namespace Triton.Interop.Utils
{
    /// <summary>
    /// Matches a string to an index.
    /// </summary>
    internal sealed class StringMatcher
    {
        private readonly Dictionary<string, int> _indices = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringMatcher"/> class with the specified
        /// <paramref name="strings"/>.
        /// </summary>
        /// <param name="strings">The strings.</param>
        public StringMatcher(IReadOnlyList<string> strings)
        {
            for (var i = 0; i < strings.Count; ++i)
            {
                _indices.Add(strings[i], i);
            }
        }

        /// <summary>
        /// Matches a string <paramref name="s"/>. Returns the string's index, or <c>-1</c> if it does not exist.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The string's index, or <c>-1</c> if it does not exist.</returns>
        public int Match(string s) => _indices.TryGetValue(s, out var index) ? index : -1;
    }
}
