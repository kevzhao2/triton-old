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

using static Triton.Lua;

namespace Triton
{
    /// <summary>
    /// Represents the results of a Lua call. This structure is ephemeral.
    /// </summary>
    public unsafe readonly ref struct LuaResults
    {
        private readonly lua_State* _state;
        private readonly int _index;

        internal LuaResults(lua_State* state, int index = 0)
        {
            _state = state;
            _index = index;
        }

        /// <summary>
        /// Deconstructs a single result from the Lua call.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="rest">The rest of the results.</param>
        public void Deconstruct(out LuaValue result, out LuaResults rest)
        {
            var index = _index;
            LuaValue.FromLua(_state, ++index, lua_type(_state, index), out result);

            rest = new(_state, index);
        }

        /// <summary>
        /// Deconstructs two results from the Lua call.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="rest">The rest of the results.</param>
        public void Deconstruct(out LuaValue result, out LuaValue result2, out LuaResults rest)
        {
            var index = _index;
            LuaValue.FromLua(_state, ++index, lua_type(_state, index), out result);
            LuaValue.FromLua(_state, ++index, lua_type(_state, index), out result2);

            rest = new(_state, index);
        }

        /// <summary>
        /// Deconstructs three results from the Lua call.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="rest">The rest of the results.</param>
        public void Deconstruct(out LuaValue result, out LuaValue result2, out LuaValue result3, out LuaResults rest)
        {
            var index = _index;
            LuaValue.FromLua(_state, ++index, lua_type(_state, index), out result);
            LuaValue.FromLua(_state, ++index, lua_type(_state, index), out result2);
            LuaValue.FromLua(_state, ++index, lua_type(_state, index), out result3);

            rest = new(_state, index);
        }
    }
}
