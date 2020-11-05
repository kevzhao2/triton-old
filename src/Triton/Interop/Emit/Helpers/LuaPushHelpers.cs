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
using Triton.Interop.Emit.Extensions;
using static Triton.Lua;

namespace Triton.Interop.Emit.Helpers
{
    /// <summary>
    /// Provides helper methods for pushing values onto a Lua stack.
    /// </summary>
    internal static unsafe class LuaPushHelpers
    {
        // For value types and reference types that should be treated as CLR entities, generic specialization of
        // `Push<T>` and `PushNullable<T>` will result in optimal code. For the other types, we provide specializations.

        private static readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new()
        {
            [typeof(string)]      = GetMethod(nameof(PushString)),
            [typeof(LuaObject)]   = GetMethod(nameof(PushLuaObject)),
            [typeof(LuaTable)]    = GetMethod(nameof(PushLuaObject)),
            [typeof(LuaFunction)] = GetMethod(nameof(PushLuaObject)),
            [typeof(LuaThread)]   = GetMethod(nameof(PushLuaObject))
        };

        private static readonly MethodInfo _push = GetMethod(nameof(Push));
        private static readonly MethodInfo _pushNullable = GetMethod(nameof(PushNullable));

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
                return;
            }

            _ = lua_pushstring(state, value);
        }

        internal static void PushLuaObject(lua_State* state, LuaObject? value)
        {
            if (value is null)
            {
                lua_pushnil(state);
                return;
            }

            value.Push(state);
        }

        internal static void Push<T>(lua_State* state, T value)
        {
            if (typeof(T) == typeof(LuaValue))
            {
                ((LuaValue)(object)value!).Push(state);
            }
            else if (typeof(T) == typeof(bool))
            {
                lua_pushboolean(state, (bool)(object)value!);
            }
            else if (typeof(T) == typeof(IntPtr))
            {
                lua_pushlightuserdata(state, (IntPtr)(object)value!);
            }
            else if (typeof(T) == typeof(byte))
            {
                lua_pushinteger(state, (byte)(object)value!);
            }
            else if (typeof(T) == typeof(short))
            {
                lua_pushinteger(state, (short)(object)value!);
            }
            else if (typeof(T) == typeof(int))
            {
                lua_pushinteger(state, (int)(object)value!);
            }
            else if (typeof(T) == typeof(long))
            {
                lua_pushinteger(state, (long)(object)value!);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                lua_pushinteger(state, (sbyte)(object)value!);
            }
            else if (typeof(T) == typeof(ushort))
            {
                lua_pushinteger(state, (ushort)(object)value!);
            }
            else if (typeof(T) == typeof(uint))
            {
                lua_pushinteger(state, (uint)(object)value!);
            }
            else if (typeof(T) == typeof(ulong))
            {
                lua_pushinteger(state, (long)(ulong)(object)value!);
            }
            else if (typeof(T) == typeof(float))
            {
                lua_pushnumber(state, (float)(object)value!);
            }
            else if (typeof(T) == typeof(double))
            {
                lua_pushnumber(state, (double)(object)value!);
            }
            else if (typeof(T) == typeof(char))
            {
                _ = lua_pushstring(state, ((char)(object)value!).ToString());
            }
            else
            {
                if (value is null)
                {
                    lua_pushnil(state);
                    return;
                }

                var environment = lua_getenvironment(state);
                environment.PushClrEntity(state, value, isTypes: false);
            }
        }

        internal static void PushNullable<T>(lua_State* state, T? value) where T : struct
        {
            if (value is null)
            {
                lua_pushnil(state);
                return;
            }

            Push(state, value.Value);
        }

        // Helper method for simplifying static field initializations.
        private static MethodInfo GetMethod(string name) =>
            typeof(LuaPushHelpers).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
    }
}
