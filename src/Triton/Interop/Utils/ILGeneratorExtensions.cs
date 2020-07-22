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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop.Utils
{
    /// <summary>
    /// Provides extensions for the <see cref="ILGenerator"/> class.
    /// </summary>
    internal static class ILGeneratorExtensions
    {
        private static readonly MethodInfo _lua_pushboolean = typeof(NativeMethods).GetMethod(nameof(lua_pushboolean))!;
        private static readonly MethodInfo _lua_pushinteger = typeof(NativeMethods).GetMethod(nameof(lua_pushinteger))!;
        private static readonly MethodInfo _lua_pushnumber = typeof(NativeMethods).GetMethod(nameof(lua_pushnumber))!;
        private static readonly MethodInfo _lua_pushstring = typeof(NativeMethods).GetMethod(nameof(lua_pushstring))!;

        private static readonly MethodInfo _pushObject =
            typeof(ILGeneratorExtensions).GetMethod(nameof(PushObject), NonPublic | Static)!;

        private static readonly HashSet<Type> _integerTypes = new HashSet<Type>
        {
            typeof(sbyte), typeof(byte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong)
        };

        private static readonly HashSet<Type> _numberTypes = new HashSet<Type> { typeof(float), typeof(double) };

        /// <summary>
        /// Emits a Lua push for the given <paramref name="type"/>, assuming that the state and the value have been
        /// emitted.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type.</param>
        public static void EmitLuaPush(this ILGenerator ilg, Type type)
        {
            if (type == typeof(bool))
            {
                ilg.Emit(Call, _lua_pushboolean);
            }
            else if (_integerTypes.Contains(type))
            {
                // As an optimization, don't emit conv.i8 if the type is already the correct size.
                if (type != typeof(long) && type != typeof(ulong))
                {
                    ilg.Emit(Conv_I8);
                }

                ilg.Emit(Call, _lua_pushinteger);
            }
            else if (_numberTypes.Contains(type))
            {
                // As an optimization, don't emit conv.r8 if the type is already the correct size.
                if (type != typeof(double))
                {
                    ilg.Emit(Conv_R8);
                }

                ilg.Emit(Call, _lua_pushnumber);
            }
            else if (type == typeof(string))
            {
                ilg.Emit(Call, _lua_pushstring);
                ilg.Emit(Pop);
            }
            else
            {
                if (type.IsValueType)
                {
                    ilg.Emit(Box, type);
                }

                ilg.Emit(Call, _pushObject);
            }
        }

        // TODO: this can be optimized by keeping the `LuaEnvironment` instance in context
        private static void PushObject(IntPtr state, object obj)
        {
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(lua_getextraspace(state)));
            if (!(handle.Target is LuaEnvironment environment))
            {
                lua_pushnil(state);
                return;
            }

            environment.PushClrObject(state, obj);
        }
    }
}
