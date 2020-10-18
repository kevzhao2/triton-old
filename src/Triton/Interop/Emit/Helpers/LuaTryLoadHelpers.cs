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
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Triton.Interop.Emit.Extensions;
using static System.Reflection.BindingFlags;
using static Triton.Lua;
using static Triton.Lua.LuaType;

namespace Triton.Interop.Emit.Helpers
{
    /// <summary>
    /// Provides helper methods for trying to load values from the Lua stack.
    /// </summary>
    internal static unsafe class LuaTryLoadHelpers
    {
        // For efficiency, we provide specializations for the `string`, `LuaObject`, `LuaTable`, `LuaFunction`, and
        // `LuaThread` types. We can rely on JIT generic specialization for all other types.

        private static readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new()
        {
            [typeof(string)]      = typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoadString), NonPublic | Static)!,
            [typeof(LuaObject)]   = typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoadLuaObject), NonPublic | Static)!,
            [typeof(LuaTable)]    = typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoadLuaTable), NonPublic | Static)!,
            [typeof(LuaFunction)] = typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoadLuaFunction), NonPublic | Static)!,
            [typeof(LuaThread)]   = typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoadLuaThread), NonPublic | Static)!,
        };

        private static readonly MethodInfo _tryLoad =
            typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoad), NonPublic | Static)!;

        private static readonly MethodInfo _tryLoadNullable =
            typeof(LuaTryLoadHelpers).GetMethod(nameof(TryLoadNullable), NonPublic | Static)!;

        /// <summary>
        /// Gets the method for trying to load a value of a given type from the Lua stack.
        /// </summary>
        /// <param name="type">The type of value.</param>
        /// <returns>The method for trying to load a value of the given type from the Lua stack.</returns>
        public static MethodInfo Get(Type type) =>
            _methodCache.GetOrAdd(type.Simplify(), type =>
                Nullable.GetUnderlyingType(type) switch
                {
                    null                => _tryLoad.MakeGenericMethod(type),
                    Type underlyingType => _tryLoadNullable.MakeGenericMethod(underlyingType)
                });

        // TODO: determine whether aggressive inlining will be beneficial
        // TODO: determine whether passing `LuaType` will be beneficial

        internal static bool TryLoadString(lua_State* state, int index, out string value)
        {
            var type = lua_type(state, index);
            if (type is not LUA_TSTRING)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            value = lua_tostring(state, index);
            return true;
        }

        internal static bool TryLoadLuaObject(lua_State* state, int index, out LuaObject value)
        {
            var type = lua_type(state, index);
            if (type is not (LUA_TTABLE or LUA_TFUNCTION or LUA_TTHREAD))
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            var environment = lua_getenvironment(state);
            value = environment.LoadLuaObject(state, index, type);
            return true;
        }

        internal static bool TryLoadLuaTable(lua_State* state, int index, out LuaTable value)
        {
            var type = lua_type(state, index);
            if (type is not LUA_TTABLE)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            var environment = lua_getenvironment(state);
            value = (LuaTable)environment.LoadLuaObject(state, index, type);
            return true;
        }

        internal static bool TryLoadLuaFunction(lua_State* state, int index, out LuaFunction value)
        {
            var type = lua_type(state, index);
            if (type is not LUA_TFUNCTION)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            var environment = lua_getenvironment(state);
            value = (LuaFunction)environment.LoadLuaObject(state, index, type);
            return true;
        }

        internal static bool TryLoadLuaThread(lua_State* state, int index, out LuaThread value)
        {
            var type = lua_type(state, index);
            if (type is not LUA_TTHREAD)
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            var environment = lua_getenvironment(state);
            value = (LuaThread)environment.LoadLuaObject(state, index, type);
            return true;
        }

        internal static bool TryLoad<T>(lua_State* state, int index, out T value)
        {
            var type = lua_type(state, index);

            Unsafe.SkipInit(out value);
            if (typeof(T) == typeof(LuaValue))
            {
                LuaValue.FromLua(state, index, type, out Unsafe.As<T, LuaValue>(ref value));
                return true;
            }
            else if (typeof(T) == typeof(bool))
            {
                if (type is not LUA_TBOOLEAN)
                {
                    return false;
                }

                Unsafe.As<T, bool>(ref value) = lua_toboolean(state, index);
                return true;
            }
            else if (typeof(T) == typeof(IntPtr))
            {
                if (type is not LUA_TLIGHTUSERDATA)
                {
                    return false;
                }

                Unsafe.As<T, IntPtr>(ref value) = (IntPtr)lua_topointer(state, index);
                return true;
            }
            else if (typeof(T) == typeof(byte))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                try
                {
                    Unsafe.As<T, byte>(ref value) = checked((byte)lua_tointeger(state, index));
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else if (typeof(T) == typeof(short))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                try
                {
                    Unsafe.As<T, short>(ref value) = checked((short)lua_tointeger(state, index));
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else if (typeof(T) == typeof(int))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                try
                {
                    Unsafe.As<T, int>(ref value) = checked((int)lua_tointeger(state, index));
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else if (typeof(T) == typeof(long))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                Unsafe.As<T, long>(ref value) = lua_tointeger(state, index);
                return true;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                try
                {
                    Unsafe.As<T, sbyte>(ref value) = checked((sbyte)lua_tointeger(state, index));
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else if (typeof(T) == typeof(ushort))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                try
                {
                    Unsafe.As<T, ushort>(ref value) = checked((ushort)lua_tointeger(state, index));
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else if (typeof(T) == typeof(uint))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                try
                {
                    Unsafe.As<T, uint>(ref value) = checked((uint)lua_tointeger(state, index));
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
            else if (typeof(T) == typeof(ulong))
            {
                if (type is not LUA_TNUMBER || !lua_isinteger(state, index))
                {
                    return false;
                }

                Unsafe.As<T, ulong>(ref value) = (ulong)lua_tointeger(state, index);
                return true;
            }
            else if (typeof(T) == typeof(float))
            {
                if (type is not LUA_TNUMBER)
                {
                    return false;
                }

                Unsafe.As<T, float>(ref value) = (float)lua_tonumber(state, index);
                return true;
            }
            else if (typeof(T) == typeof(double))
            {
                if (type is not LUA_TNUMBER)
                {
                    return false;
                }

                Unsafe.As<T, double>(ref value) = lua_tonumber(state, index);
                return true;
            }
            else if (typeof(T) == typeof(char))
            {
                if (type is not LUA_TSTRING || lua_tostring(state, index) is not { Length: 1 } str)
                {
                    return false;
                }

                Unsafe.As<T, char>(ref value) = str[0];
                return true;
            }
            else
            {
                if (type is LUA_TNIL && !typeof(T).IsValueType)
                {
                    value = default!;
                    return true;
                }
                else if (type is not LUA_TUSERDATA)
                {
                    return false;
                }

                var environment = lua_getenvironment(state);
                if (environment.LoadClrEntity(state, index) is not T innerValue)
                {
                    return false;
                }

                value = innerValue;
                return true;
            }
        }

        internal static bool TryLoadNullable<T>(lua_State* state, int index, out T? value) where T : struct
        {
            var type = lua_type(state, index);
            if (type is LUA_TNIL)
            {
                value = null;
                return true;
            }

            if (!TryLoad(state, index, out T innerValue))
            {
                Unsafe.SkipInit(out value);
                return false;
            }

            value = innerValue;
            return true;
        }
    }
}
