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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

using lua_KContext = System.IntPtr;
using size_t = System.UIntPtr;

namespace Triton.Native
{
    /// <summary>
    /// Provides access to native methods.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Style", "IDE1005:Naming Styles", Justification = "Matching declarations")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching declarations")]
    [SuppressUnmanagedCodeSecurity]
    internal static unsafe class NativeMethods
    {
        public const int LUAI_MAXSTACK = 1000000;

        public const int LUA_REGISTRYINDEX = -LUAI_MAXSTACK - 1000;

        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        public const int LUA_REFNIL = -1;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void* lua_Alloc(void* ud, void* ptr, size_t osize, size_t nsize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_CFunction(lua_State* L);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_KFunction(lua_State* L, LuaStatus status, lua_KContext ctx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate byte* lua_Reader(lua_State* L, void* ud, size_t* sz);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void lua_WarnFunction(void* ud, byte* msg, int tocont);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_Writer(lua_State* L, void* p, size_t sz, void* ud);

        // =============================================================================================================
        // lua.h functions
        //

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_absindex(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_arith(lua_State* L, LuaArithmeticOp op);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_CFunction lua_atpanic(lua_State* L, lua_CFunction panicf);

        public static LuaStatus lua_call(lua_State* L, int nargs, int nresults) =>
            lua_callk(L, nargs, nresults, default, null);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_callk(
            lua_State* L, int nargs, int nresults, lua_KContext ctx, lua_KFunction? k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_checkstack(lua_State* L, int n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_compare(lua_State* L, int index1, int index2, LuaComparisonOp op);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_concat(lua_State* L, int n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(lua_State* L, int narr, int nrec);

        // TODO: lua_dump
        // TODO: lua_error
        // TODO: lua_gc
        // TODO: lua_getallocf

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_getfield(lua_State* L, int index, byte* k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_getglobal(lua_State* L, byte* name);

        // TODO: lua_getextraspace

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_geti(lua_State* L, int index, long n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_getmetatable(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_gettable(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gettop(lua_State* L);

        // TODO: lua_getiuservalue
        // TODO: lua_insert
        // TODO: lua_isboolean
        // TODO: lua_iscfunction
        // TODO: lua_isfunction

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isinteger(lua_State* L, int index);

        // TODO: lua_islightuserdata
        // TODO: lua_isnil
        // TODO: lua_isnone
        // TODO: lua_isnoneornil

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isnumber(lua_State* L, int index);

        // TODO: lua_isstring
        // TODO: lua_istable
        // TODO: lua_isthread
        // TODO: lua_isuserdata
        // TODO: lua_isyieldable
        // TODO: lua_len
        // TODO: lua_load
        // TODO: lua_newstate

        public static void lua_newtable(lua_State* L) => lua_createtable(L, 0, 0);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_State* lua_newthread(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void* lua_newuserdatauv(lua_State* L, size_t size, int nuvalue);

        // TODO: lua_next
        // TODO: lua_numbertointeger

        public static LuaStatus lua_pcall(lua_State* L, int nargs, int nresults, int msgh) =>
            lua_pcallk(L, nargs, nresults, msgh, default, null);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_pcallk(
            lua_State* L, int nargs, int nresults, int msgh, lua_KContext ctx, lua_KFunction? k);

        public static void lua_pop(lua_State* L, int n)
        {
            if (n != 0)
            {
                Debug.Assert(lua_gettop(L) >= n);

                lua_settop(L, -n - 1);
            }
        }

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushboolean(lua_State* L, bool b);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(lua_State* L, lua_CFunction fn, int n);

        public static void lua_pushcfunction(lua_State* L, lua_CFunction fn) => lua_pushcclosure(L, fn, 0);

        // TODO: lua_pushfstring

        public static void lua_pushglobaltable(lua_State* L) =>
            lua_rawgeti(L, LUA_REGISTRYINDEX, LUA_RIDX_GLOBALS);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(lua_State* L, long n);

        // TODO: lua_pushlightuserdata
        // TODO: lua_pushliteral

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* lua_pushlstring(lua_State* L, byte* s, size_t len);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(lua_State* L, double n);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* lua_pushstring(lua_State* L, byte* s);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_pushthread(lua_State* L);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(lua_State* L, int index);

        // TODO: lua_pushvfstring
        // TODO: lua_rawequal

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawget(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_rawgeti(lua_State* L, int index, long n);

        // TODO: lua_rawgetp
        // TODO: lua_rawlen
        // TODO: lua_rawset
        // TODO: lua_rawseti
        // TODO: lua_rawsetp
        // TODO: lua_register
        // TODO: lua_remove
        // TODO: lua_replace
        // TODO: lua_resetthread

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_resume(lua_State* L, lua_State* from, int nargs, int* nresults);

        // TODO: lua_rotate
        // TODO: lua_setallocf

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setfield(lua_State* L, int index, byte* k);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setglobal(lua_State* L, byte* name);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_seti(lua_State* L, int index, long n);

        // TODO: lua_setiuservalue

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setmetatable(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settable(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(lua_State* L, int index);

        // TODO: lua_setwarnf

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus lua_status(lua_State* L);

        // TODO: lua_stringtonumber

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_toboolean(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_CFunction lua_tocfunction(lua_State* L, int index);

        public static long lua_tointeger(lua_State* L, int index) => lua_tointegerx(L, index, null);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern long lua_tointegerx(lua_State* L, int index, bool* isnum);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* lua_tolstring(lua_State* L, int index, size_t* len);

        public static double lua_tonumber(lua_State* L, int index) => lua_tonumberx(L, index, null);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern double lua_tonumberx(lua_State* L, int index, bool* isnum);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void* lua_topointer(lua_State* L, int index);

        public static byte* lua_tostring(lua_State* L, int index) => lua_tolstring(L, index, null);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_State* lua_tothread(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void* lua_touserdata(lua_State* L, int index);

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaType lua_type(lua_State* L, int index);

        // TODO: lua_typename

        public static int lua_upvalueindex(int i) => LUA_REGISTRYINDEX - i;

        // TODO: lua_version
        // TODO: lua_warning
        // TODO: lua_xmove
        // TODO: lua_yield
        // TODO: lua_yieldk

        // =============================================================================================================
        // lauxlib.h functions
        //

        // TODO: luaL_addchar
        // TODO: luaL_addgsub
        // TODO: luaL_addlstring
        // TODO: luaL_addsize
        // TODO: luaL_addstring
        // TODO: luaL_addvalue
        // TODO: luaL_argcheck
        // TODO: luaL_argerror
        // TODO: luaL_argexpected
        // TODO: luaL_buffaddr
        // TODO: luaL_buffinit
        // TODO: luaL_bufflen
        // TODO: luaL_buffinitsize
        // TODO: luaL_buffsub
        // TODO: luaL_callmeta
        // TODO: luaL_checkany
        // TODO: luaL_checkinteger
        // TODO: luaL_checklstring
        // TODO: luaL_checknumber
        // TODO: luaL_checkoption
        // TODO: luaL_checkstack
        // TODO: luaL_checkstring
        // TODO: luaL_checktype
        // TODO: luaL_checkudata
        // TODO: luaL_checkversion
        // TODO: luaL_dofile
        // TODO: luaL_dostring
        // TODO: luaL_error
        // TODO: luaL_execresult
        // TODO: luaL_fileresult
        // TODO: luaL_getmetafield
        // TODO: luaL_getmetatable
        // TODO: luaL_getsubtable
        // TODO: luaL_gsub
        // TODO: luaL_len
        // TODO: luaL_loadbuffer
        // TODO: luaL_loadbufferx
        // TODO: luaL_loadfile
        // TODO: luaL_loadfilex

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaStatus luaL_loadstring(lua_State* L, byte* s);

        // TODO: luaL_newlib
        // TODO: luaL_newlibtable
        // TODO: luaL_newmetatable

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern lua_State* luaL_newstate();

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(lua_State* L);

        // TODO: luaL_opt
        // TODO: luaL_optinteger
        // TODO: luaL_optlstring
        // TODO: luaL_optnumber
        // TODO: luaL_optstring
        // TODO: luaL_prepbuffer
        // TODO: luaL_prepbuffsize
        // TODO: luaL_pushfail
        // TODO: luaL_pushresult
        // TODO: luaL_pushresultsize

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(lua_State* L, int t);

        // TODO: luaL_requiref
        // TODO: luaL_setfuncs
        // TODO: luaL_setmetatable
        // TODO: luaL_testudata
        // TODO: luaL_tolstring
        // TODO: luaL_traceback
        // TODO: luaL_typeerror
        // TODO: luaL_typename

        [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(lua_State* L, int t, int @ref);

        // TODO: luaL_where

        /// <summary>
        /// An opaque structure which represents a Lua thread.
        /// </summary>
        public struct lua_State
        {
        }
    }
}
