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

namespace Triton.Interop.Emit.Helpers
{
    /// <summary>
    /// Provides helper methods for pushing values onto a Lua stack.
    /// </summary>
    internal static unsafe class LuaPushHelpers
    {
        // For efficiency, we provide specializations for the `string`, `LuaObject`, `LuaTable`, `LuaFunction`, and
        // `LuaThread` types. We can rely on JIT generic specialization for all other types.

        private static readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new()
        {
            [typeof(string)]      = typeof(LuaPushHelpers).GetMethod(nameof(PushString), NonPublic | Static)!,
            [typeof(LuaObject)]   = typeof(LuaPushHelpers).GetMethod(nameof(PushLuaObject), NonPublic | Static)!,
            [typeof(LuaTable)]    = typeof(LuaPushHelpers).GetMethod(nameof(PushLuaObject), NonPublic | Static)!,
            [typeof(LuaFunction)] = typeof(LuaPushHelpers).GetMethod(nameof(PushLuaObject), NonPublic | Static)!,
            [typeof(LuaThread)]   = typeof(LuaPushHelpers).GetMethod(nameof(PushLuaObject), NonPublic | Static)!
        };

        private static readonly MethodInfo _push =
            typeof(LuaPushHelpers).GetMethod(nameof(Push), NonPublic | Static)!;

        private static readonly MethodInfo _pushNullable =
            typeof(LuaPushHelpers).GetMethod(nameof(PushNullable), NonPublic | Static)!;

        /// <summary>
        /// Gets the method for pushing a value of a given type onto a Lua stack.
        /// </summary>
        /// <param name="type">The type of value.</param>
        /// <returns>The method for pushing a value of the given type onto a Lua stack.</returns>
        public static MethodInfo Get(Type type) =>
            _methodCache.GetOrAdd(type.Simplify(), type =>
                Nullable.GetUnderlyingType(type) switch
                {
                    null                => _push.MakeGenericMethod(type),
                    Type underlyingType => _pushNullable.MakeGenericMethod(underlyingType)
                });

        // TODO: determine whether aggressive inlining will be beneficial

        internal static void PushString(lua_State* state, string? value)
        {
            if (value is null)
            {
                lua_pushnil(state);
            }
            else
            {
                _ = lua_pushstring(state, value);
            }
        }

        internal static void PushLuaObject(lua_State* state, LuaObject? value)
        {
            if (value is null)
            {
                lua_pushnil(state);
            }
            else
            {
                value.Push(state);
            }
        }

        internal static void Push<T>(lua_State* state, T value)
        {
            if (typeof(T) == typeof(LuaValue))
            {
                Unsafe.As<T, LuaValue>(ref value).Push(state);
            }
            else if (typeof(T) == typeof(bool))
            {
                lua_pushboolean(state, Unsafe.As<T, bool>(ref value));
            }
            else if (typeof(T) == typeof(IntPtr))
            {
                lua_pushlightuserdata(state, Unsafe.As<T, IntPtr>(ref value));
            }
            else if (typeof(T) == typeof(byte))
            {
                lua_pushinteger(state, Unsafe.As<T, byte>(ref value));
            }
            else if (typeof(T) == typeof(short))
            {
                lua_pushinteger(state, Unsafe.As<T, short>(ref value));
            }
            else if (typeof(T) == typeof(int))
            {
                lua_pushinteger(state, Unsafe.As<T, int>(ref value));
            }
            else if (typeof(T) == typeof(long))
            {
                lua_pushinteger(state, Unsafe.As<T, long>(ref value));
            }
            else if (typeof(T) == typeof(sbyte))
            {
                lua_pushinteger(state, Unsafe.As<T, sbyte>(ref value));
            }
            else if (typeof(T) == typeof(ushort))
            {
                lua_pushinteger(state, Unsafe.As<T, ushort>(ref value));
            }
            else if (typeof(T) == typeof(uint))
            {
                lua_pushinteger(state, Unsafe.As<T, uint>(ref value));
            }
            else if (typeof(T) == typeof(ulong))
            {
                lua_pushinteger(state, (long)Unsafe.As<T, ulong>(ref value));
            }
            else if (typeof(T) == typeof(float))
            {
                lua_pushnumber(state, Unsafe.As<T, float>(ref value));
            }
            else if (typeof(T) == typeof(double))
            {
                lua_pushnumber(state, Unsafe.As<T, double>(ref value));
            }
            else if (typeof(T) == typeof(char))
            {
                _ = lua_pushstring(state, Unsafe.As<T, char>(ref value).ToString());
            }
            else
            {
                if (value is null)
                {
                    lua_pushnil(state);
                }
                else
                {
                    var environment = lua_getenvironment(state);
                    environment.PushClrEntity(state, value, isTypes: false);
                }
            }
        }

        internal static void PushNullable<T>(lua_State* state, T? value) where T : struct
        {
            if (value is null)
            {
                lua_pushnil(state);
            }
            else
            {
                Push(state, value.Value);
            }
        }
    }
}
