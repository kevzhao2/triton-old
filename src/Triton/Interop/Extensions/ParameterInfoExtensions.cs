// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Triton.Interop.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="ParameterInfo"/> class.
    /// </summary>
    internal static class ParameterInfoExtensions
    {
        /// <summary>
        /// Determines whether a parameter is a <see langword="params"/>.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// <see langword="true"/> if the parameter is a <see langword="params"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsParams(this ParameterInfo parameter) =>
            parameter.GetCustomAttribute<ParamArrayAttribute>() is { };
    }
}
