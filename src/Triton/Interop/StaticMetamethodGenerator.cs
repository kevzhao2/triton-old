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

namespace Triton.Interop
{
    /// <summary>
    /// Provides the base class for a static metamethod generator for CLR entities.
    /// </summary>
    internal unsafe abstract class StaticMetamethodGenerator : IMetavalueGenerator
    {
        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a pointer to the metamethod.
        /// </summary>
        /// <value>A pointer to the metamethod.</value>
        protected abstract delegate* unmanaged[Cdecl]<lua_State*, int> Metamethod { get; }

        /// <inheritdoc/>
        public bool IsApplicable(object entity, bool isTypes) => true;

        /// <inheritdoc/>
        public void Push(lua_State* state, object entity, bool isTypes) => lua_pushcfunction(state, Metamethod);
    }
}
