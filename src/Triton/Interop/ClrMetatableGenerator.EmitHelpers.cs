// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Triton.Interop.Extensions;
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;
using Debug = System.Diagnostics.Debug;

namespace Triton.Interop
{
    internal partial class ClrMetatableGenerator
    {
        private static readonly ConstructorInfo _spanIntConstructor =
            typeof(Span<int>).GetConstructor(new[] { typeof(void*), typeof(int) })!;

        private static readonly PropertyInfo _spanIntIndexer =
            typeof(Span<int>).GetProperty("Item")!;

        private static readonly PropertyInfo _stringLength =
            typeof(string).GetProperty(nameof(string.Length))!;

        private static readonly PropertyInfo _stringIndexer =
            typeof(string).GetProperty("Chars")!;

        private static readonly MethodInfo _charToString =
            typeof(char).GetMethod(nameof(char.ToString), Public | Static)!;

        private static readonly Type[] _metamethodParameterTypes = new[] { typeof(MetamethodContext), typeof(IntPtr) };

        private readonly List<LuaCFunction> _generatedFunctions = new List<LuaCFunction>();

        private static void EmitLuaError(
            ILGenerator ilg, string message)
        {
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldstr, message);
            ilg.Emit(Call, typeof(NativeMethods).GetMethod("luaL_error", new[] { typeof(IntPtr), typeof(string) })!);
            ilg.Emit(Ret);
        }

        #region EmitLuaErrorMemberName

        private static void EmitLuaErrorMemberName(
            ILGenerator ilg, Label label, string message)
        {
            ilg.MarkLabel(label);
            {
                ilg.Emit(Ldarg_0);  // Lua state
                ilg.Emit(Ldstr, message);
                ilg.Emit(Call, _luaL_error);
                ilg.Emit(Ret);
            }
        }

        #endregion

        private static void EmitLuaLoad(
            ILGenerator ilg, Type type, LocalBuilder temp,
            LocalBuilder luaType, LocalBuilder luaIndex, Label isInvalidValue)
        {
            // Unpack the type into the non-nullable type and then the underlying enumeration type.

            var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
            var isNullableType = nonNullableType != type;

            if (nonNullableType.IsEnum)
            {
                nonNullableType = Enum.GetUnderlyingType(nonNullableType);
            }

            var skip = ilg.DefineLabel();

            {
                // Handle `null` if it is a valid value: i.e., when the type is not a value type or is a nullable type.

                var isNotValueType = !type.IsValueType;
                if (isNotValueType || isNullableType)
                {
                    var isNotNull = ilg.DefineLabel();

                    ilg.Emit(Ldloc, luaType);
                    ilg.Emit(Brtrue_S, isNotNull);
                    {
                        if (isNotValueType)
                        {
                            ilg.Emit(Ldnull);
                            ilg.Emit(Stloc, temp);
                        }
                        else
                        {
                            ilg.Emit(Ldloca, temp);
                            ilg.Emit(Initobj, type);
                        }

                        ilg.Emit(Br_S, skip);
                    }

                    ilg.MarkLabel(isNotNull);
                }
            }

            {
                // Verify that the Lua type is correct.
                // - For the `LuaObject` type, three Lua types are valid (due to polymorphism).
                // - For the `LuaValue` type, all Lua types are valid.

                if (nonNullableType == typeof(LuaObject))
                {
                    var isCorrectType = ilg.DefineLabel();

                    ilg.Emit(Ldloc, luaType);
                    ilg.Emit(Ldc_I4_5);
                    ilg.Emit(Beq_S, isCorrectType);

                    ilg.Emit(Ldloc, luaType);
                    ilg.Emit(Ldc_I4_6);
                    ilg.Emit(Beq_S, isCorrectType);

                    ilg.Emit(Ldloc, luaType);
                    ilg.Emit(Ldc_I4_8);
                    ilg.Emit(Bne_Un, isInvalidValue);  // Not short form

                    ilg.MarkLabel(isCorrectType);
                }
                else if (nonNullableType != typeof(LuaValue))
                {
                    ilg.Emit(Ldloc, luaType);
                    ilg.Emit(true switch
                    {
                        _ when nonNullableType.IsBoolean()            => Ldc_I4_1,
                        _ when nonNullableType.IsLightUserdata()      => Ldc_I4_2,
                        _ when nonNullableType.IsInteger()            => Ldc_I4_3,
                        _ when nonNullableType.IsNumber()             => Ldc_I4_3,
                        _ when nonNullableType.IsString()             => Ldc_I4_4,
                        _ when nonNullableType == typeof(LuaTable)    => Ldc_I4_5,
                        _ when nonNullableType == typeof(LuaFunction) => Ldc_I4_6,
                        _ when nonNullableType.IsClrObject()          => Ldc_I4_7,
                        _ when nonNullableType == typeof(LuaThread)   => Ldc_I4_8,
                        _                                             => throw new InvalidOperationException()
                    });
                    ilg.Emit(Bne_Un, isInvalidValue);  // Not short form

                    if (nonNullableType.IsInteger())
                    {
                        ilg.Emit(Ldarg_1);  // Lua state
                        ilg.Emit(Ldloc, luaIndex);
                        ilg.Emit(Call, _lua_isinteger);
                        ilg.Emit(Brfalse, isInvalidValue);  // Not short form
                    }
                }
            }

            {
                // Load the value from the Lua stack. For nullable types, we need to load the value into a temporary in
                // order to construct the nullable.

                var nonNullableTemp = isNullableType ? ilg.DeclareReusableLocal(nonNullableType) : null;

                if (nonNullableType == typeof(LuaValue) || nonNullableType.IsLuaObject() ||
                    nonNullableType.IsClrObject())
                {
                    ilg.Emit(Ldarg_0);  // Required for `MetamethodContext` methods
                }

                ilg.Emit(Ldarg_1);  // Lua state
                ilg.Emit(Ldloc, luaIndex);

                if (nonNullableType == typeof(LuaValue) || nonNullableType.IsLuaObject())
                {
                    ilg.Emit(Ldloc, luaType);
                }

                ilg.Emit(Call, true switch
                {
                    _ when nonNullableType == typeof(LuaValue) => MetamethodContext._loadValue,
                    _ when nonNullableType.IsBoolean()         => _lua_toboolean,
                    _ when nonNullableType.IsLightUserdata()   => _lua_touserdata,
                    _ when nonNullableType.IsInteger()         => _lua_tointeger,
                    _ when nonNullableType.IsNumber()          => _lua_tonumber,
                    _ when nonNullableType.IsString()          => _lua_tostring,
                    _ when nonNullableType.IsLuaObject()       => MetamethodContext._loadLuaObject,
                    _ when nonNullableType.IsClrObject()       => MetamethodContext._loadClrEntity,
                    _                                          => throw new InvalidOperationException()
                });

                if (nonNullableType.IsInteger() && nonNullableType != typeof(long) && nonNullableType != typeof(ulong))
                {
                    // Verify that the integer can be converted without overflow or underflow.

                    using var tempLong = ilg.DeclareReusableLocal(typeof(long));

                    ilg.Emit(Stloc, tempLong);

                    ilg.BeginExceptionBlock();
                    {
                        ilg.Emit(Ldloc, tempLong);
                        ilg.Emit(true switch
                        {
                            _ when nonNullableType == typeof(byte)   => Conv_Ovf_U1,
                            _ when nonNullableType == typeof(ushort) => Conv_Ovf_U2,
                            _ when nonNullableType == typeof(uint)   => Conv_Ovf_U4,
                            _ when nonNullableType == typeof(sbyte)  => Conv_Ovf_I1,
                            _ when nonNullableType == typeof(short)  => Conv_Ovf_I2,
                            _                                        => Conv_Ovf_I4
                        });
                        ilg.Emit(Stloc, nonNullableTemp ?? temp);
                    }

                    ilg.BeginCatchBlock(typeof(OverflowException));
                    {
                        ilg.Emit(Leave, isInvalidValue);  // Not short form
                    }

                    ilg.EndExceptionBlock();
                }
                else if (nonNullableType == typeof(float))
                {
                    ilg.Emit(Conv_R4);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else if (nonNullableType == typeof(char))
                {
                    // Verify that the string has exactly one character.

                    using var tempString = ilg.DeclareReusableLocal(typeof(string));

                    ilg.Emit(Stloc, tempString);

                    ilg.Emit(Ldloc, tempString);
                    ilg.Emit(Call, _stringLength.GetMethod!);
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Bne_Un, isInvalidValue);  // Not short form

                    ilg.Emit(Ldloc, tempString);
                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Call, _stringIndexer.GetMethod!);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else if (nonNullableType.IsLuaObject() && nonNullableType != typeof(LuaObject))
                {
                    ilg.Emit(Castclass, nonNullableType);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else if (nonNullableType.IsClrObject())
                {
                    // Verify that the object type is correct.

                    using var tempObject = ilg.DeclareReusableLocal(typeof(object));

                    ilg.Emit(Stloc, tempObject);

                    ilg.Emit(Ldloc, tempObject);
                    ilg.Emit(Isinst, nonNullableType);
                    ilg.Emit(Brfalse, isInvalidValue);  // Not short form

                    ilg.Emit(Ldloc, tempObject);
                    ilg.Emit(Unbox_Any, nonNullableType);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else
                {
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }

                if (isNullableType)
                {
                    ilg.Emit(Ldloca, temp);
                    ilg.Emit(Ldloc, nonNullableTemp!);
                    ilg.Emit(Call, type.GetConstructor(new[] { nonNullableType! })!);
                }

                nonNullableTemp?.Dispose();
            }

            ilg.MarkLabel(skip);
        }

        private static void EmitLuaLoad(
            ILGenerator ilg, Type clrType, LocalBuilder temp,
            Action<ILGenerator> getLuaType,
            Action<ILGenerator> getLuaIndex,
            Label isInvalidLuaValue)
        {
            //Debug.Assert(!clrType.IsByRef);
            //Debug.Assert(!clrType.IsByRefLike);
            Debug.Assert(temp.LocalType == clrType, "Temporary should match type");

            // Unpack the type into the non-nullable type and then the underlying enumeration type.

            var nonNullableType = Nullable.GetUnderlyingType(clrType) ?? clrType;
            var isNullableType = nonNullableType != clrType;

            if (nonNullableType.IsEnum)
            {
                nonNullableType = Enum.GetUnderlyingType(nonNullableType);
            }

            var skip = ilg.DefineLabel();

            {
                // Handle `null` if it is a valid value: i.e., when the type is not a value type or is a nullable type.

                var isNotValueType = !clrType.IsValueType;
                if (isNotValueType || isNullableType)
                {
                    var isNotNull = ilg.DefineLabel();

                    getLuaType(ilg);
                    ilg.Emit(Brtrue_S, isNotNull);
                    {
                        if (isNotValueType)
                        {
                            ilg.Emit(Ldnull);
                            ilg.Emit(Stloc, temp);
                        }
                        else
                        {
                            ilg.Emit(Ldloca, temp);
                            ilg.Emit(Initobj, clrType);
                        }

                        ilg.Emit(Br_S, skip);
                    }

                    ilg.MarkLabel(isNotNull);
                }
            }

            {
                // Verify that the Lua type is correct.
                // - For the `LuaObject` type, three Lua types are valid (due to polymorphism).
                // - For the `LuaValue` type, all Lua types are valid.

                if (nonNullableType == typeof(LuaObject))
                {
                    var isCorrectType = ilg.DefineLabel();

                    getLuaType(ilg);
                    ilg.Emit(Ldc_I4_5);
                    ilg.Emit(Beq_S, isCorrectType);

                    getLuaType(ilg);
                    ilg.Emit(Ldc_I4_6);
                    ilg.Emit(Beq_S, isCorrectType);

                    getLuaType(ilg);
                    ilg.Emit(Ldc_I4_8);
                    ilg.Emit(Bne_Un, isInvalidLuaValue);  // Not short form

                    ilg.MarkLabel(isCorrectType);
                }
                else if (nonNullableType != typeof(LuaValue))
                {
                    getLuaType(ilg);
                    ilg.Emit(true switch
                    {
                        _ when nonNullableType.IsBoolean()            => Ldc_I4_1,
                        _ when nonNullableType.IsLightUserdata()      => Ldc_I4_2,
                        _ when nonNullableType.IsInteger()            => Ldc_I4_3,
                        _ when nonNullableType.IsNumber()             => Ldc_I4_3,
                        _ when nonNullableType.IsString()             => Ldc_I4_4,
                        _ when nonNullableType == typeof(LuaTable)    => Ldc_I4_5,
                        _ when nonNullableType == typeof(LuaFunction) => Ldc_I4_6,
                        _ when nonNullableType.IsClrObject()          => Ldc_I4_7,
                        _ when nonNullableType == typeof(LuaThread)   => Ldc_I4_8,
                        _                                             => throw new InvalidOperationException()
                    });
                    ilg.Emit(Bne_Un, isInvalidLuaValue);  // Not short form

                    if (nonNullableType.IsInteger())
                    {
                        ilg.Emit(Ldarg_1);
                        getLuaIndex(ilg);
                        ilg.Emit(Call, _lua_isinteger);
                        ilg.Emit(Brfalse, isInvalidLuaValue);  // Not short form
                    }
                }
            }

            {
                // Load the value from the Lua stack. For nullable types, we need to load the value into a temporary in
                // order to construct the nullable.

                var nonNullableTemp = isNullableType ? ilg.DeclareReusableLocal(nonNullableType) : null;

                if (nonNullableType == typeof(LuaValue) || nonNullableType.IsLuaObject() ||
                    nonNullableType.IsClrObject())
                {
                    ilg.Emit(Ldarg_0);  // Required for `MetamethodContext` methods
                }

                ilg.Emit(Ldarg_1);
                getLuaIndex(ilg);

                if (nonNullableType == typeof(LuaValue) || nonNullableType.IsLuaObject())
                {
                    getLuaType(ilg);
                }

                ilg.Emit(Call, true switch
                {
                    _ when nonNullableType == typeof(LuaValue) => MetamethodContext._loadValue,
                    _ when nonNullableType.IsBoolean()         => _lua_toboolean,
                    _ when nonNullableType.IsLightUserdata()   => _lua_touserdata,
                    _ when nonNullableType.IsInteger()         => _lua_tointeger,
                    _ when nonNullableType.IsNumber()          => _lua_tonumber,
                    _ when nonNullableType.IsString()          => _lua_tostring,
                    _ when nonNullableType.IsLuaObject()       => MetamethodContext._loadLuaObject,
                    _ when nonNullableType.IsClrObject()       => MetamethodContext._loadClrEntity,
                    _                                          => throw new InvalidOperationException()
                });

                if (nonNullableType.IsInteger() && nonNullableType != typeof(long) && nonNullableType != typeof(ulong))
                {
                    // Verify that the integer can be converted without overflow or underflow.

                    using var tempLong = ilg.DeclareReusableLocal(typeof(long));

                    ilg.Emit(Stloc, tempLong);

                    ilg.BeginExceptionBlock();
                    {
                        ilg.Emit(Ldloc, tempLong);
                        ilg.Emit(true switch
                        {
                            _ when nonNullableType == typeof(byte)   => Conv_Ovf_U1,
                            _ when nonNullableType == typeof(ushort) => Conv_Ovf_U2,
                            _ when nonNullableType == typeof(uint)   => Conv_Ovf_U4,
                            _ when nonNullableType == typeof(sbyte)  => Conv_Ovf_I1,
                            _ when nonNullableType == typeof(short)  => Conv_Ovf_I2,
                            _                                        => Conv_Ovf_I4
                        });
                        ilg.Emit(Stloc, nonNullableTemp ?? temp);
                    }

                    ilg.BeginCatchBlock(typeof(OverflowException));
                    {
                        ilg.Emit(Leave, isInvalidLuaValue);  // Not short form
                    }

                    ilg.EndExceptionBlock();
                }
                else if (nonNullableType == typeof(float))
                {
                    ilg.Emit(Conv_R4);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else if (nonNullableType == typeof(char))
                {
                    // Verify that the string has exactly one character.

                    using var tempString = ilg.DeclareReusableLocal(typeof(string));

                    ilg.Emit(Stloc, tempString);

                    ilg.Emit(Ldloc, tempString);
                    ilg.Emit(Call, _stringLength.GetMethod!);
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Bne_Un, isInvalidLuaValue);  // Not short form

                    ilg.Emit(Ldloc, tempString);
                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Call, _stringIndexer.GetMethod!);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else if (nonNullableType.IsLuaObject() && nonNullableType != typeof(LuaObject))
                {
                    ilg.Emit(Castclass, nonNullableType);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else if (nonNullableType.IsClrObject())
                {
                    // Verify that the object type is correct.

                    using var tempObject = ilg.DeclareReusableLocal(typeof(object));

                    ilg.Emit(Stloc, tempObject);

                    ilg.Emit(Ldloc, tempObject);
                    ilg.Emit(Isinst, nonNullableType);
                    ilg.Emit(Brfalse, isInvalidLuaValue);  // Not short form

                    ilg.Emit(Ldloc, tempObject);
                    ilg.Emit(Unbox_Any, nonNullableType);
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }
                else
                {
                    ilg.Emit(Stloc, nonNullableTemp ?? temp);
                }

                if (isNullableType)
                {
                    ilg.Emit(Ldloca, temp);
                    ilg.Emit(Ldloc, nonNullableTemp!);
                    ilg.Emit(Call, clrType.GetConstructor(new[] { nonNullableType! })!);
                }

                nonNullableTemp?.Dispose();
            }

            ilg.MarkLabel(skip);
        }

        #region EmitLuaPush

        private static void EmitLuaPush(
            ILGenerator ilg, Type type, LocalBuilder value)
        {
            // Unpack the type into the non-byref type, then the non-nullable type, and finally the underlying
            // enumeration type.

            var isByRefType = type.IsByRef;
            var nonByRefType = isByRefType ? type.GetElementType()! : type;

            var nonNullableType = Nullable.GetUnderlyingType(nonByRefType) ?? nonByRefType;
            var isNullableType = nonNullableType != nonByRefType;

            if (nonNullableType.IsEnum)
            {
                nonNullableType = Enum.GetUnderlyingType(nonNullableType);
            }

            var skip = ilg.DefineLabel();

            {
                // Handle `null` if it is a valid value: i.e., when the type is not a value type or is a nullable type.

                var isNotValueType = !type.IsValueType;
                if (isNotValueType || isNullableType)
                {
                    var isNull = ilg.DefineLabel();
                    var isNotNull = ilg.DefineLabel();

                    if (isNotValueType)
                    {
                        ilg.Emit(Ldloc, value);
                        ilg.Emit(isNullableType ? Brfalse_S : Brtrue_S, isNullableType ? isNull : isNotNull);
                    }

                    if (isNullableType)
                    {
                        ilg.Emit(isByRefType ? Ldloc : Ldloca, value);
                        ilg.Emit(Call, nonByRefType.GetProperty("HasValue")!.GetMethod!);
                        ilg.Emit(Brtrue_S, isNotNull);
                    }

                    {
                        ilg.MarkLabel(isNull);

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Call, _lua_pushnil);
                        ilg.Emit(Br_S, skip);
                    }

                    ilg.MarkLabel(isNotNull);
                }
            }

            {
                // Push the value onto the Lua stack.

                if (nonNullableType == typeof(LuaValue) || nonNullableType.IsLuaObject() ||
                    nonNullableType.IsClrObject())
                {
                    ilg.Emit(Ldarg_0);  // Required for `MetamethodContext` methods
                }

                ilg.Emit(Ldarg_1); // Lua state

                if (!isNullableType)
                {
                    ilg.Emit(Ldloc, value);

                    if (isByRefType)
                    {
                        ilg.EmitLdind(nonByRefType);
                    }
                }
                else
                {
                    ilg.Emit(isByRefType ? Ldloc : Ldloca, value);
                    ilg.Emit(Call, nonByRefType.GetProperty("Value")!.GetMethod!);
                }

                if (nonNullableType.IsSignedInteger() && nonNullableType != typeof(long))
                {
                    ilg.Emit(Conv_I8);
                }
                else if (nonNullableType.IsUnsignedInteger() && nonNullableType != typeof(ulong))
                {
                    ilg.Emit(Conv_U8);
                }
                else if (nonNullableType == typeof(float))
                {
                    ilg.Emit(Conv_R8);
                }
                else if (nonNullableType == typeof(char))
                {
                    ilg.Emit(Call, _charToString);
                }
                else if (nonNullableType.IsClrStruct())
                {
                    ilg.Emit(Box, nonNullableType);
                }

                ilg.Emit(Call, true switch
                {
                    _ when nonNullableType == typeof(LuaValue) => MetamethodContext._pushValue,
                    _ when nonNullableType.IsBoolean()         => _lua_pushboolean,
                    _ when nonNullableType.IsLightUserdata()   => _lua_pushlightuserdata,
                    _ when nonNullableType.IsInteger()         => _lua_pushinteger,
                    _ when nonNullableType.IsNumber()          => _lua_pushnumber,
                    _ when nonNullableType.IsString()          => _lua_pushstring,
                    _ when nonNullableType.IsLuaObject()       => MetamethodContext._pushLuaObject,
                    _ when nonNullableType.IsClrObject()       => MetamethodContext._pushClrEntity,
                    _                                          => throw new InvalidOperationException()
                });

                if (nonNullableType.IsString())
                {
                    ilg.Emit(Pop);  // Pop the return value
                }
            }

            ilg.MarkLabel(skip);
        }

        #endregion

        private static LocalBuilder EmitDeclareTarget(ILGenerator ilg, Type objType)
        {
            var isByRef = objType.IsClrStruct();

            var target = ilg.DeclareLocal(isByRef ? objType.MakeByRefType() : objType);
            ilg.Emit(Ldarg_0);
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_1);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.LoadClrEntity))!);
            ilg.Emit(isByRef ? Unbox : Unbox_Any, objType);
            ilg.Emit(Stloc, target);

            return target;
        }

        private static LocalBuilder EmitDeclareKeyType(ILGenerator ilg)
        {
            var keyType = ilg.DeclareLocal(typeof(LuaType));
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Call, typeof(NativeMethods).GetMethod("lua_type")!);
            ilg.Emit(Stloc, keyType);

            return keyType;
        }

        private static LocalBuilder EmitDeclareValueType(ILGenerator ilg)
        {
            var valueType = ilg.DeclareLocal(typeof(LuaType));
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_3);  // Value
            ilg.Emit(Call, typeof(NativeMethods).GetMethod("lua_type")!);
            ilg.Emit(Stloc, valueType);

            return valueType;
        }

        private static LocalBuilder EmitDeclareValueIndex(ILGenerator ilg)
        {
            var valueIndex = ilg.DeclareLocal(typeof(int));
            ilg.Emit(Ldc_I4_3);  // Value
            ilg.Emit(Stloc, valueIndex);

            return valueIndex;
        }

        private static LocalBuilder EmitDeclareArgCount(ILGenerator ilg)
        {
            var argCount = ilg.DeclareLocal(typeof(int));

            ilg.Emit(Ldarg_1);
            ilg.Emit(Call, _lua_gettop);
            ilg.Emit(Stloc, argCount);

            return argCount;
        }

        private static LocalBuilder EmitDeclareArgCount(
            ILGenerator ilg, int offset)
        {
            var argCount = ilg.DeclareLocal(typeof(int));
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Call, typeof(NativeMethods).GetMethod("lua_gettop")!);
            ilg.Emit(Ldc_I4, offset);
            ilg.Emit(Sub);
            ilg.Emit(Stloc, argCount);

            return argCount;
        }

        private static LocalBuilder EmitDeclareArgTypes(
            ILGenerator ilg, LocalBuilder argCount)
        {
            var argTypes = ilg.DeclareLocalSpan<LuaType>(argCount, 256);
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldloc, argTypes);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetArgTypes))!);

            return argTypes;
        }

        private static LocalBuilder EmitDeclareIndexerArgCount(
            ILGenerator ilg, LocalBuilder keyType, LocalBuilder? valueType)
        {
            var argCount = ilg.DeclareLocal(typeof(int));
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetIndexerArgCount))!);
            if (valueType is not null)
            {
                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Add);
            }
            ilg.Emit(Stloc, argCount);

            return argCount;
        }

        private static LocalBuilder EmitDeclareIndexerArgTypes(
            ILGenerator ilg, LocalBuilder keyType, LocalBuilder? valueType, LocalBuilder argCount)
        {
            var argTypes = ilg.DeclareLocalSpan<LuaType>(argCount, 256);
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldloc, keyType);
            if (valueType is not null)
            {
                ilg.Emit(Ldloc, valueType);
            }
            else
            {
                ilg.Emit(Ldc_I4_M1);
            }
            ilg.Emit(Ldloc, argTypes);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetKeyTypes))!);

            return argTypes;
        }

        private static (LocalBuilder index, Label isNotMember) EmitDeclareMemberIndex(
            ILGenerator ilg, LocalBuilder keyType)
        {
            var index = ilg.DeclareLocal(typeof(int));

            var isNotMember = ilg.DefineLabel();

            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Ldc_I4_4);
            ilg.Emit(Bne_Un, isNotMember);  // Not short form
            {
                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);  // Lua state
                ilg.Emit(Ldc_I4_2);  // Key
                ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetMemberIndex))!);
                ilg.Emit(Stloc, index);
            }

            return (index, isNotMember);
        }

        private static LocalBuilder EmitDeclareSzArrayIndex(
            ILGenerator ilg, LocalBuilder keyType)
        {
            var index = ilg.DeclareLocal(typeof(int));
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetSzArrayIndex))!);
            ilg.Emit(Stloc, index);

            return index;
        }

        private static LocalBuilder EmitDeclareArrayIndices(
            ILGenerator ilg, LocalBuilder keyType, int rank)
        {
            var indices = ilg.DeclareLocal(typeof(Span<int>));
            ilg.Emit(Ldc_I4, 4 * rank);
            ilg.Emit(Conv_U);
            ilg.Emit(Localloc);
            ilg.Emit(Ldc_I4, rank);
            ilg.Emit(Newobj, typeof(Span<int>).GetConstructor(new[] { typeof(void*), typeof(int) })!);
            ilg.Emit(Stloc, indices);

            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Ldloc, indices);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetArrayIndices))!);

            return indices;
        }

        private static LocalBuilder EmitDeclareTypeArgs(
            ILGenerator ilg, LocalBuilder keyType)
        {
            var typeArgs = ilg.DeclareLocal(typeof(Type[]));
            ilg.Emit(Ldarg_0);
            ilg.Emit(Ldarg_1);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.GetTypeArgs))!);
            ilg.Emit(Stloc, typeArgs);

            return typeArgs;
        }

        private static void EmitIndexTypeArgs(
           ILGenerator ilg,
           Action<ILGenerator> getLuaKeyType,
           Action<ILGenerator, LocalBuilder> typeArgsAction)
        {
            var isUserdata = ilg.DefineLabel();
            var isNotTable = ilg.DefineLabel();

            getLuaKeyType(ilg);
            ilg.Emit(Ldc_I4_7);
            ilg.Emit(Beq_S, isUserdata);

            getLuaKeyType(ilg);
            ilg.Emit(Ldc_I4_5);
            ilg.Emit(Bne_Un, isNotTable);  // Not short form
            {
                var typeArgs = ilg.DeclareLocal(typeof(Type[]));

                ilg.MarkLabel(isUserdata);

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                getLuaKeyType(ilg);
                ilg.Emit(Call, MetamethodContext._constructTypeArgs);
                ilg.Emit(Stloc, typeArgs);

                typeArgsAction(ilg, typeArgs);
            }

            ilg.MarkLabel(isNotTable);
        }

        private static void EmitCallMethod(
            ILGenerator ilg, LocalBuilder? target, MethodBase method,
            LocalBuilder argCount, LocalBuilder argTypes, Label isInvalidCall)
        {
            var parameters = method.GetParameters();
            var args = parameters.Select(p => ilg.DeclareReusableLocal(p.ParameterType)).ToArray();

            {
                // Use the argument count bounds to filter out calls with too few arguments or too many arguments, for
                // fast overload checks.
                //
                // For optimal codegen, this is done with an unsigned comparison.

                var (minArgs, maxArgs) = method.GetArgCountBounds();

                ilg.Emit(Ldloc, argCount);
                ilg.Emit(Ldc_I4, minArgs);
                ilg.Emit(Sub);
                ilg.Emit(Ldc_I4, maxArgs - minArgs);
                ilg.Emit(Bgt_Un, isInvalidCall);  // Not short form
            }

            {
                // Fill in the arguments for the call.

                using var argIndex = ilg.DeclareReusableLocal(typeof(int));
                ilg.Emit(Ldloc, argCount);
                ilg.Emit(Neg);
                ilg.Emit(Stloc, argIndex);


                foreach (var (parameter, arg) in parameters.Zip(args))
                {
                    using var argType = ilg.DeclareReusableLocal(typeof(LuaType));
                    ilg.Emit(Ldloca, argTypes);
                    ilg.Emit(Ldloc, argIndex);
                    ilg.Emit(Neg);
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Sub);
                    ilg.Emit(Call, typeof(Span<LuaType>).GetProperty("Item")!.GetMethod!);
                    ilg.Emit(Ldind_I4);
                    ilg.Emit(Stloc, argType);

                    EmitLuaLoad(ilg, parameter.ParameterType, arg, argType, argIndex, isInvalidCall);

                    ilg.Emit(Ldloc, argIndex);
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Add);
                    ilg.Emit(Stloc, argIndex);
                }
            }

            if (target is not null)
            {
                ilg.Emit(Ldloc, target);
            }

            var returnType = method.GetReturnType();
            var value = returnType != typeof(void) ? ilg.DeclareReusableLocal(returnType) : null;

            if (method is ConstructorInfo constructor)
            {
                var isValueType = returnType.IsValueType;

                if (isValueType)
                {
                    ilg.Emit(Ldloca, value!);
                }
                foreach (var arg in args)
                {
                    ilg.Emit(Ldloc, arg);
                }
                ilg.Emit(isValueType ? Call : Newobj, constructor);
                if (!isValueType)
                {
                    ilg.Emit(Stloc, value!);
                }
            }
            else
            {
                foreach (var arg in args)
                {
                    ilg.Emit(Ldloc, arg);
                }
                ilg.Emit(target is not null ? Callvirt : Call, (MethodInfo)method);
                if (value is not null)
                {
                    ilg.Emit(Stloc, value);
                }
            }

            if (value is not null)
            {
                EmitLuaPush(ilg, returnType, value);
            }

            ilg.Emit(value is not null ? Ldc_I4_1 : Ldc_I4_0);
            ilg.Emit(Ret);

            foreach (var arg in args)
            {
                arg.Dispose();
            }
            value?.Dispose();
        }

        private static void EmitCallMethods(
            ILGenerator ilg, LocalBuilder? target, IReadOnlyList<MethodBase> methods,
            LocalBuilder argCount, LocalBuilder argTypes)
        {
            var nextMethods = ilg.DefineLabels(methods.Count);

            foreach (var (method, nextMethod) in methods.Zip(nextMethods))
            {
                EmitCallMethod(ilg, target, method, argCount, argTypes, nextMethod);

                ilg.MarkLabel(nextMethod);
            }
        }

        private static void EmitCallMethod(
            ILGenerator ilg, MethodBase clrMethod,
            Action<ILGenerator> getLuaArgCount,
            Action<ILGenerator, LocalBuilder> getLuaArgType,
            Action<ILGenerator, LocalBuilder> getLuaArgIndex,
            Action<ILGenerator, MethodBase, ILGeneratorExtensions.ReusableLocalBuilder[], LocalBuilder?> callClrMethod,
            Label isInvalidCall)
        {
            var parameters = clrMethod.GetParameters();
            var args = parameters.Select(p => ilg.DeclareReusableLocal(p.ParameterType)).ToArray();

            {
                // Use the argument count bounds to filter out calls with too few arguments or too many arguments. This
                // is done using an unsigned comparison for optimal codegen.

                var (minArgs, maxArgs) = clrMethod.GetArgCountBounds();

                getLuaArgCount(ilg);
                ilg.Emit(Ldc_I4, minArgs);
                ilg.Emit(Sub);
                ilg.Emit(Ldc_I4, maxArgs - minArgs);
                ilg.Emit(Bgt_Un, isInvalidCall);  // Not short form
            }

            {
                // Fill in the arguments for the call. We declare an argument index local in order to handle methods
                // with a variable number of arguments.

                using var argIndex = ilg.DeclareReusableLocal(typeof(int));
                ilg.Emit(Ldc_I4_1);  // Starts at 1 to match Lua convention
                ilg.Emit(Stloc, argIndex);

                for (var i = 0; i < parameters.Length; ++i)
                {
                    var parameter = parameters[i];
                    var parameterType = parameters[i].ParameterType;
                    var arg = args[i];

                    // If the parameter is a `params` array, then construct an array with the rest of the Lua arguments.

                    if (parameter.IsParams())
                    {
                        var elementType = parameterType.GetElementType()!;

                        // Create an empty array with a size equal to the number of remaining Lua arguments.

                        getLuaArgCount(ilg);
                        ilg.Emit(Ldloc, argIndex);
                        ilg.Emit(Sub);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Add);
                        ilg.Emit(Newarr, elementType);
                        ilg.Emit(Stloc, arg);

                        // Fill in the array using the remaining Lua arguments.

                        using var arrIndex = ilg.DeclareReusableLocal(typeof(int));
                        ilg.Emit(Ldc_I4_0);
                        ilg.Emit(Stloc, arrIndex);

                        var loopStart = ilg.DefineLabel();

                        ilg.Emit(Br, loopStart);  // Not short form
                        {
                            var loopBody = ilg.DefineAndMarkLabel();

                            using var temp = ilg.DeclareReusableLocal(elementType);

                            EmitLuaLoad(ilg, elementType, temp,
                                ilg => getLuaArgType(ilg, argIndex),
                                ilg => getLuaArgIndex(ilg, argIndex),
                                isInvalidCall);

                            ilg.Emit(Ldloc, argIndex);
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Add);
                            ilg.Emit(Stloc, argIndex);

                            ilg.Emit(Ldloc, arg);
                            ilg.Emit(Ldloc, arrIndex);
                            ilg.Emit(Ldloc, temp);
                            ilg.EmitStelem(elementType);

                            ilg.Emit(Ldloc, arrIndex);
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Add);
                            ilg.Emit(Stloc, arrIndex);

                            ilg.MarkLabel(loopStart);

                            ilg.Emit(Ldloc, arrIndex);
                            ilg.Emit(Ldloc, arg);
                            ilg.Emit(Ldlen);
                            ilg.Emit(Conv_I4);
                            ilg.Emit(Blt, loopBody);  // Not short form
                        }

                        break;
                    }

                    EmitLuaLoad(ilg, parameterType, arg,
                        ilg => getLuaArgType(ilg, argIndex),
                        ilg => getLuaArgIndex(ilg, argIndex),
                        isInvalidCall);
                }
            }

            var returnType = clrMethod.GetReturnType();
            if (returnType == typeof(void))
            {
                callClrMethod(ilg, clrMethod, args, null);

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            }
            else
            {
                using var temp = ilg.DeclareReusableLocal(returnType);

                callClrMethod(ilg, clrMethod, args, temp);

                EmitLuaPush(ilg, returnType, temp);
                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Ret);
            }
            
            foreach (var arg in args)
            {
                arg.Dispose();
            }
        }

        private static void EmitCallMethods(
            ILGenerator ilg, IReadOnlyList<MethodBase> methods,
            Action<ILGenerator> getLuaArgCount,
            Action<ILGenerator, LocalBuilder> getLuaArgType,
            Action<ILGenerator, LocalBuilder> getLuaArgIndex,
            Action<ILGenerator, MethodBase, ILGeneratorExtensions.ReusableLocalBuilder[], LocalBuilder?> callClrMethod)
        {
            var nextMethods = ilg.DefineLabels(methods.Count);

            for (var i = 0; i < methods.Count; ++i)
            {
                EmitCallMethod(ilg, methods[i],
                    getLuaArgCount,
                    getLuaArgType,
                    getLuaArgIndex,
                    callClrMethod,
                    nextMethods[i]);

                ilg.MarkLabel(nextMethods[i]);
            }
        }

        private void PushFunction(IntPtr state, string name, Action<ILGenerator, MetamethodContext> emit)
        {
            var context = new MetamethodContext(_environment, this);
            var method = new DynamicMethod(name, typeof(int), _metamethodParameterTypes, typeof(MetamethodContext));
            var ilg = method.GetILGenerator();

            emit(ilg, context);

            var function = (LuaCFunction)method.CreateDelegate(typeof(LuaCFunction), context);
            LuaCFunction protectedFunction = ProtectedFunction;
            _generatedFunctions.Add(protectedFunction);  // Prevent garbage collection

            lua_pushcfunction(state, protectedFunction);
            return;

            int ProtectedFunction(IntPtr state)
            {
                try
                {
                    return function(state);
                }
                catch (Exception ex)
                {
                    return luaL_error(state, $"unhandled CLR exception:\n{ex}");
                }
            }
        }
    }
}
