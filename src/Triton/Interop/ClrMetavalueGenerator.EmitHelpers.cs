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

namespace Triton.Interop
{
    internal partial class ClrMetavalueGenerator
    {
        private static readonly PropertyInfo _stringLength =
            typeof(string).GetProperty(nameof(string.Length))!;

        private static readonly PropertyInfo _stringIndexer =
            typeof(string).GetProperty("Chars")!;

        private static readonly MethodInfo _charToString =
            typeof(char).GetMethod(nameof(char.ToString), Public | Static)!;

        private static readonly Type[] _metamethodParameterTypes = new[] { typeof(MetamethodContext), typeof(IntPtr) };

        private readonly List<LuaCFunction> _generatedMetamethods = new List<LuaCFunction>();

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
            ILGenerator ilg, Type clrType,
            Action<ILGenerator> getLuaType,
            Action<ILGenerator> getLuaIndex,
            Label isInvalidLuaValue)
        {
            clrType = clrType.Simplify();

            // Verify that the Lua type is correct. This is not required for the `LuaValue` type, since it is a tagged
            // union that supports all Lua types.

            if (clrType != typeof(LuaValue))
            {
                if (clrType == typeof(LuaObject))
                {
                    // `LuaObject` can correspond to three different Lua types

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
                else
                {
                    getLuaType(ilg);
                    ilg.Emit(true switch
                    {
                        _ when clrType == typeof(bool)        => Ldc_I4_1,
                        _ when clrType.IsLightUserdata()      => Ldc_I4_2,
                        _ when clrType.IsInteger()            => Ldc_I4_3,
                        _ when clrType.IsNumber()             => Ldc_I4_3,
                        _ when clrType.IsString()             => Ldc_I4_4,
                        _ when clrType == typeof(LuaTable)    => Ldc_I4_5,
                        _ when clrType == typeof(LuaFunction) => Ldc_I4_6,
                        _ when clrType.IsClrObject()          => Ldc_I4_7,
                        _ when clrType == typeof(LuaThread)   => Ldc_I4_8,
                        _                                     => throw new InvalidOperationException()
                    });
                    ilg.Emit(Bne_Un, isInvalidLuaValue);  // Not short form

                    if (clrType.IsInteger())
                    {
                        ilg.Emit(Ldarg_1);
                        getLuaIndex(ilg);
                        ilg.Emit(Call, _lua_isinteger);
                        ilg.Emit(Brfalse, isInvalidLuaValue);  // Not short form
                    }
                }
            }

            if (clrType == typeof(LuaValue) || clrType.IsLuaObject() || clrType.IsClrObject())
            {
                ilg.Emit(Ldarg_0);  // Required for `MetamethodContext` methods
            }

            ilg.Emit(Ldarg_1);
            getLuaIndex(ilg);

            if (clrType == typeof(LuaValue) || clrType.IsLuaObject())
            {
                getLuaType(ilg);
            }

            ilg.Emit(Call, true switch
            {
                _ when clrType == typeof(LuaValue) => MetamethodContext._loadValue,
                _ when clrType.IsBoolean()         => _lua_toboolean,
                _ when clrType.IsLightUserdata()   => _lua_touserdata,
                _ when clrType.IsInteger()         => _lua_tointeger,
                _ when clrType.IsNumber()          => _lua_tonumber,
                _ when clrType.IsString()          => _lua_tostring,
                _ when clrType.IsLuaObject()       => MetamethodContext._loadLuaObject,
                _ when clrType.IsClrObject()       => MetamethodContext._loadClrEntity,
                _                                  => throw new InvalidOperationException()
            });

            if (clrType.IsInteger() && clrType != typeof(long) && clrType != typeof(ulong))
            {
                // Verify that the integer can be converted without overflow or underflow.

                using var temp = ilg.DeclareReusableLocal(typeof(long));
                using var result = ilg.DeclareReusableLocal(clrType);

                ilg.Emit(Stloc, temp);

                ilg.BeginExceptionBlock();
                {
                    ilg.Emit(Ldloc, temp);
                    ilg.Emit(true switch
                    {
                        _ when clrType == typeof(byte)   => Conv_Ovf_U1,
                        _ when clrType == typeof(ushort) => Conv_Ovf_U2,
                        _ when clrType == typeof(uint)   => Conv_Ovf_U4,
                        _ when clrType == typeof(sbyte)  => Conv_Ovf_I1,
                        _ when clrType == typeof(short)  => Conv_Ovf_I2,
                        _                                => Conv_Ovf_I4
                    });
                    ilg.Emit(Stloc, result);
                }

                ilg.BeginCatchBlock(typeof(OverflowException));
                {
                    ilg.Emit(Leave, isInvalidLuaValue);  // Not short form
                }

                ilg.EndExceptionBlock();

                ilg.Emit(Ldloc, result);
            }
            else if (clrType == typeof(float))
            {
                ilg.Emit(Conv_R4);
            }
            else if (clrType == typeof(char))
            {
                // Verify that the string has exactly one character.

                using var temp = ilg.DeclareReusableLocal(typeof(string));

                ilg.Emit(Stloc, temp);

                ilg.Emit(Ldloc, temp);
                ilg.Emit(Call, _stringLength.GetMethod!);
                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Bne_Un, isInvalidLuaValue);  // Not short form

                ilg.Emit(Ldloc, temp);
                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Call, _stringIndexer.GetMethod!);
            }
            else if (clrType.IsLuaObject() && clrType != typeof(LuaObject))
            {
                ilg.Emit(Castclass, clrType);
            }
            else if (clrType.IsClrObject())
            {
                // Verify that the type is correct.

                var isStruct = clrType.IsClrStruct();
                using var temp = ilg.DeclareReusableLocal(isStruct ? clrType.MakeByRefType() : clrType);

                ilg.Emit(Isinst, clrType);
                ilg.Emit(Stloc, temp);

                ilg.Emit(Ldloc, temp);
                ilg.Emit(Brfalse, isInvalidLuaValue);  // Not short form

                ilg.Emit(Ldloc, temp);
            }
        }

        private static void EmitLuaPush(
            ILGenerator ilg, Type clrType,
            Action<ILGenerator> getClrValue)
        {
            clrType = clrType.Simplify();

            if (clrType == typeof(LuaValue) || clrType.IsLuaObject() || clrType.IsClrObject())
            {
                ilg.Emit(Ldarg_0);  // Required for `MetamethodContext` methods
            }

            ilg.Emit(Ldarg_1);
            getClrValue(ilg);

            if (clrType.IsSignedInteger() && clrType != typeof(long))
            {
                ilg.Emit(Conv_I8);
            }
            else if (clrType.IsUnsignedInteger() && clrType != typeof(ulong))
            {
                ilg.Emit(Conv_U8);
            }
            else if (clrType == typeof(float))
            {
                ilg.Emit(Conv_R8);
            }
            else if (clrType == typeof(char))
            {
                ilg.Emit(Call, _charToString);
            }
            else if (clrType.IsClrStruct())
            {
                ilg.Emit(Box, clrType);
            }

            ilg.Emit(Call, true switch
            {
                _ when clrType == typeof(LuaValue) => MetamethodContext._pushValue,
                _ when clrType.IsBoolean()         => _lua_pushboolean,
                _ when clrType.IsLightUserdata()   => _lua_pushlightuserdata,
                _ when clrType.IsInteger()         => _lua_pushinteger,
                _ when clrType.IsNumber()          => _lua_pushnumber,
                _ when clrType.IsString()          => _lua_pushstring,
                _ when clrType.IsLuaObject()       => MetamethodContext._pushLuaObject,
                _ when clrType.IsClrObject()       => MetamethodContext._pushClrEntity,
                _                                  => throw new InvalidOperationException()
            });

            if (clrType.IsString())
            {
                ilg.Emit(Pop);  // Pop the return value
            }
        }

        private static LocalBuilder EmitDeclareTarget(ILGenerator ilg, Type objType)
        {
            var isStruct = objType.IsClrStruct();
            var target = ilg.DeclareLocal(isStruct ? objType.MakeByRefType() : objType);

            ilg.Emit(Ldarg_0);
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_1);
            ilg.Emit(Call, MetamethodContext._loadClrEntity);
            ilg.Emit(isStruct ? Unbox : Unbox_Any, objType);
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

        private static void EmitIndexMembers(
            ILGenerator ilg, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator> getKeyLuaType,
            Action<ILGenerator, FieldInfo> getFieldValue,
            Action<ILGenerator, PropertyInfo> getPropertyValue)
        {
            var isNonReadableProperty = LazyEmitErrorMemberName(ilg, "attempt to get non-readable property '{0}'");
            var isByRefLikeProperty = LazyEmitErrorMemberName(ilg, "attempt to get byref-like property '{0}'");

            var skip = ilg.DefineLabel();
            var isInvalidMember = ilg.DefineLabel();

            ilg.Emit(Br_S, skip);
            {
                ilg.MarkLabel(isInvalidMember);

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            }

            ilg.MarkLabel(skip);

            EmitSwitchMembers(ilg, members,
                getKeyLuaType,
                (ilg, member) =>
                {
                    switch (member)
                    {
                        case FieldInfo field:
                            var fieldType = field.FieldType;

                            EmitLuaPush(ilg, fieldType,
                                ilg => getFieldValue(ilg, field));
                            break;

                        case PropertyInfo property:
                            var propertyType = property.PropertyType;
                            var isByRef = propertyType.IsByRef;
                            if (isByRef)
                            {
                                propertyType = propertyType.GetElementType()!;
                            }

                            if (property.GetMethod?.IsPublic != true)
                            {
                                ilg.Emit(Br, isNonReadableProperty.Value);  // Not short form
                                return;
                            }

                            if (propertyType.IsByRefLike)
                            {
                                ilg.Emit(Br, isByRefLikeProperty.Value);  // Not short form
                                return;
                            }

                            EmitLuaPush(ilg, propertyType,
                                ilg => getPropertyValue(ilg, property));
                            break;

                        default:
                            throw new InvalidOperationException();
                    }

                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Ret);
                },
                isInvalidMember);
        }

        private void EmitIndexTypeArgs(
           ILGenerator ilg,
           Action<ILGenerator> getKeyLuaType,
           Action<ILGenerator, LocalBuilder> typeArgsAction)
        {
            var isUserdata = ilg.DefineLabel();
            var isNotTable = ilg.DefineLabel();

            getKeyLuaType(ilg);
            ilg.Emit(Ldc_I4_7);
            ilg.Emit(Beq_S, isUserdata);

            getKeyLuaType(ilg);
            ilg.Emit(Ldc_I4_5);
            ilg.Emit(Bne_Un, isNotTable);  // Not short form
            {
                var typeArgs = ilg.DeclareLocal(typeof(Type[]));

                ilg.MarkLabel(isUserdata);

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                getKeyLuaType(ilg);
                ilg.Emit(Call, MetamethodContext._loadClrTypes);
                ilg.Emit(Stloc, typeArgs);

                typeArgsAction(ilg, typeArgs);
            }

            ilg.MarkLabel(isNotTable);
        }

        private static void EmitNewIndexMembers(
            ILGenerator ilg, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator> getKeyLuaType,
            Action<ILGenerator> getValueLuaType,
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
                getKeyLuaType,
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

                            EmitLuaLoad(ilg, fieldType,
                                getValueLuaType,
                                ilg => ilg.Emit(Ldc_I4_3),
                                invalidFieldValue.Value);

                            {
                                var isStruct = fieldType.IsClrStruct();
                                using var temp = ilg.DeclareReusableLocal(
                                    isStruct ? fieldType.MakeByRefType() : fieldType);
                                ilg.Emit(Stloc, temp);

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

                            EmitLuaLoad(ilg, propertyType,
                                getValueLuaType,
                                ilg => ilg.Emit(Ldc_I4_3),
                                invalidPropertyValue.Value);

                            {
                                var isStruct = propertyType.IsClrStruct();
                                using var temp = ilg.DeclareReusableLocal(
                                    isStruct ? propertyType.MakeByRefType() : propertyType);
                                ilg.Emit(Stloc, temp);

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
            ILGenerator ilg, MethodBase clrMethodOrConstructor,
            Action<ILGenerator> getLuaArgCount,
            Action<ILGenerator, LocalBuilder> getLuaArgType,
            Action<ILGenerator, LocalBuilder> getLuaArgIndex,
            Action<ILGenerator, MethodBase, LocalBuilder[]> callClrMethod,
            Label isInvalidCall)
        {
            // Use the argument count bounds to filter out calls with too few arguments or too many arguments. This is
            // done using an unsigned comparison.

            var (minArgs, maxArgs) = clrMethodOrConstructor.GetArgCountBounds();

            getLuaArgCount(ilg);
            ilg.Emit(Ldc_I4, minArgs);
            ilg.Emit(Sub);
            ilg.Emit(Ldc_I4, maxArgs - minArgs);
            ilg.Emit(Bgt_Un, isInvalidCall);  // Not short form

            // Declare local variables for the arguments.

            var parameters = clrMethodOrConstructor.GetParameters();
            var args = parameters.Select(p => ilg.DeclareReusableLocal(p.ParameterType)).ToArray();

            var reusableLocals = parameters.Select(p => ilg.DeclareReusableLocal(p.ParameterType)).ToArray();
            var locals = reusableLocals.Select(l => (LocalBuilder)l).ToArray();

            for (var i = 0; i < parameters.Length; ++i)
            {
                var parameterType = parameters[i].ParameterType;

                using var temp = ilg.DeclareReusableLocal(typeof(int));
                
                ilg.Emit(Ldc_I4, i + 1);
                ilg.Emit(Stloc, temp);

                EmitLuaLoad(ilg, parameterType,
                    ilg => getLuaArgType(ilg, temp),
                    ilg => getLuaArgIndex(ilg, temp),
                    isInvalidCall);
                ilg.Emit(Stloc, reusableLocals[i]);
            }

            switch (clrMethodOrConstructor)
            {
                case ConstructorInfo constructor:
                    EmitLuaPush(ilg, constructor.DeclaringType!,
                        ilg => callClrMethod(ilg, clrMethodOrConstructor, locals));
                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Ret);
                    break;

                case MethodInfo method:
                    callClrMethod(ilg, clrMethodOrConstructor, locals);
                    break;
            }

            // TODO: return values?

            var results = 0;
            ilg.Emit(Ldc_I4, results);
            ilg.Emit(Ret);

            foreach (var reusableLocal in reusableLocals)
            {
                reusableLocal.Dispose();
            }
        }

        private static void EmitCallMethods(
            ILGenerator ilg, IReadOnlyList<MethodBase> methods,
            Action<ILGenerator> getLuaArgCount,
            Action<ILGenerator, LocalBuilder> getLuaArgType,
            Action<ILGenerator, LocalBuilder> getLuaArgIndex,
            Action<ILGenerator, MethodBase, LocalBuilder[]> callMethod)
        {
            var nextMethods = ilg.DefineLabels(methods.Count);

            for (var i = 0; i < methods.Count; ++i)
            {
                EmitCallMethod(ilg, methods[i],
                    getLuaArgCount,
                    getLuaArgType,
                    getLuaArgIndex,
                    callMethod,
                    nextMethods[i]);

                ilg.MarkLabel(nextMethods[i]);
            }
        }

        private LuaCFunction GenerateMetamethod(string name, Action<ILGenerator, MetamethodContext> emitAction)
        {
            var context = new MetamethodContext(_environment);
            var method = new DynamicMethod(name, typeof(int), _metamethodParameterTypes, typeof(MetamethodContext));
            var ilg = method.GetILGenerator();

            emitAction(ilg, context);

            var metamethod = (LuaCFunction)method.CreateDelegate(typeof(LuaCFunction), context);
            LuaCFunction protectedMetamethod = ProtectedMetamethod;
            _generatedMetamethods.Add(protectedMetamethod);  // Prevent garbage collection
            return protectedMetamethod;

            int ProtectedMetamethod(IntPtr state)
            {
                try
                {
                    return metamethod(state);
                }
                catch (Exception ex)
                {
                    return luaL_error(state, $"unhandled CLR exception:\n{ex}");
                }
            }
        }
    }
}
