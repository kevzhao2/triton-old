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

namespace Triton.Binding {
    /// <summary>
    /// Contains extension methods.
    /// </summary>
    internal static class Extensions {
        private static readonly Dictionary<Type, TypeBindingInfo> TypeBindingInfoCache = new Dictionary<Type, TypeBindingInfo>();

        /// <summary>
        /// Gets the <see cref="TypeBindingInfo"/> for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="TypeBindingInfo"/>.</returns>
        public static TypeBindingInfo GetBindingInfo(this Type type) {
            lock (TypeBindingInfoCache) {
                if (!TypeBindingInfoCache.TryGetValue(type, out var info)) {
                    info = TypeBindingInfo.Construct(type);
                    TypeBindingInfoCache[type] = info;
                }
                return info;
            }
        }

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
    }
}
