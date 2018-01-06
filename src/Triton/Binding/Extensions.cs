// Copyright (c) 2018 Kevin Zhao
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
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Triton.Binding {
    /// <summary>
    /// Contains extension methods relevant to object binding.
    /// </summary>
    internal static class Extensions {
        /// <summary>
        /// Gets the value associated with the given key in a dictionary or its default value if the key doesn't exist.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value, or its default.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) {
            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }
        
        /// <summary>
        /// Tries to coerce the object into the given type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="type">The type.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if the object was successfully coerced; <c>false</c> otherwise.</returns>
        public static bool TryCoerce(this object obj, Type type, out object result) {
            result = obj;
            if (result == null) {
#if NETCORE
                return type.GetTypeInfo().IsClass || Nullable.GetUnderlyingType(type) != null;
#else
                return type.IsClass || Nullable.GetUnderlyingType(type) != null;
#endif
            }

            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsByRef) {
                type = type.GetElementType();
            }

            if (result is long l) {
                var typeCode = Type.GetTypeCode(type);
                switch (typeCode) {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    try {
                        result = Convert.ChangeType(result, typeCode, CultureInfo.CurrentCulture);
                        return true;
                    } catch (OverflowException) {
                        return false;
                    }

                case TypeCode.UInt64:
                    // UInt64 is a special case since we want to avoid OverflowExceptions.
                    result = (ulong)l;
                    return true;
                }
            } else if (result is double d) {
                if (type == typeof(float)) {
                    result = (float)d;
                    return true;
                } else if (type == typeof(decimal)) {
                    result = (decimal)d;
                    return true;
                }
            } else if (result is string s) {
                if (type == typeof(char)) {
                    result = s.FirstOrDefault();
                    return s.Length == 1;
                }
            }

#if NETCORE
            return type.GetTypeInfo().IsInstanceOfType(result);
#else
            return type.IsInstanceOfType(result);
#endif
        }
    }
}
