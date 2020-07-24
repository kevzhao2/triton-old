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
using System.Linq;
using System.Reflection;
using static System.Reflection.BindingFlags;

namespace Triton.Interop
{
    /// <summary>
    /// Provides extensions for the <see cref="Type"/> class.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets all of the publicly accessible <see langword="const"/> fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All of the publicly accessible <see langword="const"/> fields.</returns>
        public static IEnumerable<FieldInfo> GetAllConstFields(this Type type) =>
            type.GetFields(Public | Static | FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsSpecialName);

        /// <summary>
        /// Gets all of the publicly accessible <see langword="static"/> fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All of the publicly accessible <see langword="static"/> fields.</returns>
        public static IEnumerable<FieldInfo> GetAllStaticFields(this Type type) =>
            type.GetFields(Public | Static | FlattenHierarchy)
                .Where(f => !f.IsLiteral && !f.IsSpecialName);

        /// <summary>
        /// Gets all of the publicly accessible <see langword="static"/> properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All of the publicly accessible <see langword="static"/> properties.</returns>
        public static IEnumerable<PropertyInfo> GetAllStaticProperties(this Type type) =>
            type.GetProperties(Public | Static | FlattenHierarchy)
                .Where(p => !p.IsSpecialName);

        /// <summary>
        /// Gets all of the publicly accessible nested types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>All of the publicly accessible nested types.</returns>
        public static IEnumerable<Type> GetAllNestedTypes(this Type type) =>
            type.BaseType is null
                ? Enumerable.Empty<Type>()
                : type.GetNestedTypes().Concat(GetAllNestedTypes(type.BaseType)).Where(t => !t.IsSpecialName);
    }
}
