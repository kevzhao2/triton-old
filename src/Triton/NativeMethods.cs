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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.CallingConvention;

namespace Triton
{
    /// <summary>
    /// Represents a Lua state as an opaque structure.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Naming consistency")]
    internal struct lua_State
    {
    }

    /// <summary>
    /// Provides access to native methods.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Naming consistency")]
    [DebuggerStepThrough]
    internal static unsafe class NativeMethods
    {
        public const int LUAI_MAXSTACK = 1000000;

        public const int LUA_MULTRET = -1;

        public const int LUA_REGISTRYINDEX = -LUAI_MAXSTACK - 1000;

        public const int LUA_OK = 0;
        public const int LUA_YIELD = 1;
        public const int LUA_ERRRUN = 2;
        public const int LUA_ERRSYNTAX = 3;
        public const int LUA_ERRMEM = 4;
        public const int LUA_ERRERR = 5;

        public const int LUA_TNONE = -1;
        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;

        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        #region Lua state manipulation

        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern lua_State* luaL_newstate();

        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void luaL_openlibs(lua_State* L);

        public static void* lua_getextraspace(lua_State* L) => (void*)((IntPtr)L - IntPtr.Size);

        public static LuaEnvironment lua_getenvironment(lua_State* L)
        {
            var handle = *(GCHandle*)lua_getextraspace(L);
            var target = handle.Target!;
            return Unsafe.As<object, LuaEnvironment>(ref target);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_status(lua_State* L);

        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_close(lua_State* L);

        #endregion

        #region Lua stack manipulation

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_gettop(lua_State* L);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_settop(lua_State* L, int index);

        public static void lua_pop(lua_State* L, int n) => lua_settop(L, -n - 1);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_rotate(lua_State* L, int index, int n);

        public static void lua_remove(lua_State* L, int index)
        {
            lua_rotate(L, index, -1);
            lua_pop(L, 1);
        }

        #endregion

        #region Lua stack pushing

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushnil(lua_State* L);

        public static void lua_pushboolean(lua_State* L, bool b)
        {
            lua_pushboolean(L, Unsafe.As<bool, byte>(ref b));
            return;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_pushboolean(lua_State* L, int b);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushinteger(lua_State* L, long n);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushnumber(lua_State* L, double n);

        [SkipLocalsInit]
        public static void lua_pushstring(lua_State* L, string s)
        {
            const int bufferSize = 1024;

            if (s.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = s)
                {
                    var length = Encoding.UTF8.GetBytes(chars, s.Length, bytes, bufferSize);
                    lua_pushlstring(L, bytes, (nuint)length);
                }
            }
            else
            {
                SlowPath(L, s);
            }

            return;

            static void SlowPath(lua_State* L, string s)
            {
                var arr = Encoding.UTF8.GetBytes(s);
                fixed (byte* bytes = arr)
                {
                    lua_pushlstring(L, bytes, (nuint)arr.Length);
                }
            }

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_pushlstring(lua_State* L, byte* s, nuint len);
        }

        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_createtable(lua_State* L, int narr, int nrec);

        public static void lua_newtable(lua_State* L) => lua_createtable(L, 0, 0);

        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern lua_State* lua_newthread(lua_State* L);

        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void* lua_newuserdatauv(lua_State* L, nuint size, int nuvalue);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushcclosure(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> fn, int n);

        public static void lua_pushcfunction(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> fn) =>
            lua_pushcclosure(L, fn, 0);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushvalue(lua_State* L, int index);
        #endregion

        #region Lua stack accessing

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_type(lua_State* L, int index);

        public static bool lua_isinteger(lua_State* L, int index)
        {
            return lua_isinteger(L, index) != 0;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_isinteger(lua_State* L, int index);
        }

        public static bool lua_toboolean(lua_State* L, int index)
        {
            return lua_toboolean(L, index) != 0;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_toboolean(lua_State* L, int index);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern long lua_tointegerx(lua_State* L, int index, bool* isnum);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern double lua_tonumberx(lua_State* L, int index, bool* isnum);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern byte* lua_tolstring(lua_State* L, int index, nuint* len);

        [SkipLocalsInit]
        public static string lua_tostring(lua_State* L, int index)
        {
            nuint len;
            var bytes = lua_tolstring(L, index, &len);

            return Encoding.UTF8.GetString(bytes, (int)len);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void* lua_touserdata(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void* lua_topointer(lua_State* L, int index);

        #endregion

        #region Lua getters

        [SkipLocalsInit]
        public static int lua_getglobal(lua_State* L, string name)
        {
            const int bufferSize = 1024;

            if (name.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = name)
                {
                    var length = Encoding.UTF8.GetBytes(chars, name.Length, bytes, bufferSize);
                    bytes[length] = 0;

                    return lua_getglobal(L, bytes);
                }
            }
            else
            {
                return SlowPath(L, name);
            }

            static int SlowPath(lua_State* L, string name)
            {
                fixed (byte* bytes = Encoding.UTF8.GetBytes(name + '\0'))
                {
                    return lua_getglobal(L, bytes);
                }
            }

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_getglobal(lua_State* L, byte* name);
        }

        [SkipLocalsInit]
        public static int lua_getfield(lua_State* L, int index, string k)
        {
            const int bufferSize = 1024;

            if (k.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = k)
                {
                    var length = Encoding.UTF8.GetBytes(chars, k.Length, bytes, bufferSize);
                    bytes[length] = 0;

                    return lua_getfield(L, index, bytes);
                }
            }
            else
            {
                return SlowPath(L, index, k);
            }

            static int SlowPath(lua_State* L, int index, string k)
            {
                fixed (byte* bytes = Encoding.UTF8.GetBytes(k + '\0'))
                {
                    return lua_getfield(L, index, bytes);
                }
            }

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_getfield(lua_State* L, int index, byte* k);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_geti(lua_State* L, int index, long i);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_gettable(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_rawgeti(lua_State* L, int index, long n);

        public static bool lua_getmetatable(lua_State* L, int index)
        {
            return lua_getmetatable(L, index) != 0;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_getmetatable(lua_State* L, int index);
        }

        #endregion

        #region Lua setters

        [SkipLocalsInit]
        public static void lua_setglobal(lua_State* L, string name)
        {
            const int bufferSize = 1024;

            if (name.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = name)
                {
                    var length = Encoding.UTF8.GetBytes(chars, name.Length, bytes, bufferSize);
                    bytes[length] = 0;

                    lua_setglobal(L, bytes);
                }
            }
            else
            {
                SlowPath(L, name);
            }

            static void SlowPath(lua_State* L, string name)
            {
                fixed (byte* bytes = Encoding.UTF8.GetBytes(name + '\0'))
                {
                    lua_setglobal(L, bytes);
                }
            }

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_setglobal(lua_State* L, byte* name);
        }

        [SkipLocalsInit]
        public static void lua_setfield(lua_State* L, int index, string k)
        {
            const int bufferSize = 1024;

            if (k.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = k)
                {
                    var length = Encoding.UTF8.GetBytes(chars, k.Length, bytes, bufferSize);
                    bytes[length] = 0;

                    lua_setfield(L, index, bytes);
                }
            }
            else
            {
                SlowPath(L, index, k);
            }

            static void SlowPath(lua_State* L, int index, string k)
            {
                fixed (byte* bytes = Encoding.UTF8.GetBytes(k + '\0'))
                {
                    lua_setfield(L, index, bytes);
                }
            }

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_setfield(lua_State* L, int index, byte* k);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_seti(lua_State* L, int index, long i);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_settable(lua_State* L, int index);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_rawseti(lua_State* L, int index, long i);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_setmetatable(lua_State* L, int index);

        #endregion

        #region Lua helpers

        [SkipLocalsInit]
        public static void luaL_loadstring(lua_State* L, string s)
        {
            const int bufferSize = 1024;

            if (s.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = s)
                {
                    var length = Encoding.UTF8.GetBytes(chars, s.Length, bytes, bufferSize);
                    bytes[length] = 0;

                    if (luaL_loadstring(L, bytes) != LUA_OK)
                    {
                        var message = lua_tostring(L, -1);
                        ThrowHelper.ThrowLuaLoadException(message);
                    }
                }
            }
            else
            {
                SlowPath(L, s);
            }

            static void SlowPath(lua_State* L, string s)
            {
                fixed (byte* bytes = Encoding.UTF8.GetBytes(s + '\0'))
                {
                    if (luaL_loadstring(L, bytes) != LUA_OK)
                    {
                        var message = lua_tostring(L, -1);
                        ThrowHelper.ThrowLuaLoadException(message);
                    }
                }
            }

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int luaL_loadstring(lua_State* L, byte* s);
        }

        public static LuaResults lua_pcall(lua_State* L, int nargs, int nresults)
        {
            if (lua_pcallk(L, nargs, nresults, 0, null, null) != LUA_OK)
            {
                var message = lua_tostring(L, -1);
                ThrowHelper.ThrowLuaRuntimeException(message);
            }

            return new(L);

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_pcallk(lua_State* L, int nargs, int nresults, int msgh, void* ctx, void* k);
        }

        [SkipLocalsInit]
        public static LuaResults lua_resume(lua_State* L, lua_State* from, int nargs)
        {
            int nresults;
            if (lua_resume(L, from, nargs, &nresults) is not (LUA_OK or LUA_YIELD))
            {
                var message = lua_tostring(L, -1);
                ThrowHelper.ThrowLuaRuntimeException(message);
            }
            return new(L);

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_resume(lua_State* L, lua_State* from, int nargs, int* nresults);
        }

        public static bool lua_next(lua_State* L, int index)
        {
            return lua_next(L, index) != 0;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_next(lua_State* L, int index);
        }

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int luaL_ref(lua_State* L, int t);

        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void luaL_unref(lua_State* L, int t, int @ref);

        #endregion
    }
}
