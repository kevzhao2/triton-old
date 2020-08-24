// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

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
        /// <summary>
        /// Gets the type's <see langword="public"/> fields.
        /// </summary>
        /// <param name="type">The type.</param>
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
        /// <param name="type">The type.</param>
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
        /// <param name="type">The type.</param>
        /// <param name="isStatic">
        /// <see langword="true"/> to get static properties, <see langword="false"/> to get instance properties.
        /// </param>
        /// <returns>The type's <see langword="public"/> properties.</returns>
        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type, bool isStatic) =>
            type.GetProperties(Public | (isStatic ? Static | FlattenHierarchy : Instance))
                .Where(p => !p.IsSpecialName && p.GetIndexParameters().Length == 0);

        public static IEnumerable<PropertyInfo> GetPublicIndexers(this Type type)
        {
            var indexerName = type.GetCustomAttribute<DefaultMemberAttribute>()?.MemberName;
            return indexerName is null
                ? Array.Empty<PropertyInfo>()
                : type.GetProperties(Public | Instance)
                      .Where(p => p.Name == indexerName);
        }

        /// <summary>
        /// Gets the type's <see langword="public"/> methods.
        /// </summary>
        /// <param name="type">The type.</param>
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
        /// <param name="type">The type.</param>
        /// <returns>The type's <see langword="public"/> nested types.</returns>
        public static IEnumerable<Type> GetPublicNestedTypes(this Type type) =>
            type.BaseType is null
                ? Array.Empty<Type>()
                : type.GetNestedTypes().Concat(type.BaseType.GetPublicNestedTypes()).Where(t => !t.IsSpecialName);

        /// <summary>
        /// Determines whether a type is a boolean type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is a boolean type; otherwise, <see langword="false"/>.</returns>
        public static bool IsBoolean(this Type type) =>
            type == typeof(bool);

        /// <summary>
        /// Determines whether a type is a light userdata type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a light userdata type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsLightUserdata(this Type type) =>
            type.IsPointer || type == typeof(IntPtr) || type == typeof(UIntPtr);

        /// <summary>
        /// Determines whether a type is an integral type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an integral type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsInteger(this Type type) =>
            type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
            type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

        /// <summary>
        /// Determines whether a type is a signed integral type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a signed integral type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSignedInteger(this Type type) =>
            type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long);

        /// <summary>
        /// Determines whether a type is an unsigned integral type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is an unsigned integral type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsUnsignedInteger(this Type type) =>
            type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

        /// <summary>
        /// Determines whether a type is a numeric type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is a numeric type; otherwise, <see langword="false"/>.</returns>
        public static bool IsNumber(this Type type) =>
            type == typeof(float) || type == typeof(double);

        /// <summary>
        /// Determines whether a type is a string type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> if the type is a string type; otherwise, <see langword="false"/>.</returns>
        public static bool IsString(this Type type) =>
            type == typeof(string) || type == typeof(char);

        /// <summary>
        /// Determines whether a type is a Lua object type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a Lua object type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsLuaObject(this Type type) =>
            typeof(LuaObject).IsAssignableFrom(type);

        /// <summary>
        /// Determines whether a type is a CLR object type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a CLR object type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsClrObject(this Type type) =>
            type.IsClrClass() || type.IsClrStruct();

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
            type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(LuaValue);

        /// <summary>
        /// Simplifies the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The simplified type.</returns>
        public static Type Simplify(this Type type) =>
            type switch
            {
                _ when type.IsPointer => typeof(IntPtr),
                _ when type.IsEnum => type.GetEnumUnderlyingType(),
                _ => type
            };
    }
}
