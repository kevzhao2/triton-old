// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

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
        public void Deconstruct(out LuaValue result, out LuaValue result2, out LuaValue result3, out LuaResults rest)
        {
            _environment.LoadValue(_state, ++_index, out result);
            _environment.LoadValue(_state, ++_index, out result2);
            _environment.LoadValue(_state, ++_index, out result3);
            rest = this;
        }
    }
}
