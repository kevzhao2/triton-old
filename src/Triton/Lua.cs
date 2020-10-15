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
using System.Text;
using static Triton.Lua.LuaStatus;

namespace Triton
{
    /// <summary>
    /// Provides access to the Lua C API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching declarations")]
    internal static unsafe class Lua
    {
        /// <summary>
        /// An opaque structure representing a Lua state.
        /// </summary>
        public struct lua_State
        {
        }

        /// <summary>
        /// Specifies the status of a Lua operation.
        /// </summary>
        public enum LuaStatus
        {
            LUA_OK = 0,
            LUA_YIELD = 1,
            LUA_ERRRUN = 2,
            LUA_ERRSYNTAX = 3,
            LUA_ERRMEM = 4,
            LUA_ERRERR = 5
        }

        /// <summary>
        /// Specifies the type of a Lua value.
        /// </summary>
        public enum LuaType
        {
            LUA_TNONE = -1,
            LUA_TNIL = 0,
            LUA_TBOOLEAN = 1,
            LUA_TLIGHTUSERDATA = 2,
            LUA_TNUMBER = 3,
            LUA_TSTRING = 4,
            LUA_TTABLE = 5,
            LUA_TFUNCTION = 6,
            LUA_TUSERDATA = 7,
            LUA_TTHREAD = 8
        }

        public const int LUA_REGISTRYINDEX = -1001000;

        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        public const int LUA_REFNIL = -1;

        // =============================================================================================================
        // lua.h imports
        //
        // See http://www.lua.org/manual/5.4/manual.html#4 for a detailed description of these functions.

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(lua_State* L, int narr, int nrec);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_geti(lua_State* L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gettop(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_getmetatable(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_gettable(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isinteger(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_State* lua_newthread(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void* lua_newuserdatauv(lua_State* L, nuint size, int nuvalue);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_next(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushboolean(lua_State* L, bool b);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> fn, int n);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(lua_State* L, long n);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlightuserdata(lua_State* L, IntPtr p);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(lua_State* L);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(lua_State* L, double n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgeti(lua_State* L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(lua_State* L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_seti(lua_State* L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setmetatable(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settable(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_status(lua_State* L);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_toboolean(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* lua_tolstring(lua_State* L, int index, nuint* len);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void* lua_topointer(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_type(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaType lua_getfield(lua_State* L, int index, byte* k);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaType lua_getglobal(lua_State* L, byte* name);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaStatus lua_pcallk(
            lua_State* L, int nargs, int nresults, int msgh, IntPtr ctx, IntPtr k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte* lua_pushlstring(lua_State* L, byte* s, nuint len);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaStatus lua_resume(lua_State* l, lua_State* from, int nargs, int* nresults);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern void lua_setfield(lua_State* L, int index, byte* k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern void lua_setglobal(lua_State* L, byte* name);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern long lua_tointegerx(lua_State* L, int index, bool* isnum);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern double lua_tonumberx(lua_State* L, int index, bool* isnum);

        // =============================================================================================================
        // lauxlib.h imports
        //
        // See http://www.lua.org/manual/5.4/manual.html#5 for a detailed description of these functions.

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_State* luaL_newstate();

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(lua_State* L, int t);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(lua_State* L, int t, int @ref);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern LuaStatus luaL_loadstring(lua_State* L, byte* s);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        private static extern int luaL_error(lua_State* L, byte* fmt);

        // =============================================================================================================
        // Helpers

        public static LuaEnvironment lua_getenvironment(lua_State* L)
        {
            var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_getextraspace(L));
            return (LuaEnvironment)handle.Target!;
        }

        public static void* lua_getextraspace(lua_State* L) => (void*)((IntPtr)L - IntPtr.Size);

        public static LuaType lua_getfield(lua_State* L, int index, string k)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(k.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(k, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return lua_getfield(L, index, buffer);
            }
        }

        [SkipLocalsInit]
        public static LuaType lua_getglobal(lua_State* L, string name)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(name.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(name, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return lua_getglobal(L, buffer);
            }
        }

        public static void lua_newtable(lua_State* L) => lua_createtable(L, 0, 0);

        public static LuaResults lua_pcall(lua_State* L, int nargs, int nresults, int msgh)
        {
            var status = lua_pcallk(L, nargs, nresults, msgh, default, default);
            if (status != LUA_OK)
            {
                var message = lua_tostring(L, -1);
                throw new LuaRuntimeException(message);
            }

            return new(L);
        }

        public static void lua_pop(lua_State* L, int n) => lua_settop(L, -n - 1);

        public static void lua_pushcfunction(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> fn) =>
            lua_pushcclosure(L, fn, 0);

        [SkipLocalsInit]
        public static byte* lua_pushstring(lua_State* L, string s)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(s.Length);
            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(s, span);

            fixed (byte* buffer = span)
            {
                return lua_pushlstring(L, buffer, (nuint)length);
            }
        }

        [SkipLocalsInit]
        public static LuaResults lua_resume(lua_State* L, lua_State* from, int nargs)
        {
            int nresults;
            var status = lua_resume(L, from, nargs, &nresults);
            if (status != LUA_OK && status != LUA_YIELD)
            {
                var message = lua_tostring(L, -1);
                throw new LuaRuntimeException(message);
            }

            return new(L);
        }

        [SkipLocalsInit]
        public static void lua_setfield(lua_State* L, int index, string k)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(k.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(k, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                lua_setfield(L, index, buffer);
            }
        }

        [SkipLocalsInit]
        public static unsafe void lua_setglobal(lua_State* L, string name)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(name.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(name, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                lua_setglobal(L, buffer);
            }
        }

        public static GCHandle lua_tohandle(lua_State* L, int index)
        {
            var ptr = lua_topointer(L, 1);
            return GCHandle.FromIntPtr(*(IntPtr*)ptr);
        }

        public static long lua_tointeger(lua_State* L, int index) => lua_tointegerx(L, index, null);

        public static double lua_tonumber(lua_State* L, int index) => lua_tonumberx(L, index, null);

        [SkipLocalsInit]
        public static string lua_tostring(lua_State* L, int index)
        {
            nuint length;
            var ptr = lua_tolstring(L, index, &length);

            return Encoding.UTF8.GetString(ptr, (int)length);
        }

        public static object lua_totarget(lua_State* L, int index) => lua_tohandle(L, index).Target!;

        [SkipLocalsInit]
        public static int luaL_error(lua_State* L, string fmt)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(fmt.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(fmt, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                return luaL_error(L, buffer);
            }
        }

        [SkipLocalsInit]
        public static void luaL_loadstring(lua_State* L, string s)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(s.Length) + 1;

            var span = maxLength <= 1024 ? stackalloc byte[1024] : new byte[maxLength];
            var length = Encoding.UTF8.GetBytes(s, span);
            span[length] = 0;  // Null terminator

            fixed (byte* buffer = span)
            {
                var status = luaL_loadstring(L, buffer);
                if (status != LUA_OK)
                {
                    var message = lua_tostring(L, -1);
                    throw new LuaLoadException(message);
                }
            }
        }
    }
}
