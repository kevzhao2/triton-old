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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Triton.Interop
{
    /// <summary>
    /// Acts as a proxy for generic CLR types with the same names.
    /// </summary>
    internal sealed class ProxyGenericClrTypes
    {
        internal ProxyGenericClrTypes(Type[] types)
        {
            Debug.Assert(types.All(t => t is { }));
            Debug.Assert(types.Count(t => !t.IsGenericTypeDefinition) <= 1);

            Types = types;
        }

        /// <summary>
        /// Gets the generic CLR types.
        /// </summary>
        public Type[] Types { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is ProxyGenericClrTypes { Types: var types } && Types.SequenceEqual(types);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            ((IStructuralEquatable)Types).GetHashCode(EqualityComparer<Type>.Default);

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string ToString() => string.Join(", ", (IEnumerable<Type>)Types);
    }
}
