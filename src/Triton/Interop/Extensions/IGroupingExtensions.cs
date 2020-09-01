// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Triton.Interop.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="IGrouping{TKey, TElement}"/> interface.
    /// </summary>
    internal static class IGroupingExtensions
    {
        /// <summary>
        /// Deconstructs the grouping into a key and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key.</typeparam>
        /// <typeparam name="TElement">The type of element.</typeparam>
        /// <param name="grouping">The grouping.</param>
        /// <param name="key">The resulting key.</param>
        /// <param name="values">The resulting values.</param>
        public static void Deconstruct<TKey, TElement>(
            this IGrouping<TKey, TElement> grouping, out TKey key, out IEnumerable<TElement> values)
        {
            key = grouping.Key;
            values = grouping;
        }
    }
}
