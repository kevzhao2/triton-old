// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Triton.Interop.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="MethodBase"/> class.
    /// </summary>
    internal static class MethodBaseExtensions
    {
        /// <summary>
        /// Gets the method's argument count bounds.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The argument count bounds.</returns>
        public static (int minArgs, int maxArgs) GetArgCountBounds(this MethodBase method)
        {
            var minArgs = 0;
            var maxArgs = 0;

            foreach (var parameter in method.GetParameters())
            {
                // If the parameter is a params, then the number of arguments is unbounded. We can also break since it
                // is the last parameter.

                if (parameter.IsParams())
                {
                    maxArgs = int.MaxValue;
                    break;
                }

                ++maxArgs;
                if (!parameter.IsOptional)
                {
                    ++minArgs;
                }
            }

            return (minArgs, maxArgs);
        }

        /// <summary>
        /// Gets the return type of the method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The return type.</returns>
        public static Type GetReturnType(this MethodBase method) =>
            method is MethodInfo { ReturnType: var returnType } ? returnType : method.DeclaringType!;
    }
}
