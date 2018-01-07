using System;
using System.Runtime.InteropServices;

namespace Triton.Interop {
    /// <summary>
    /// Specifies a Lua debug structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct LuaDebug {
        public int @event;
        public IntPtr name;
        public IntPtr nameWhat;
        public IntPtr what;
        public IntPtr source;
        public int currentLine;
        public int lineDefined;
        public int lastLineDefined;
        public byte numUpvalues;
        public byte numParams;
        public byte isVararg;
        public byte isTailcall;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] shortSource;

        public IntPtr callInfo;
    }
}
