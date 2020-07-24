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
using System.Diagnostics.CodeAnalysis;

namespace Triton.Interop
{
    /// <summary>
    /// Provides context for a generated metamethod.
    /// </summary>
    internal sealed class MetamethodContext
    {
        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Implicitly used")]
        private readonly LuaEnvironment _environment;
        private readonly Dictionary<string, int> _memberNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetamethodContext"/> class with the specified Lua
        /// <paramref name="environment"/> and <paramref name="memberNames"/>.
        /// </summary>
        /// <param name="environment">The Lua environment.</param>
        /// <param name="memberNames">The member names.</param>
        internal MetamethodContext(LuaEnvironment environment, IReadOnlyList<string> memberNames)
        {
            _environment = environment;

            _memberNames = new Dictionary<string, int>();
            for (var i = 0; i < memberNames.Count; ++i)
            {
                _memberNames.Add(memberNames[i], i);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unread private members", Justification = "Implicitly used")]
        private int MatchMemberName(string memberName) => _memberNames.TryGetValue(memberName, out var i) ? i : -1;
    }
}
