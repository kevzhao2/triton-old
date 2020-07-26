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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Triton
{
    /// <summary>
    /// Provides access to native methods.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Style", "IDE1005:Naming Styles", Justification = "Matching declarations")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching declarations")]
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        public const int LUAI_MAXSTACK = 1000000;

        public const int LUA_REGISTRYINDEX = -LUAI_MAXSTACK - 1000;

        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        public const int LUA_REFNIL = -1;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_CFunction(IntPtr L);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_KFunction(IntPtr L, LuaStatus status, IntPtr ctx);

        // =============================================================================================================
        // lua.h functions
        //

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_checkstack(IntPtr L, int n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(IntPtr L, int narr, int nrec);

        public static unsafe LuaType lua_getfield(IntPtr L, int index, string k)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(k.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(k, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return lua_getfield(L, index, (IntPtr)buffer);
            }
        }

        public static unsafe LuaType lua_getglobal(IntPtr L, string name)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(name.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(name, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return lua_getglobal(L, (IntPtr)buffer);
            }
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_geti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_gettable(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isinteger(IntPtr L, int index);

        public static void lua_newtable(IntPtr L) => lua_createtable(L, 0, 0);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newthread(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newuserdatauv(IntPtr L, UIntPtr size, int nuvalue);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_next(IntPtr L, int index);

        public static LuaStatus lua_pcall(IntPtr L, int nargs, int nresults, int msgh) =>
            lua_pcallk(L, nargs, nresults, msgh, IntPtr.Zero, null);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_pcallk(
            IntPtr L, int nargs, int nresults, int msgh, IntPtr ctx, lua_KFunction? k);

        public static void lua_pop(IntPtr L, int n)
        {
            if (n != 0)
            {
                lua_settop(L, -n - 1);
            }
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushboolean(IntPtr L, bool b);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(IntPtr L, lua_CFunction fn, int n);

        public static void lua_pushcfunction(IntPtr L, lua_CFunction fn) => lua_pushcclosure(L, fn, 0);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(IntPtr L, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(IntPtr L, double n);

        public static unsafe IntPtr lua_pushstring(IntPtr L, string s)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(s.Length);

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(s, span);

            fixed (byte* buffer = span)
            {
                return lua_pushlstring(L, (IntPtr)buffer, (UIntPtr)length);
            }
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgeti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgetp(IntPtr L, int index, IntPtr p);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawsetp(IntPtr L, int index, IntPtr p);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_remove(IntPtr L, int index);

        public static unsafe LuaStatus lua_resume(IntPtr L, IntPtr from, int nargs, out int nresults)
        {
            fixed (int* nresultsPtr = &nresults)
            {
                return lua_resume(L, from, nargs, (IntPtr)nresultsPtr);
            }
        }

        public static unsafe void lua_setfield(IntPtr L, int index, string k)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(k.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(k, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                lua_setfield(L, index, (IntPtr)buffer);
            }
        }

        public static unsafe void lua_setglobal(IntPtr L, string name)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(name.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(name, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                lua_setglobal(L, (IntPtr)buffer);
            }
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_seti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setmetatable(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settable(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_status(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_toboolean(IntPtr L, int index);

        public static long lua_tointeger(IntPtr L, int index) => lua_tointegerx(L, index, IntPtr.Zero);

        public static double lua_tonumber(IntPtr L, int index) => lua_tonumberx(L, index, IntPtr.Zero);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_topointer(IntPtr L, int index);

        public static unsafe string lua_tostring(IntPtr L, int index)
        {
            UIntPtr length;
            var ptr = lua_tolstring(L, index, (IntPtr)(&length));

            return Encoding.UTF8.GetString((byte*)ptr, (int)length);
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_touserdata(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_type(IntPtr L, int index);

        public static int lua_upvalueindex(int i) => LUA_REGISTRYINDEX - i;

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaType lua_getfield(IntPtr L, int index, IntPtr k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaType lua_getglobal(IntPtr L, IntPtr name);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr lua_pushlstring(IntPtr L, IntPtr s, UIntPtr len);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaStatus lua_resume(IntPtr l, IntPtr from, int nargs, IntPtr nresults);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern void lua_setfield(IntPtr L, int index, IntPtr k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern void lua_setglobal(IntPtr L, IntPtr name);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern long lua_tointegerx(IntPtr L, int index, IntPtr isnum);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr lua_tolstring(IntPtr L, int index, IntPtr len);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern double lua_tonumberx(IntPtr L, int index, IntPtr isnum);

        // =============================================================================================================
        // lauxlib.h functions
        //

        public static unsafe int luaL_error(IntPtr L, string fmt)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(fmt.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(fmt, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return luaL_error(L, (IntPtr)buffer);
            }
        }

        public static unsafe LuaStatus luaL_loadstring(IntPtr L, string s)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(s.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(s, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return luaL_loadstring(L, (IntPtr)buffer);
            }
        }

        public static unsafe bool luaL_newmetatable(IntPtr L, string tname)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(tname.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(tname, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return luaL_newmetatable(L, (IntPtr)buffer);
            }
        }

        public static unsafe void luaL_setmetatable(IntPtr L, string tname)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(tname.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(tname, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                luaL_setmetatable(L, (IntPtr)buffer);
            }
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_newstate();

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(IntPtr L, int t);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(IntPtr L, int t, int @ref);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern int luaL_error(IntPtr L, IntPtr fmt);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool luaL_newmetatable(IntPtr L, IntPtr tname);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern void luaL_setmetatable(IntPtr L, IntPtr tname);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaStatus luaL_loadstring(IntPtr L, IntPtr s);

        public enum LuaStatus
        {
            Ok = 0,
            Yield = 1,
            ErrRun = 2,
            ErrSyntax = 3,
            ErrMem = 4,
            ErrErr = 5
        }

        public enum LuaType
        {
            None = -1,
            Nil = 0,
            Boolean = 1,
            LightUserdata = 2,
            Number = 3,
            String = 4,
            Table = 5,
            Function = 6,
            Userdata = 7,
            Thread = 8
        }
    }
}
