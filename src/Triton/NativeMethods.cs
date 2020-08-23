// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Triton
{
    /// <summary>
    /// Provides access to native Lua methods.
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

        internal static readonly MethodInfo _lua_gettop =
            typeof(NativeMethods).GetMethod(nameof(lua_gettop))!;

        internal static readonly MethodInfo _lua_isinteger =
            typeof(NativeMethods).GetMethod(nameof(lua_isinteger))!;

        internal static readonly MethodInfo _lua_toboolean =
            typeof(NativeMethods).GetMethod(nameof(lua_toboolean))!;

        internal static readonly MethodInfo _lua_tointeger =
            typeof(NativeMethods).GetMethod(nameof(lua_tointeger))!;

        internal static readonly MethodInfo _lua_tonumber =
            typeof(NativeMethods).GetMethod(nameof(lua_tonumber))!;

        internal static readonly MethodInfo _lua_touserdata =
            typeof(NativeMethods).GetMethod(nameof(lua_touserdata))!;

        internal static readonly MethodInfo _lua_tostring =
            typeof(NativeMethods).GetMethod(nameof(lua_tostring))!;

        internal static readonly MethodInfo _lua_pushboolean =
            typeof(NativeMethods).GetMethod(nameof(lua_pushboolean))!;

        internal static readonly MethodInfo _lua_pushlightuserdata =
            typeof(NativeMethods).GetMethod(nameof(lua_pushlightuserdata))!;

        internal static readonly MethodInfo _lua_pushinteger =
            typeof(NativeMethods).GetMethod(nameof(lua_pushinteger))!;

        internal static readonly MethodInfo _lua_pushnil =
            typeof(NativeMethods).GetMethod(nameof(lua_pushnil))!;

        internal static readonly MethodInfo _lua_pushnumber =
            typeof(NativeMethods).GetMethod(nameof(lua_pushnumber))!;

        internal static readonly MethodInfo _lua_pushstring =
            typeof(NativeMethods).GetMethod(nameof(lua_pushstring), new[] { typeof(IntPtr), typeof(string) })!;

        internal static readonly MethodInfo _lua_remove =
            typeof(NativeMethods).GetMethod(nameof(lua_remove))!;

        internal static readonly MethodInfo _lua_type =
            typeof(NativeMethods).GetMethod(nameof(lua_type))!;

        internal static readonly MethodInfo _luaL_error =
            typeof(NativeMethods).GetMethod(nameof(luaL_error), new[] { typeof(IntPtr), typeof(string) })!;

        // =============================================================================================================
        // lua.h imports
        //
        // See http://www.lua.org/manual/5.4/manual.html#4 for a detailed description of these functions.

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(IntPtr L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(IntPtr L, int narr, int nrec);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_getfield(IntPtr L, int index, IntPtr k);

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

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_getglobal(IntPtr L, IntPtr name);

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
        public static extern IntPtr lua_pushstring(IntPtr L, IntPtr s);

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
        public static extern IntPtr lua_pushlstring(IntPtr L, IntPtr s, UIntPtr len);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgeti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgetp(IntPtr L, int index, IntPtr p);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr lua_rawlen(IntPtr L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(IntPtr L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawsetp(IntPtr L, int index, IntPtr p);

        public static void lua_remove(IntPtr L, int index)
        {
            lua_rotate(L, index, -1);
            lua_pop(L, 1);
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_resume(IntPtr l, IntPtr from, int nargs, IntPtr nresults);

        public static unsafe LuaStatus lua_resume(IntPtr L, IntPtr from, int nargs)
        {
            int nresults;
            return lua_resume(L, from, nargs, (IntPtr)(&nresults));
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rotate(IntPtr L, int index, int n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setfield(IntPtr L, int index, IntPtr k);

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

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setglobal(IntPtr L, IntPtr name);

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

        [DoesNotReturn]
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

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus luaL_loadstring(IntPtr L, IntPtr s);

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
    }
}
