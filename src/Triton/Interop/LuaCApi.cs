using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

using lua_State = System.IntPtr;

namespace Triton.Interop
{
    /// <summary>
    /// Exposes the Lua C API in managed code.
    /// </summary>
    [SuppressMessage("Style", "IDE1005:Naming Styles", Justification = "Matching declarations")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching declarations")]
    [SuppressUnmanagedCodeSecurity]
    internal static class LuaCApi
    {
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_State luaL_newstate();

    }
}
