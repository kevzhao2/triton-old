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
using System.Diagnostics.CodeAnalysis;

namespace Triton
{
    /// <summary>
    /// Provides helper methods for throwing exceptions.
    /// </summary>
    internal static class ThrowHelper
    {
        /// <summary>
        /// Throws an <see cref="ArgumentException"/> with the specified parameter name and message.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="message">The message.</param>
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName, string message) =>
            throw new ArgumentException(message, paramName);

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> with the specified parameter name.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName) =>
            throw new ArgumentNullException(paramName);

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> with the specified parameter name and message.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="message">The message.</param>
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message) =>
            throw new ArgumentOutOfRangeException(paramName, message);

        /// <summary>
        /// Throws an <see cref="InvalidCastException"/>.
        /// </summary>
        [DoesNotReturn]
        public static void ThrowInvalidCastException() =>
            throw new InvalidCastException();

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        [DoesNotReturn]
        public static void ThrowInvalidOperationException(string message) =>
            throw new InvalidOperationException(message);

        /// <summary>
        /// Throws a <see cref="LuaLoadException"/> with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        [DoesNotReturn]
        public static void ThrowLuaLoadException(string message) =>
            throw new LuaLoadException(message);

        /// <summary>
        /// Throws a <see cref="LuaRuntimeException"/> with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        [DoesNotReturn]
        public static void ThrowLuaRuntimeException(string message) =>
            throw new LuaRuntimeException(message);

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> with the specified object name.
        /// </summary>
        /// <param name="objectName">The object name.</param>
        [DoesNotReturn]
        public static void ThrowObjectDisposedException(string objectName) =>
            throw new ObjectDisposedException(objectName);
    }
}
