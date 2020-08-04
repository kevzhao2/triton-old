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
using System.Diagnostics.CodeAnalysis;

namespace Triton.Interop
{
    /// <summary>
    /// Acts as a proxy for a CLR type.
    /// </summary>
    internal sealed class ProxyClrType
    {
        internal ProxyClrType(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public Type Type { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ProxyClrType { Type: var type } && Type.Equals(type);

        /// <inheritdoc/>
        public override int GetHashCode() => Type.GetHashCode();

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string ToString() => Type.ToString();
    }
}
