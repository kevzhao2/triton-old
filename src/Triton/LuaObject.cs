// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;

namespace Triton
{
    /// <summary>
    /// Provides the base class for a Lua object.
    /// </summary>
    public abstract class LuaObject
    {
        // These fields are internal to centralize logic inside of the `LuaObjectManager` class.

        internal readonly IntPtr _state;
        internal readonly LuaEnvironment _environment;
        internal readonly int _ref;

        private protected LuaObject(IntPtr state, LuaEnvironment environment, int @ref)
        {
            _state = state;
            _environment = environment;
            _ref = @ref;
        }
    }
}
