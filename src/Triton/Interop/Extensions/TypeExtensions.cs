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

namespace Triton.Interop.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="Type"/> class.
    /// </summary>
    internal static class TypeExtensions
    {
        private static readonly HashSet<Type> _lightUserdataTypes = new HashSet<Type>
        {
            typeof(IntPtr), typeof(UIntPtr)
        };

        private static readonly HashSet<Type> _integralTypes = new HashSet<Type>
        {
            typeof(byte), typeof(ushort), typeof(uint), typeof(ulong),
            typeof(sbyte), typeof(short), typeof(int), typeof(long)
        };

        private static readonly HashSet<Type> _signedIntegralTypes = new HashSet<Type>
        {
            typeof(sbyte), typeof(short), typeof(int), typeof(long)
        };

        private static readonly HashSet<Type> _unsignedIntegralTypes = new HashSet<Type>
        {
            typeof(byte), typeof(ushort), typeof(uint), typeof(ulong)
        };

        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(float), typeof(double)
        };

        private static readonly HashSet<Type> _stringTypes = new HashSet<Type>
        {
            typeof(string), typeof(char)
        };

        /// <summary>
        /// Gets the type's public static fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type's public static fields.</returns>
        public static IEnumerable<FieldInfo> GetPublicStaticFields(this Type type) =>
            type.GetFields(Public | Static | FlattenHierarchy)
                .Where(f => !f.IsSpecialName);

        /// <summary>
        /// Gets the type's public instance fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type's public instance fields.</returns>
        public static IEnumerable<FieldInfo> GetPublicInstanceFields(this Type type) =>
            type.GetFields(Public | Instance)
                .Where(f => !f.IsSpecialName);

        /// <summary>
        /// Gets the type's public static properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type's public static properties.</returns>
        public static IEnumerable<PropertyInfo> GetPublicStaticProperties(this Type type) =>
            type.GetProperties(Public | Static | FlattenHierarchy)
                .Where(p => !p.IsSpecialName);

        /// <summary>
        /// Gets the type's public instance properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type's public instance properties.</returns>
        public static IEnumerable<PropertyInfo> GetPublicInstanceProperties(this Type type) =>
            type.GetProperties(Public | Instance)
                .Where(p => !p.IsSpecialName);

        /// <summary>
        /// Gets the type's public nested types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type's public nested types.</returns>
        public static IEnumerable<Type> GetPublicNestedTypes(this Type type) =>
            type.BaseType is null
                ? Array.Empty<Type>()
                : type.GetNestedTypes().Concat(type.BaseType.GetPublicNestedTypes()).Where(t => !t.IsSpecialName);

        /// <summary>
        /// Determines whether a type is a light userdata type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a light userdata type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsLightUserdata(this Type type) => type.IsPointer || _lightUserdataTypes.Contains(type);

        /// <summary>
        /// Determines whether a type is an integral type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an integral type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsInteger(this Type type) => type.IsEnum || _integralTypes.Contains(type);

        /// <summary>
        /// Determines whether a type is a signed integral type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a signed integral type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSignedInteger(this Type type) =>
            _signedIntegralTypes.Contains(type.IsEnum ? type.GetEnumUnderlyingType() : type);

        /// <summary>
        /// Determines whether a type is an unsigned integral type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an unsigned integral type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsUnsignedInteger(this Type type) =>
            _unsignedIntegralTypes.Contains(type.IsEnum ? type.GetEnumUnderlyingType() : type);

        /// <summary>
        /// Determines whether a type is a numeric type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is a numeric type; otherwise, <see langword="false"/>.</returns>
        public static bool IsNumber(this Type type) => _numericTypes.Contains(type);

        /// <summary>
        /// Determines whether a type is a string type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is a string type; otherwise, <see langword="false"/>.</returns>
        public static bool IsString(this Type type) => _stringTypes.Contains(type);

        /// <summary>
        /// Determines whether a type is a Lua object type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a Lua object type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsLuaObject(this Type type) => typeof(LuaObject).IsAssignableFrom(type);

        /// <summary>
        /// Determines whether a type is a CLR object type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR object type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrObject(this Type type) => type.IsClrClass() || type.IsClrStruct();

        /// <summary>
        /// Determines whether a type is a CLR class type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR class type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrClass(this Type type) =>
            (type.IsClass && type != typeof(string) && !type.IsLuaObject() && !type.IsPointer) || type.IsInterface;

        /// <summary>
        /// Determines whether a type is a CLR struct type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR struct type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrStruct(this Type type) =>
            type.IsValueType && !type.IsPrimitive;

        /// <summary>
        /// Simplifies the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The simplified type.</returns>
        public static Type Simplify(this Type type) =>
            type switch
            {
                _ when type.IsPointer => typeof(IntPtr),
                _ when type.IsEnum    => type.GetEnumUnderlyingType(),
                _                     => type
            };
    }
}
