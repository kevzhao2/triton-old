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
    internal static class TypeExtensions
    {
        public static IEnumerable<FieldInfo> GetPublicStaticFields(this Type type) =>
            type.GetFields(Public | Static | FlattenHierarchy)
                .Where(f => !f.IsSpecialName);

        public static IEnumerable<PropertyInfo> GetPublicStaticProperties(this Type type) =>
            type.GetProperties(Public | Static | FlattenHierarchy)
                .Where(p => !p.IsSpecialName);

        public static IEnumerable<Type> GetPublicNestedTypes(this Type type) =>
            type.BaseType is null
                ? Enumerable.Empty<Type>()
                : type.GetNestedTypes().Concat(GetPublicNestedTypes(type.BaseType)).Where(t => !t.IsSpecialName);

        public static Type Simplify(this Type type) => true switch
        {
            _ when type.IsEnum => type.GetEnumUnderlyingType(),
            _ when type.IsPointer => typeof(IntPtr),
            _ => type
        };
    }
}
