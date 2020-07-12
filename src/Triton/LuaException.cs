﻿// Copyright (c) 2020 Kevin Zhao
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
using System.Runtime.Serialization;

namespace Triton
{
    /// <summary>
    /// The exception that is thrown when an error from within Lua occurs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class LuaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaException"/> class.
        /// </summary>
        public LuaException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaException"/> class with the specified
        /// <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        public LuaException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaException"/> class with the specified
        /// <paramref name="message"/> and <paramref name="inner"/> exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public LuaException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaException"/> class with the specified serialization
        /// <paramref name="info"/> and <paramref name="context"/>.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The serialization context.</param>
        protected LuaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
