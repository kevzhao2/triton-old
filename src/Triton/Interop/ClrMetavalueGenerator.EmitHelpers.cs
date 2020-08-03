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
using System.Text;
using Triton.Interop.Extensions;
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    internal partial class ClrMetavalueGenerator
    {
        private static readonly MethodInfo _charToString =
            typeof(char).GetMethod(nameof(char.ToString), Public | Static);

        private static readonly PropertyInfo _stringIndexer = typeof(string).GetProperty("Chars");
        private static readonly PropertyInfo _stringLength = typeof(string).GetProperty(nameof(string.Length));

        private static void EmitLuaError(ILGenerator ilg, string message)
        {
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldstr, message);
            ilg.Emit(Call, _luaL_error);
        }

        private static void EmitLuaLoad(
            ILGenerator ilg, Type type,
            Action<ILGenerator> getLuaTypeAction,
            Action<ILGenerator> getLuaIndexAction,
            Action<ILGenerator> invalidAction)
        {
            /*type = type.Simplify();

            var isByRef = type.IsByRef;
            if (isByRef)
            {
                type = type.GetElementType();
            }*/

            /*var expectedLuaType = type switch
            {
                _ when type == typeof(bool)                     => _lua_pushboolean,
                _ when type == typeof(byte)                     => _lua_pushinteger,
                _ when type == typeof(sbyte)                    => _lua_pushinteger,
                _ when type == typeof(short)                    => _lua_pushinteger,
                _ when type == typeof(ushort)                   => _lua_pushinteger,
                _ when type == typeof(int)                      => _lua_pushinteger,
                _ when type == typeof(uint)                     => _lua_pushinteger,
                _ when type == typeof(long)                     => _lua_pushinteger,
                _ when type == typeof(ulong)                    => _lua_pushinteger,
                _ when type == typeof(IntPtr)                   => _lua_pushlightuserdata,
                _ when type == typeof(UIntPtr)                  => _lua_pushlightuserdata,
                _ when type == typeof(char)                     => _lua_pushstring,
                _ when type == typeof(float)                    => _lua_pushnumber,
                _ when type == typeof(double)                   => _lua_pushnumber,
                _ when type == typeof(string)                   => _lua_pushstring,
                _ when typeof(LuaObject).IsAssignableFrom(type) => MetamethodContext._pushLuaObject,
                _                                               => MetamethodContext._pushClrEntity
            };*/

            if (type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) ||
                type == typeof(sbyte) || type == typeof(short) || type == typeof(int))
            {
                var temp = ilg.DeclareLocal(type);

                var success = ilg.DefineLabel();

                var fail = ilg.BeginExceptionBlock();
                {
                    ilg.Emit(type switch
                    {
                        _ when type == typeof(byte)   => Conv_Ovf_U1,
                        _ when type == typeof(ushort) => Conv_Ovf_U2,
                        _ when type == typeof(uint)   => Conv_Ovf_U4,
                        _ when type == typeof(sbyte)  => Conv_Ovf_I1,
                        _ when type == typeof(short)  => Conv_Ovf_I2,
                        _                             => Conv_Ovf_I4
                    });
                    ilg.Emit(Stloc, temp);
                    ilg.Emit(Leave, success);  // Not short form
                }

                ilg.BeginCatchBlock(typeof(OverflowException));
                {
                    ilg.Emit(Leave_S, fail);
                }

                ilg.EndExceptionBlock();
                {
                    invalidAction(ilg);
                }

                ilg.MarkLabel(success);

                ilg.Emit(Ldloc, temp);
            }
            else if (type == typeof(char))
            {
                var temp = ilg.DeclareLocal(typeof(string));

                var isLengthOne = ilg.DefineLabel();

                ilg.Emit(Stloc, temp);

                ilg.Emit(Ldloc, temp);
                ilg.Emit(Call, _stringLength.GetMethod);
                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Beq, isLengthOne);  // Not short form
                {
                    invalidAction(ilg);
                }

                ilg.MarkLabel(isLengthOne);

                ilg.Emit(Ldloc, temp);
                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Call, _stringIndexer.GetMethod);
            }
            else if (type == typeof(float))
            {
                ilg.Emit(Conv_R4);
            }
            else if (!type.IsPrimitive && type.IsValueType)
            {
            }
            else if (!type.IsValueType && type != typeof(string))
            {
            }

            if (type == typeof(bool))
            {
                var isBoolean = ilg.DefineLabel();

                getLuaTypeAction(ilg);
                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Beq, isBoolean);  // Not short form
                {
                    invalidAction(ilg);
                }

                ilg.Emit(Ldarg_1);
                getLuaIndexAction(ilg);
                ilg.Emit(Call, _lua_toboolean);
            }
            else if (type == typeof(IntPtr) || type == typeof(UIntPtr))
            {
                var isLightUserdata = ilg.DefineLabel();

                getLuaTypeAction(ilg);
                ilg.Emit(Ldc_I4_2);
                ilg.Emit(Beq, isLightUserdata);  // Not short form
                {
                    invalidAction(ilg);
                }

                ilg.Emit(Ldarg_1);
                getLuaIndexAction(ilg);
                ilg.Emit(Call, _lua_touserdata);
            }
            else if (type == typeof(string))
            {
                var isString = ilg.DefineLabel();

                getLuaTypeAction(ilg);
                ilg.Emit(Ldc_I4_4);
                ilg.Emit(Beq, isString);  // Not short form
                {
                    invalidAction(ilg);
                }

                ilg.Emit(Ldarg_1);
                getLuaIndexAction(ilg);
                ilg.Emit(Call, _lua_tostring);
            }








        }

        private static void EmitLuaPush(
            ILGenerator ilg, Type type,
            Action<ILGenerator> getAction)
        {
            type = type.Simplify();

            var isByRef = type.IsByRef;
            if (isByRef)
            {
                type = type.GetElementType();
            }

            if (!type.IsValueType && type != typeof(string))
            {
                ilg.Emit(Ldarg_0);  // Required to call `MetamethodContext` instance methods
            }

            ilg.Emit(Ldarg_1);
            getAction(ilg);

            if (isByRef)
            {
                ilg.EmitLdind(type);
            }

            // If necessary, convert the value into an acceptable form. This may involve widening, converting, or boxing
            // the value.
            //
            if (type == typeof(byte) || type == typeof(ushort) || type == typeof(uint))
            {
                ilg.Emit(Conv_U8);
            }
            else if (type == typeof(sbyte) || type == typeof(short) || type == typeof(int))
            {
                ilg.Emit(Conv_I8);
            }
            else if (type == typeof(char))
            {
                ilg.Emit(Call, _charToString);
            }
            else if (type == typeof(float))
            {
                ilg.Emit(Conv_R8);
            }
            else if (!type.IsPrimitive && type.IsValueType)
            {
                ilg.Emit(Box, type);
            }

            ilg.Emit(Call, true switch
            {
                _ when type == typeof(bool)                     => _lua_pushboolean,
                _ when type == typeof(byte)                     => _lua_pushinteger,
                _ when type == typeof(sbyte)                    => _lua_pushinteger,
                _ when type == typeof(short)                    => _lua_pushinteger,
                _ when type == typeof(ushort)                   => _lua_pushinteger,
                _ when type == typeof(int)                      => _lua_pushinteger,
                _ when type == typeof(uint)                     => _lua_pushinteger,
                _ when type == typeof(long)                     => _lua_pushinteger,
                _ when type == typeof(ulong)                    => _lua_pushinteger,
                _ when type == typeof(IntPtr)                   => _lua_pushlightuserdata,
                _ when type == typeof(UIntPtr)                  => _lua_pushlightuserdata,
                _ when type == typeof(char)                     => _lua_pushstring,
                _ when type == typeof(float)                    => _lua_pushnumber,
                _ when type == typeof(double)                   => _lua_pushnumber,
                _ when type == typeof(string)                   => _lua_pushstring,
                _ when typeof(LuaObject).IsAssignableFrom(type) => MetamethodContext._pushLuaObject,
                _                                               => MetamethodContext._pushClrEntity
            });
        }
    }
}
