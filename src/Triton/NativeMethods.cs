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
using System.Runtime.InteropServices;
using System.Text;

namespace Triton
{
    /// <summary>
    /// Provides access to native Lua functions.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Style", "IDE1005:Naming Styles", Justification = "Matching declarations")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching declarations")]
    internal static class NativeMethods
    {
        /// <summary>
        /// Specifies the status of a Lua operation.
        /// </summary>
        public enum LuaStatus
        {
            Ok = 0,
            Yield = 1,
            ErrRun = 2,
            ErrSyntax = 3,
            ErrMem = 4,
            ErrErr = 5
        }

        /// <summary>
        /// Specifies the type of a Lua value.
        /// </summary>
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

        /// <summary>
        /// Represents a Lua C function.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The number of results.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaCFunction(IntPtr L);

        public const int LUAI_MAXSTACK = 1000000;

        public const int LUA_REGISTRYINDEX = -LUAI_MAXSTACK - 1000;

        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        public const int LUA_REFNIL = -1;

        // =============================================================================================================
        // lua.h imports
        //
        // See http://www.lua.org/manual/5.4/manual.html#4 for a detailed description of these functions.
        //

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(IntPtr L, int narr, int nrec);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_geti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gettop(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isinteger(IntPtr L, int index);

        public static void lua_newtable(IntPtr L) => lua_createtable(L, 0, 0);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newthread(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newuserdatauv(IntPtr L, UIntPtr size, int nuvalue);

        public static LuaStatus lua_pcall(IntPtr L, int nargs, int nresults, int msgh) =>
            lua_pcallk(L, nargs, nresults, msgh, default, default);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_pcallk(IntPtr L, int nargs, int nresults, int msgh, IntPtr ctx, IntPtr k);

        public static void lua_pop(IntPtr L, int n) => lua_settop(L, -n - 1);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushboolean(IntPtr L, bool b);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(IntPtr L, LuaCFunction fn, int n);

        public static void lua_pushcfunction(IntPtr L, LuaCFunction fn) => lua_pushcclosure(L, fn, 0);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(IntPtr L, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(IntPtr L, double n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_pushlstring(IntPtr L, IntPtr s, UIntPtr len);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgeti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rotate(IntPtr L, int index, int n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setfield(IntPtr L, int index, IntPtr k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setmetatable(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_toboolean(IntPtr L, int index);

        public static long lua_tointeger(IntPtr L, int index) => lua_tointegerx(L, index, IntPtr.Zero);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern long lua_tointegerx(IntPtr L, int index, IntPtr isnum);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_tolstring(IntPtr L, int index, IntPtr len);

        public static double lua_tonumber(IntPtr L, int index) => lua_tonumberx(L, index, IntPtr.Zero);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern double lua_tonumberx(IntPtr L, int index, IntPtr isnum);

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

        // =============================================================================================================
        // lauxlib.h imports
        //
        // See http://www.lua.org/manual/5.4/manual.html#5 for a detailed description of these functions.
        //

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_newstate();

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(IntPtr L, int t);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(IntPtr L, int t, int @ref);

        [DoesNotReturn]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_error(IntPtr L, IntPtr fmt);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus luaL_loadstring(IntPtr L, IntPtr s);
    }
}
