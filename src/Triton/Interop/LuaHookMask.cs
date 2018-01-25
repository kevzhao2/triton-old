using System;

namespace Triton.Interop {
    /// <summary>
    /// Specifies the mask for a <see cref="LuaHook"/>.
    /// </summary>
    [Flags]
    internal enum LuaHookMask {
        Call = 1 << 0,
        Return = 1 << 1,
        Line = 1 << 2,
        Count = 1 << 3
    }
}
