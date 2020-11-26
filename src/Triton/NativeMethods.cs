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
    /// An opaque structure representing a Lua state.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Naming consistency")]
    internal struct lua_State
    {
    }

    /// <summary>
    /// Provides access to native methods.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Naming consistency")]
    [SkipLocalsInit]
    internal static unsafe class NativeMethods
    {
        public const int LUAI_MAXSTACK = 1000000;

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

        /// <summary>
        /// Creates a new Lua state.
        /// </summary>
        /// <returns>The resulting Lua state.</returns>
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern lua_State* luaL_newstate();

        /// <summary>
        /// Opens the standard Lua libraries in the Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void luaL_openlibs(lua_State* L);

        /// <summary>
        /// Gets a pointer to the extra space portion of the Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>A pointer to the extra space portion of the Lua state.</returns>
        public static void* lua_getextraspace(lua_State* L) => (void*)((IntPtr)L - IntPtr.Size);

        /// <summary>
        /// Gets the environment associated with the Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The environment associated with the Lua state.</returns>
        public static LuaEnvironment lua_getenvironment(lua_State* L)
        {
            var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_getextraspace(L));
            var target = handle.Target!;
            return Unsafe.As<object, LuaEnvironment>(ref target);  // optimal cast, should be safe
        }

        /// <summary>
        /// Closes the given Lua state.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_close(lua_State* L);

        #endregion

        #region Lua stack manipulation

        /// <summary>
        /// Gets the top index of the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <returns>The top index of the stack.</returns>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_gettop(lua_State* L);

        /// <summary>
        /// Sets the top index of the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index to set the top of the stack to.</param>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_settop(lua_State* L, int idx);

        /// <summary>
        /// Pops the given number of values off of the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="n">The number of values to pop off of the stack.</param>
        public static void lua_pop(lua_State* L, int n) => lua_settop(L, -n - 1);

        /// <summary>
        /// Rotates the values on the stack from the given index to the top by the given number of positions.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index to the start the rotation at.</param>
        /// <param name="n">The number of positions to rotate by.</param>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_rotate(lua_State* L, int idx, int n);

        /// <summary>
        /// Removes the value on the stack at the given index.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to remove.</param>
        public static void lua_remove(lua_State* L, int idx)
        {
            lua_rotate(L, idx, -1);
            lua_pop(L, 1);
        }

        #endregion

        #region Lua stack pushing

        /// <summary>
        /// Pushes <see langword="nil"/> onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushnil(lua_State* L);

        /// <summary>
        /// Pushes the given boolean onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="b">The boolean to push onto the stack.</param>
        public static void lua_pushboolean(lua_State* L, bool b)
        {
            lua_pushboolean(L, Unsafe.As<bool, int>(ref b));
            return;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_pushboolean(lua_State* L, int b);
        }

        /// <summary>
        /// Pushes the given integer onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="n">The integer to push onto the stack.</param>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushinteger(lua_State* L, long n);

        /// <summary>
        /// Pushes the given number onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="n">The number to push onto the stack.</param>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushnumber(lua_State* L, double n);

        /// <summary>
        /// Pushes the given string onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="s">The string to push onto the stack.</param>
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
                var arr = Encoding.UTF8.GetBytes(s);
                fixed (byte* bytes = arr)
                {
                    lua_pushlstring(L, bytes, (nuint)arr.Length);
                }
            }

            return;

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_pushlstring(lua_State* L, byte* s, nuint len);
        }

        /// <summary>
        /// Pushes the given C closure onto the stack with the given number of upvalues.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="fn">The C closure to push onto the stack.</param>
        /// <param name="n">The number of upvalues in the closure.</param>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void lua_pushcclosure(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> fn, int n);

        /// <summary>
        /// Pushes the given C function onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="fn">The C function to push onto the stack.</param>
        public static void lua_pushcfunction(lua_State* L, delegate* unmanaged[Cdecl]<lua_State*, int> fn) =>
            lua_pushcclosure(L, fn, 0);
        #endregion

        #region Lua stack accessing

        /// <summary>
        /// Gets the type of the value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to check.</param>
        /// <returns>The type of the value at the index on the stack.</returns>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern int lua_type(lua_State* L, int idx);

        /// <summary>
        /// Determines whether the value at the given index on the stack is an integer.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to check.</param>
        /// <returns><see langword="true"/> if the value at the index on the stack is an integer; otherwise, <see langword="false"/>.</returns>
        public static bool lua_isinteger(lua_State* L, int idx)
        {
            return lua_isinteger(L, idx) != 0;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_isinteger(lua_State* L, int idx);
        }

        /// <summary>
        /// Gets the boolean value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to get.</param>
        /// <returns>The boolean value at the index on the stack.</returns>
        public static bool lua_toboolean(lua_State* L, int idx)
        {
            return lua_toboolean(L, idx) != 0;

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_toboolean(lua_State* L, int idx);
        }

        /// <summary>
        /// Gets the integer value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to get.</param>
        /// <param name="isnum">A pointer to a value which will indicate whether the value is an integer.</param>
        /// <returns>The integer value at the index on the stack.</returns>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern long lua_tointegerx(lua_State* L, int idx, bool* isnum);

        /// <summary>
        /// Gets the number value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to get.</param>
        /// <param name="isnum">A pointer to a value which will indicate whether the value is a number.</param>
        /// <returns>The number value at the index on the stack.</returns>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern double lua_tonumberx(lua_State* L, int idx, bool* isnum);

        /// <summary>
        /// Gets the string value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to get.</param>
        /// <param name="len">A pointer to a value which will contain the length of the string.</param>
        /// <returns>The string value at the index on the stack.</returns>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern byte* lua_tolstring(lua_State* L, int idx, nuint* len);

        /// <summary>
        /// Gets the string value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to get.</param>
        /// <returns>The string value at the index on the stack.</returns>
        public static string lua_tostring(lua_State* L, int idx)
        {
            nuint len;
            var bytes = lua_tolstring(L, idx, &len);

            return Encoding.UTF8.GetString(bytes, (int)len);
        }

        /// <summary>
        /// Gets the userdata value at the given index on the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="idx">The index of the value to get.</param>
        /// <returns>The userdata value at the index on the stack.</returns>
        [SuppressGCTransition]
        [DllImport("lua54", CallingConvention = Cdecl)]
        public static extern void* lua_touserdata(lua_State* L, int idx);

        #endregion

        #region Lua getters

        /// <summary>
        /// Pushes the value of the global with the given name onto the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="name">The name of the global whose value to push.</param>
        /// <returns>The type of the global.</returns>
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

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_getglobal(lua_State* L, byte* name);

            static int SlowPath(lua_State* L, string name)
            {
                var arr = Encoding.UTF8.GetBytes(name + '\0');
                fixed (byte* bytes = arr)
                {
                    return lua_getglobal(L, bytes);
                }
            }
        }

        #endregion

        #region Lua setters

        /// <summary>
        /// Sets the value of the global to the top of the stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="name">The name of the global whose value to set.</param>
        public static void lua_setglobal(lua_State* L, string name)
        {
            const int bufferSize = 1024;

            if (name.Length < bufferSize / 3)
            {
                var bytes = stackalloc byte[bufferSize];
                fixed (char* chars = name)
                {
                    var length = Encoding.UTF8.GetBytes(chars, name.Length, bytes, bufferSize);
                    lua_setglobal(L, bytes);
                }
            }
            else
            {
                var arr = Encoding.UTF8.GetBytes(name);
                fixed (byte* bytes = arr)
                {
                    lua_setglobal(L, bytes);
                }
            }

            [SuppressGCTransition]
            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern void lua_setglobal(lua_State* L, byte* name);
        }

        #endregion

        #region Lua helpers

        /// <summary>
        /// Loads a string as a function, pushing it onto the Lua stack.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="s">The string to load as a function.</param>
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

                    if (luaL_loadstring(L, bytes) is not LUA_OK)
                    {
                        var message = lua_tostring(L, -1);
                        ThrowLuaLoadException(message);
                    }
                }
            }
            else
            {
                SlowPath(L, s);
            }

            static void SlowPath(lua_State* L, string s)
            {
                var arr = Encoding.UTF8.GetBytes(s + '\0');
                fixed (byte* bytes = arr)
                {
                    if (luaL_loadstring(L, bytes) is not LUA_OK)
                    {
                        var message = lua_tostring(L, -1);
                        ThrowLuaLoadException(message);
                    }
                }
            }

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int luaL_loadstring(lua_State* L, byte* s);

            [DebuggerStepThrough]
            [DoesNotReturn]
            static void ThrowLuaLoadException(string message) => throw new LuaLoadException(message);
        }

        /// <summary>
        /// Calls a function on the stack with the number of arguments and results.
        /// </summary>
        /// <param name="L">The Lua state.</param>
        /// <param name="nargs">The number of arguments.</param>
        /// <param name="nresults">The number of results.</param>
        /// <returns>The results of the call.</returns>
        public static LuaResults lua_pcall(lua_State* L, int nargs, int nresults)
        {
            if (lua_pcallk(L, nargs, nresults, 0, null, null) is not LUA_OK)
            {
                var message = lua_tostring(L, -1);
                ThrowLuaRuntimeException(message);
            }

            return new(L);

            [DllImport("lua54", CallingConvention = Cdecl)]
            static extern int lua_pcallk(lua_State* L, int nargs, int nresults, int msgh, void* ctx, void* k);

            [DebuggerStepThrough]
            [DoesNotReturn]
            static void ThrowLuaRuntimeException(string message) => throw new LuaRuntimeException(message);
        }

        #endregion
    }
}
