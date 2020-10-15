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
    /// Defines a metamethod generator for CLR entities.
    /// </summary>
    internal unsafe interface IMetamethodGenerator
    {
        /// <summary>
        /// Gets the metamethod's name.
        /// </summary>
        /// <value>The metamethod's name.</value>
        public string Name { get; }

        /// <summary>
        /// Determines whether the generator is applicable for the given CLR entity.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <param name="isTypes"><see langword="true"/> if the entity is types; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the generator is applicable; otherwise, <see langword="false"/>.</returns>
        public bool IsApplicable(object entity, bool isTypes);

        /// <summary>
        /// Pushes the metamethod for the given CLR entity onto the stack. Requires that the generator is applicable.
        /// </summary>
        /// <param name="state">The Lua state to push the metamethod onto.</param>
        /// <param name="entity">The entity whose metamethod to push.</param>
        /// <param name="isTypes"><see langword="true"/> if the entity is types; otherwise, <see langword="false"/>.</param>
        public void PushMetamethod(lua_State* state, object entity, bool isTypes);
    }
}
