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
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    internal static class ILGeneratorExtensions
    {
        private static readonly FieldInfo _environment =
            typeof(MetamethodContext).GetField("_environment", NonPublic | Instance)!;

        private static readonly MethodInfo _lua_pushboolean
            = typeof(NativeMethods).GetMethod(nameof(lua_pushboolean))!;

        private static readonly MethodInfo _lua_pushlightuserdata
            = typeof(NativeMethods).GetMethod(nameof(lua_pushlightuserdata))!;

        private static readonly MethodInfo _lua_pushinteger
            = typeof(NativeMethods).GetMethod(nameof(lua_pushinteger))!;

        private static readonly MethodInfo _lua_pushnumber
            = typeof(NativeMethods).GetMethod(nameof(lua_pushnumber))!;

        private static readonly MethodInfo _lua_pushstring
            = typeof(NativeMethods).GetMethod(nameof(lua_pushstring))!;

        private static readonly MethodInfo _luaL_error
            = typeof(NativeMethods).GetMethod(nameof(luaL_error))!;

        private static readonly MethodInfo _pushLuaObject =
            typeof(ILGeneratorExtensions).GetMethod(nameof(PushLuaObject), NonPublic | Static)!;

        private static readonly MethodInfo _pushClrEntity =
            typeof(ILGeneratorExtensions).GetMethod(nameof(PushClrEntity), NonPublic | Static)!;

        private static readonly MethodInfo _intPtrOpExplicit =
            typeof(IntPtr).GetMethod("op_Explicit", new[] { typeof(void*) });

        private static readonly HashSet<Type> _signedIntegerTypes =
            new HashSet<Type> { typeof(sbyte), typeof(short), typeof(int), typeof(long) };

        private static readonly HashSet<Type> _unsignedIntegerTypes =
            new HashSet<Type> { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) };

        private static readonly HashSet<Type> _numberTypes = new HashSet<Type> { typeof(float), typeof(double) };

        public static Label[] DefineLabels(this ILGenerator ilg, int count)
        {
            var labels = new Label[count];
            for (var i = 0; i < count; ++i)
            {
                labels[i] = ilg.DefineLabel();
            }

            return labels;
        }

        public static void EmitLoadIndirect(this ILGenerator ilg, Type type)
        {
            if (type == typeof(sbyte))       ilg.Emit(Ldind_I1);
            else if (type == typeof(byte))   ilg.Emit(Ldind_U1);
            else if (type == typeof(short))  ilg.Emit(Ldind_I2);
            else if (type == typeof(ushort)) ilg.Emit(Ldind_U2);
            else if (type == typeof(int))    ilg.Emit(Ldind_I4);
            else if (type == typeof(uint))   ilg.Emit(Ldind_U4);
            else if (type == typeof(long))   ilg.Emit(Ldind_I8);
            else if (type == typeof(ulong))  ilg.Emit(Ldind_I8);
            else if (type == typeof(float))  ilg.Emit(Ldind_R4);
            else if (type == typeof(double)) ilg.Emit(Ldind_R8);
            else if (type.IsValueType)       ilg.Emit(Ldobj, type);
            else                             ilg.Emit(Ldind_Ref);
        }

        public static void EmitLuaError(this ILGenerator ilg, string message)
        {
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldstr, message);
            ilg.Emit(Call, _luaL_error);
        }

        public static void EmitLuaPush(this ILGenerator ilg, Type type)
        {
            if (type == typeof(bool))
            {
                ilg.Emit(Call, _lua_pushboolean);
            }
            else if (type.IsPointer)
            {
                ilg.Emit(Call, _intPtrOpExplicit);
                ilg.Emit(Call, _lua_pushlightuserdata);
            }
            else if (_signedIntegerTypes.Contains(type))
            {
                // As an optimization, don't emit conv.i8 if the type is already the correct size.
                if (type != typeof(long))
                {
                    ilg.Emit(Conv_I8);
                }

                ilg.Emit(Call, _lua_pushinteger);
            }
            else if (_unsignedIntegerTypes.Contains(type))
            {
                // As an optimization, don't emit conv.u8 if the type is already the correct size.
                if (type != typeof(ulong))
                {
                    ilg.Emit(Conv_U8);
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
            else if (typeof(LuaObject).IsAssignableFrom(type))
            {
                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldfld, _environment);
                ilg.Emit(Call, _pushLuaObject);
            }
            else
            {
                if (type.IsValueType)
                {
                    ilg.Emit(Box, type);
                }

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldfld, _environment);
                ilg.Emit(Call, _pushClrEntity);
            }
        }

        private static void PushLuaObject(IntPtr state, LuaObject? obj, LuaEnvironment environment)
        {
            if (obj is null)
            {
                lua_pushnil(state);
            }
            else
            {
                environment.PushLuaObject(state, obj);
            }
        }

        private static void PushClrEntity(IntPtr state, object? entity, LuaEnvironment environment)
        {
            if (entity is null)
            {
                lua_pushnil(state);
            }
            else
            {
                environment.PushClrEntity(state, entity);
            }
        }
    }
}
