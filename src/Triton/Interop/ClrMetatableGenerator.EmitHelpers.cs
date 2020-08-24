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
        private static readonly PropertyInfo _stringLength =
            typeof(string).GetProperty(nameof(string.Length))!;

        private static readonly PropertyInfo _stringIndexer =
            typeof(string).GetProperty("Chars")!;

        private static readonly MethodInfo _charToString =
            typeof(char).GetMethod(nameof(char.ToString), Public | Static)!;

        private static readonly Type[] _metamethodParameterTypes = new[] { typeof(MetamethodContext), typeof(IntPtr) };

        private readonly List<LuaCFunction> _generatedFunctions = new List<LuaCFunction>();

        private static Lazy<Label> LazyEmitErrorMemberName(ILGenerator ilg, string format) =>
            new Lazy<Label>(() =>
            {
                var skip = ilg.DefineLabel();
                var isError = ilg.DefineLabel();

                ilg.Emit(Br_S, skip);
                {
                    ilg.MarkLabel(isError);

                    ilg.Emit(Ldarg_0);
                    ilg.Emit(Ldarg_1);
                    ilg.Emit(Ldstr, format);
                    ilg.Emit(Call, MetamethodContext._errorMemberName);
                    ilg.Emit(Ret);
                }

                ilg.MarkLabel(skip);

                return isError;
            });

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

        private static void EmitLuaPush(
            ILGenerator ilg, Type clrType, LocalBuilder temp)
        {
            Debug.Assert(temp.LocalType == clrType, "Temporary should match type");
            //Debug.Assert(!clrType.IsByRefLike);

            // Unpack the type into the non-byref type, then the non-nullable type, and finally the underlying
            // enumeration type.

            var isByRefType = clrType.IsByRef;
            var nonByRefType = isByRefType ? clrType.GetElementType()! : clrType;

            var nonNullableType = Nullable.GetUnderlyingType(nonByRefType) ?? nonByRefType;
            var isNullableType = nonNullableType != nonByRefType;

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
                    var isNull = ilg.DefineLabel();
                    var isNotNull = ilg.DefineLabel();

                    if (isNotValueType)
                    {
                        ilg.Emit(Ldloc, temp);
                        ilg.Emit(isNullableType ? Brfalse_S : Brtrue_S, isNullableType ? isNull : isNotNull);
                    }

                    if (isNullableType)
                    {
                        ilg.Emit(isByRefType ? Ldloc : Ldloca, temp);
                        ilg.Emit(Call, nonByRefType.GetProperty(nameof(Nullable<int>.HasValue))!.GetMethod!);
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

                ilg.Emit(Ldarg_1);

                if (!isNullableType)
                {
                    ilg.Emit(Ldloc, temp);

                    if (isByRefType)
                    {
                        ilg.EmitLdind(nonByRefType);
                    }
                }
                else
                {
                    ilg.Emit(isByRefType ? Ldloc : Ldloca, temp);
                    ilg.Emit(Call, nonByRefType.GetProperty(nameof(Nullable<int>.Value))!.GetMethod!);
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

        private static LocalBuilder EmitDeclareTarget(ILGenerator ilg, Type objType)
        {
            var isByRefType = objType.IsClrStruct();
            var target = ilg.DeclareLocal(isByRefType ? objType.MakeByRefType() : objType);

            ilg.Emit(Ldarg_0);
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_1);
            ilg.Emit(Call, MetamethodContext._loadClrEntity);
            ilg.Emit(isByRefType ? Unbox : Unbox_Any, objType);
            ilg.Emit(Stloc, target);

            return target;
        }

        private static LocalBuilder EmitDeclareKeyType(ILGenerator ilg)
        {
            var keyType = ilg.DeclareLocal(typeof(LuaType));

            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_2);
            ilg.Emit(Call, _lua_type);
            ilg.Emit(Stloc, keyType);

            return keyType;
        }

        private static LocalBuilder EmitDeclareValueType(ILGenerator ilg)
        {
            var valueType = ilg.DeclareLocal(typeof(LuaType));

            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_3);
            ilg.Emit(Call, _lua_type);
            ilg.Emit(Stloc, valueType);

            return valueType;
        }

        private static LocalBuilder EmitDeclareArgCount(ILGenerator ilg)
        {
            var argCount = ilg.DeclareLocal(typeof(int));

            ilg.Emit(Ldarg_1);
            ilg.Emit(Call, _lua_gettop);
            ilg.Emit(Stloc, argCount);

            return argCount;
        }

        private static (LocalBuilder numKeys, LocalBuilder keyTypes) EmitFlattenKey(
            ILGenerator ilg, LocalBuilder keyType)
        {
            var numKeys = ilg.DeclareLocal(typeof(int));
            var keyTypes = ilg.DeclareLocal(typeof(LuaType*));

            {
                // Determine the number of keys and set up the key types.

                var isNotTable = ilg.DefineLabel();
                var skip = ilg.DefineLabel();

                ilg.Emit(Ldloc, keyType);
                ilg.Emit(Ldc_I4_5);
                ilg.Emit(Bne_Un_S, isNotTable);
                {
                    ilg.Emit(Ldarg_1);  // Lua state
                    ilg.Emit(Ldc_I4_2);  // Key
                    ilg.Emit(Call, _lua_rawlen);
                    ilg.Emit(Conv_I4);
                    ilg.Emit(Br_S, skip);
                }

                ilg.MarkLabel(isNotTable);
                {
                    ilg.Emit(Ldc_I4_1);
                }

                ilg.MarkLabel(skip);

                ilg.Emit(Dup);
                ilg.Emit(Stloc, numKeys);

                ilg.Emit(Conv_U);
                ilg.Emit(Ldc_I4_4);
                ilg.Emit(Mul_Ovf_Un);
                ilg.Emit(Localloc);
                ilg.Emit(Stloc, keyTypes);
            }

            {
                // Push the keys onto the stack while filling in the key types.

                var isNotTable = ilg.DefineLabel();
                var skip = ilg.DefineLabel();

                ilg.Emit(Ldloc, keyType);
                ilg.Emit(Ldc_I4_5);
                ilg.Emit(Bne_Un_S, isNotTable);
                {
                    using var tableIndex = ilg.DeclareReusableLocal(typeof(int));
                    ilg.Emit(Ldc_I4_1);  // 1-based indices
                    ilg.Emit(Stloc, tableIndex);

                    var loopHead = ilg.DefineLabel();

                    ilg.Emit(Br_S, loopHead);
                    {
                        var loopBody = ilg.DefineAndMarkLabel();

                        ilg.Emit(Ldloc, keyTypes);
                        ilg.Emit(Ldloc, tableIndex);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Sub);
                        ilg.Emit(Conv_I);
                        ilg.Emit(Ldc_I4_4);
                        ilg.Emit(Mul);
                        ilg.Emit(Add);
                        ilg.Emit(Ldarg_1);  // Lua state
                        ilg.Emit(Ldc_I4_2);  // Key
                        ilg.Emit(Ldloc, tableIndex);
                        ilg.Emit(Conv_I8);
                        ilg.Emit(Call, _lua_rawgeti);
                        ilg.Emit(Stind_I4);

                        ilg.Emit(Ldloc, tableIndex);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Add);
                        ilg.Emit(Stloc, tableIndex);

                        ilg.MarkLabel(loopHead);

                        ilg.Emit(Ldloc, tableIndex);
                        ilg.Emit(Ldloc, numKeys);
                        ilg.Emit(Ble_S, loopBody);
                    }

                    ilg.Emit(Br_S, skip);
                }

                ilg.MarkLabel(isNotTable);
                {
                    ilg.Emit(Ldarg_1);  // Lua state
                    ilg.Emit(Ldc_I4_2);  // Key
                    ilg.Emit(Call, _lua_pushvalue);

                    ilg.Emit(Ldloc, keyTypes);
                    ilg.Emit(Ldloc, keyType);
                    ilg.Emit(Stind_I4);
                }

                ilg.MarkLabel(skip);
            }

            return (numKeys, keyTypes);
        }

        private static void EmitSwitchMembers(
            ILGenerator ilg, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator> getKeyLuaType,
            Action<ILGenerator, MemberInfo> memberAction,
            Label isInvalidMember)
        {
            var isNotString = ilg.DefineLabel();

            getKeyLuaType(ilg);
            ilg.Emit(Ldc_I4_4);
            ilg.Emit(Bne_Un, isNotString);  // Not short form
            {
                var cases = ilg.DefineLabels(members.Count);

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Call, MetamethodContext._matchMemberName);
                ilg.Emit(Switch, cases);

                ilg.Emit(Br, isInvalidMember);  // Not short form

                for (var i = 0; i < members.Count; ++i)
                {
                    ilg.MarkLabel(cases[i]);

                    memberAction(ilg, members[i]);
                }
            }

            ilg.MarkLabel(isNotString);
        }

        private void EmitIndexTypeArgs(
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
                ilg.Emit(Call, MetamethodContext._loadClrTypes);
                ilg.Emit(Stloc, typeArgs);

                typeArgsAction(ilg, typeArgs);
            }

            ilg.MarkLabel(isNotTable);
        }

        private static void EmitNewIndexMembers(
            ILGenerator ilg, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator> getLuaKeyType,
            Action<ILGenerator> getLuaValueType,
            Action<ILGenerator, FieldInfo, LocalBuilder> setFieldValue,
            Action<ILGenerator, PropertyInfo, LocalBuilder> setPropertyValue)
        {
            var isConstField = LazyEmitErrorMemberName(ilg, "attempt to set const field '{0}'");
            var isNonWritableField = LazyEmitErrorMemberName(ilg, "attempt to set non-writable field '{0}'");
            var invalidFieldValue = LazyEmitErrorMemberName(ilg, "attempt to set field '{0}' with invalid value");

            var isEvent = LazyEmitErrorMemberName(ilg, "attempt to set event '{0}'");

            var isNonWritableProperty = LazyEmitErrorMemberName(ilg, "attempt to set non-writable property '{0}'");
            var isByRefLikeProperty = LazyEmitErrorMemberName(ilg, "attempt to set byref-like property '{0}'");
            var invalidPropertyValue = LazyEmitErrorMemberName(ilg, "attempt to set property '{0}' with invalid value");

            var isMethod = LazyEmitErrorMemberName(ilg, "attempt to set method '{0}'");

            var isNestedType = LazyEmitErrorMemberName(ilg, "attempt to set nested type '{0}'");

            var isInvalidMember = LazyEmitErrorMemberName(ilg, "attempt to set invalid member '{0}'");

            EmitSwitchMembers(ilg, members,
                getLuaKeyType,
                (ilg, member) =>
                {
                    switch (member)
                    {
                        case FieldInfo field:
                            var fieldType = field.FieldType;

                            if (field.IsLiteral)
                            {
                                ilg.Emit(Br, isConstField.Value);  // Not short form
                                return;
                            }

                            if (field.IsInitOnly)
                            {
                                ilg.Emit(Br, isNonWritableField.Value);  // Not short form
                                return;
                            }

                            {
                                using var temp = ilg.DeclareReusableLocal(fieldType);

                                EmitLuaLoad(ilg, fieldType, temp,
                                    getLuaValueType,
                                    ilg => ilg.Emit(Ldc_I4_3),
                                    invalidFieldValue.Value);
                                setFieldValue(ilg, field, temp);
                            }
                            break;

                        case EventInfo @event:
                            ilg.Emit(Br, isEvent.Value);  // Not short form
                            return;

                        case PropertyInfo property:
                            var propertyType = property.PropertyType;
                            var isByRef = property.PropertyType.IsByRef;
                            if (isByRef)
                            {
                                propertyType = propertyType.GetElementType()!;
                            }

                            if ((isByRef ? property.GetMethod : property.SetMethod)?.IsPublic != true)
                            {
                                ilg.Emit(Br, isNonWritableProperty.Value);  // Not short form
                                return;
                            }

                            if (propertyType.IsByRefLike)
                            {
                                ilg.Emit(Br, isByRefLikeProperty.Value);  // Not short form
                                return;
                            }

                            {
                                using var temp = ilg.DeclareReusableLocal(propertyType);

                                EmitLuaLoad(ilg, propertyType, temp,
                                    getLuaValueType,
                                    ilg => ilg.Emit(Ldc_I4_3),
                                    invalidPropertyValue.Value);
                                setPropertyValue(ilg, property, temp);
                            }
                            break;

                        case MethodInfo method:
                            ilg.Emit(Br, isMethod.Value);  // Not short form
                            return;

                        case Type nestedType:
                            ilg.Emit(Br, isNestedType.Value);  // Not short form
                            return;

                        default:
                            throw new NotImplementedException();
                    }

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                },
                isInvalidMember.Value);
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
