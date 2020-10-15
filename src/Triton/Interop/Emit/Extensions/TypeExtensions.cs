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

namespace Triton.Interop.Emit.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="Type"/> class.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets the type's <see langword="public"/> fields.
        /// </summary>
        /// <param name="type">The type whose fields to get.</param>
        /// <param name="isStatic">
        /// <see langword="true"/> to get static fields, <see langword="false"/> to get instance fields.
        /// </param>
        /// <returns>The type's <see langword="public"/> fields.</returns>
        public static IEnumerable<FieldInfo> GetPublicFields(this Type type, bool isStatic) =>
            type.GetFields(Public | (isStatic ? Static | FlattenHierarchy : Instance))
                .Where(f => !f.IsSpecialName);

        /// <summary>
        /// Gets the type's <see langword="public"/> events.
        /// </summary>
        /// <param name="type">The type whose events to get.</param>
        /// <param name="isStatic">
        /// <see langword="true"/> to get static events, <see langword="false"/> to get instance events.
        /// </param>
        /// <returns>The type's <see langword="public"/> events.</returns>
        public static IEnumerable<EventInfo> GetPublicEvents(this Type type, bool isStatic) =>
            type.GetEvents(Public | (isStatic ? Static | FlattenHierarchy : Instance))
                .Where(f => !f.IsSpecialName);

        /// <summary>
        /// Gets the type's <see langword="public"/> properties.
        /// </summary>
        /// <param name="type">The type whose properties to get.</param>
        /// <param name="isStatic">
        /// <see langword="true"/> to get static properties, <see langword="false"/> to get instance properties.
        /// </param>
        /// <returns>The type's <see langword="public"/> properties.</returns>
        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type, bool isStatic) =>
            type.GetProperties(Public | (isStatic ? Static | FlattenHierarchy : Instance))
                .Where(p => !p.IsSpecialName && p.GetIndexParameters().Length == 0);

        /// <summary>
        /// Gets the type's <see langword="public"/> indexers.
        /// </summary>
        /// <param name="type">The type whose indexers to get.</param>
        /// <returns>The type's <see langword="public"/> indexers.</returns>
        public static IEnumerable<PropertyInfo> GetPublicIndexers(this Type type) =>
            type.GetCustomAttribute<DefaultMemberAttribute>()?.MemberName is string indexerName ?
                type.GetProperties(Public | Instance)
                    .Where(p => p.Name == indexerName && p.GetIndexParameters().Length != 0) :
                Array.Empty<PropertyInfo>();

        /// <summary>
        /// Gets the type's <see langword="public"/> methods.
        /// </summary>
        /// <param name="type">The type whose methods to get.</param>
        /// <param name="isStatic">
        /// <see langword="true"/> to get static methods, <see langword="false"/> to get instance methods.
        /// </param>
        /// <returns>The type's <see langword="public"/> methods.</returns>
        public static IEnumerable<MethodInfo> GetPublicMethods(this Type type, bool isStatic) =>
            type.GetMethods(Public | (isStatic ? Static | FlattenHierarchy : Instance))
                .Where(f => !f.IsSpecialName);

        /// <summary>
        /// Gets the type's <see langword="public"/> nested types.
        /// </summary>
        /// <param name="type">The type whose nested types to get.</param>
        /// <returns>The type's <see langword="public"/> nested types.</returns>
        public static IEnumerable<Type> GetPublicNestedTypes(this Type type) =>
            type.BaseType is null ?
                Array.Empty<Type>() :
                type.GetNestedTypes().Concat(type.BaseType.GetPublicNestedTypes()).Where(t => !t.IsSpecialName);

        /// <summary>
        /// Determines whether the type is a boolean type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is a boolean type; otherwise, <see langword="false"/>.</returns>
        public static bool IsBoolean(this Type type) => type == typeof(bool);

        /// <summary>
        /// Determines whether the type is a pointer type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is a pointer type; otherwise, <see langword="false"/>.</returns>
        public static bool IsPointer(this Type type) => type == typeof(IntPtr) || type == typeof(UIntPtr);

        /// <summary>
        /// Determines whether the type is an integer type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an integer type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsInteger(this Type type) => type.IsSignedInteger() || type.IsUnsignedInteger();

        /// <summary>
        /// Determines whether the type is a signed integer type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a signed integer type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSignedInteger(this Type type) =>
            type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);

        /// <summary>
        /// Determines whether the type is an unsigned integer type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an unsigned integer type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsUnsignedInteger(this Type type) =>
            type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

        /// <summary>
        /// Determines whether the type is a number type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is a number type; otherwise, <see langword="false"/>.</returns>
        public static bool IsNumber(this Type type) => type == typeof(float) || type == typeof(double);

        /// <summary>
        /// Determines whether the type is a string type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is a string type; otherwise, <see langword="false"/>.</returns>
        public static bool IsString(this Type type) => type == typeof(char) || type == typeof(string);

        /// <summary>
        /// Determines whether the type is a Lua object type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a Lua object type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsLuaObject(this Type type) => typeof(LuaObject).IsAssignableFrom(type);

        /// <summary>
        /// Determines whether the type is a CLR object type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR object type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrObject(this Type type) => type.IsClrClass() || type.IsClrStruct();

        /// <summary>
        /// Determines whether the type is a CLR class type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR class type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrClass(this Type type) =>
            (type.IsClass && !type.IsString() && !type.IsLuaObject()) || type.IsInterface;

        /// <summary>
        /// Determines whether the type is a CLR struct type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR struct type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrStruct(this Type type) => type.IsValueType && !type.IsPrimitive && !type.IsLuaValue();

        /// <summary>
        /// Determines whether the type is a Lua value type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a Lua value type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsLuaValue(this Type type) => type == typeof(LuaValue);
    }
}
