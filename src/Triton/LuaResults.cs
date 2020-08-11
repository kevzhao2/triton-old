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

namespace Triton
{
    /// <summary>
    /// Represents the results of a Lua call. This structure is intended to be ephemeral.
    /// </summary>
    public ref struct LuaResults
    {
        private readonly IntPtr _state;
        private readonly LuaEnvironment _environment;

        private int _index;

        internal LuaResults(IntPtr state, LuaEnvironment environment)
        {
            _state = state;
            _environment = environment;
            _index = 0;
        }

        /// <summary>
        /// Deconstructs a single result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="rest">The rest of the results.</param>
        public void Deconstruct(out LuaValue result, out LuaResults rest)
        {
            _environment.LoadValue(_state, ++_index, out result);
            rest = this;
        }

        /// <summary>
        /// Deconstructs two results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="rest">The rest of the results.</param>
        public void Deconstruct(out LuaValue result, out LuaValue result2, out LuaResults rest)
        {
            _environment.LoadValue(_state, ++_index, out result);
            _environment.LoadValue(_state, ++_index, out result2);
            rest = this;
        }

        /// <summary>
        /// Deconstructs three results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="rest">The rest of the results.</param>
        public void Deconstruct(
            out LuaValue result, out LuaValue result2, out LuaValue result3, out LuaResults rest)
        {
            _environment.LoadValue(_state, ++_index, out result);
            _environment.LoadValue(_state, ++_index, out result2);
            _environment.LoadValue(_state, ++_index, out result3);
            rest = this;
        }
    }
}
