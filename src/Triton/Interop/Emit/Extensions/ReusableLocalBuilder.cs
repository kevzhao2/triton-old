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
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Triton.Interop.Emit.Extensions
{
    /// <summary>
    /// Represents a reusable local variable within a method or constructor.
    /// </summary>
    internal sealed class ReusableLocalBuilder : IDisposable
    {
        private static readonly ConditionalWeakTable<ILGenerator, Dictionary<Type, Stack<ReusableLocalBuilder>>>
            _freeLocalsByType = new();

        private readonly ILGenerator _ilg;
        private readonly LocalBuilder _localBuilder;

        private ReusableLocalBuilder(ILGenerator ilg, LocalBuilder localBuilder)
        {
            _ilg = ilg;
            _localBuilder = localBuilder;
        }

        /// <summary>
        /// Allocates a reusable local variable from the given IL generator of the specified type.
        /// </summary>
        /// <param name="ilg">The IL generator to allocate the local variable from.</param>
        /// <param name="type">The type of the local variable.</param>
        /// <returns>The reusable local variable.</returns>
        public static ReusableLocalBuilder Allocate(ILGenerator ilg, Type type) =>
            _freeLocalsByType.GetOrCreateValue(ilg).TryGetValue(type, out var freeLocals) && freeLocals.Count > 0 ?
                freeLocals.Pop() :
                new ReusableLocalBuilder(ilg, ilg.DeclareLocal(type));

        /// <summary>
        /// Frees the reusable local variable, allowing it to be reused.
        /// </summary>
        public void Free() =>
            _freeLocalsByType.GetOrCreateValue(_ilg).GetOrCreateValue(_localBuilder.LocalType).Push(this);

        void IDisposable.Dispose() => Free();

        /// <summary>
        /// Converts the reusable local variable into a local variable.
        /// </summary>
        /// <param name="local">The reusable local variable.</param>
        public static implicit operator LocalBuilder(ReusableLocalBuilder local) => local._localBuilder;
    }
}
