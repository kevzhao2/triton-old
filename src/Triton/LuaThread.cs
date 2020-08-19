﻿// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Triton
{
    /// <summary>
    /// Represents a Lua thread.
    /// </summary>
    public class LuaThread : LuaObject
    {
        internal LuaThread(IntPtr state, LuaEnvironment environment, int @ref) : base(state, environment, @ref)
        {
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string ToString() => $"thread {_ref}";
    }
}
