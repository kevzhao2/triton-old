// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Triton
{
    /// <summary>
    /// The exception that is thrown when a Lua error occurs at runtime.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class LuaRuntimeException : LuaException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaRuntimeException"/> class.
        /// </summary>
        public LuaRuntimeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaRuntimeException"/> class with the specified
        /// <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        public LuaRuntimeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaRuntimeException"/> class with the specified
        /// <paramref name="message"/> and <paramref name="inner"/> exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public LuaRuntimeException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaRuntimeException"/> class with the specified
        /// serialization <paramref name="info"/> and <paramref name="context"/>.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The serialization context.</param>
        protected LuaRuntimeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
