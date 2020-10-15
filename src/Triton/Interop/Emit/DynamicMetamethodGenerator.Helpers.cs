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
using Triton.Interop.Emit.Extensions;
using static System.Reflection.Emit.OpCodes;
using static Triton.Lua;

namespace Triton.Interop.Emit
{
    internal unsafe partial class DynamicMetamethodGenerator
    {
        private static readonly MethodInfo _lua_pushboolean = typeof(Lua).GetMethod(nameof(lua_pushboolean))!;
        private static readonly MethodInfo _lua_pushinteger = typeof(Lua).GetMethod(nameof(lua_pushinteger))!;
        private static readonly MethodInfo _lua_pushlightuserdata = typeof(Lua).GetMethod(nameof(lua_pushlightuserdata))!;
        private static readonly MethodInfo _lua_pushnil = typeof(Lua).GetMethod(nameof(lua_pushnil))!;
        private static readonly MethodInfo _lua_pushnumber = typeof(Lua).GetMethod(nameof(lua_pushnumber))!;
        private static readonly MethodInfo _lua_pushstring = typeof(Lua).GetMethod(nameof(lua_pushstring))!;

        private static readonly MethodInfo _lua_tolstring = typeof(Lua).GetMethod(nameof(lua_tolstring))!;

        private static readonly MethodInfo _lua_type = typeof(Lua).GetMethod(nameof(lua_type))!;

        private static readonly MethodInfo _charToString =
            typeof(char).GetMethod(nameof(char.ToString), BindingFlags.Public | BindingFlags.Static)!;

        private static readonly MethodInfo _luaValuePush =
            typeof(LuaValue).GetMethod(nameof(LuaValue.Push), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly MethodInfo _luaObjectPush =
            typeof(LuaObject).GetMethod(nameof(LuaObject.Push), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly MethodInfo _luaEnvironmentPushClrEntity =
            typeof(LuaEnvironment)
                .GetMethod(nameof(LuaEnvironment.PushClrEntity), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly MethodInfo _luaEnvironmentLoadClrEntity =
            typeof(LuaEnvironment)
                .GetMethod(nameof(LuaEnvironment.LoadClrEntity), BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Interns the given members' names in the Lua state for efficient string comparisons.
        protected static IReadOnlyList<nint> InternMemberNames(lua_State* state, IReadOnlyList<MemberInfo> members)
        {
            lua_createtable(state, members.Count, 0);

            var result = new List<nint>();
            for (var i = 0; i < members.Count; ++i)
            {
                var ptr = lua_pushstring(state, members[i].Name);
                lua_rawseti(state, -2, i + 1);

                result.Add((nint)ptr);
            }

            _ = luaL_ref(state, LUA_REGISTRYINDEX);

            return result;
        }

        // Emits code to push a non-byref local variable onto the Lua stack.
        protected static void EmitLuaPush(ILGenerator ilg, LocalBuilder value)
        {
            // Collapse the type into a simplified form. This greatly simplifies the code generation logic.

            var type = value.LocalType;
            if (type.IsPointer)
            {
                type = typeof(IntPtr);
            }

            var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
            var isNullableType = nonNullableType != type;
            if (nonNullableType.IsEnum)
            {
                nonNullableType = Enum.GetUnderlyingType(nonNullableType);
            }

            // Emit code to push the local variable onto the Lua stack. `null` needs to be handled if it is a valid
            // value (i.e., the type is a non-value type or is `Nullable<T>`).

            if (!type.IsValueType || isNullableType)
            {
                var skip = ilg.DefineLabel();
                var isNotNull = ilg.DefineLabel();

                if (isNullableType)
                {
                    ilg.Emit(Ldloca, value);
                    ilg.Emit(Call, type.GetProperty(nameof(Nullable<int>.HasValue))!.GetMethod!);
                }
                else if (!type.IsValueType)
                {
                    ilg.Emit(Ldloc, value);
                }
                ilg.Emit(Brtrue_S, isNotNull);
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Call, _lua_pushnil);
                    ilg.Emit(Br_S, skip);
                }

                ilg.MarkLabel(isNotNull);
                {
                    EmitLuaPush(ilg, value);
                }

                ilg.MarkLabel(skip);
            }
            else
            {
                EmitLuaPush(ilg, value);
            }

            return;

            void EmitLuaPush(ILGenerator ilg, LocalBuilder value)
            {
                if (nonNullableType.IsLuaValue())
                {
                    EmitLoad(ilg, value);
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Call, _luaValuePush);
                }
                else if (nonNullableType.IsLuaObject())
                {
                    EmitLoad(ilg, value);
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Call, _luaObjectPush);
                }
                else if (nonNullableType.IsClrObject())
                {
                    ilg.Emit(Ldarg_1);  // Lua environment
                    ilg.Emit(Ldarg_0);  // Lua state
                    EmitLoad(ilg, value);
                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Call, _luaEnvironmentPushClrEntity);
                }
                else
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    EmitLoad(ilg, value);
                    ilg.Emit(Call, true switch
                    {
                        _ when nonNullableType.IsBoolean() => _lua_pushboolean,
                        _ when nonNullableType.IsPointer() => _lua_pushlightuserdata,
                        _ when nonNullableType.IsInteger() => _lua_pushinteger,
                        _ when nonNullableType.IsNumber()  => _lua_pushnumber,
                        _ when nonNullableType.IsString()  => _lua_pushstring,
                        _                                  => throw new InvalidOperationException()
                    });

                    if (nonNullableType.IsString())
                    {
                        ilg.Emit(Pop);
                    }
                }
            }

            void EmitLoad(ILGenerator ilg, LocalBuilder value)
            {
                if (isNullableType)
                {
                    ilg.Emit(Ldloca, value);
                    ilg.Emit(Call, type.GetProperty(nameof(Nullable<int>.Value))!.GetMethod!);
                }
                else
                {
                    ilg.Emit(Ldloc, value);
                }

                // Convert the value into a suitable format. For example, `lua_pushinteger` and `lua_pushnumber` take
                // `int64` and `float64`, respectively, so smaller ints and floats need to be widened.

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
            }
        }

        // Emits code to declare a `target` local variable, which is the target of the metamethod.
        protected static LocalBuilder EmitDeclareTarget(ILGenerator ilg, Type objType)
        {
            var isStruct = objType.IsClrStruct();

            // For CLR structs, we need to declare a byref local variable. This allows the struct to be mutated.

            var target = ilg.DeclareLocal(isStruct ? objType.MakeByRefType() : objType);
            ilg.Emit(Ldarg_1);  // Lua environment
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldc_I4_1);  // Target
            ilg.Emit(Call, _luaEnvironmentLoadClrEntity);
            ilg.Emit(isStruct ? Unbox : Unbox_Any, objType);

            return target;
        }

        // Emits code to declare a `keyType` local variable, which is the type of the key in the `__index` and
        // `__newindex` metamethods.
        protected static LocalBuilder EmitDeclareKeyType(ILGenerator ilg)
        {
            var keyType = ilg.DeclareLocal(typeof(LuaType));
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Call, _lua_type);
            ilg.Emit(Stloc, keyType);

            return keyType;
        }

        // Emits code to declare a `keyPtr` local variable, which is the pointer of the string key in the `__index`
        // and `__newindex` metamethods.
        protected static LocalBuilder EmitDeclareKeyPtr(ILGenerator ilg)
        {
            var keyPtr = ilg.DeclareLocal(typeof(nint));
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Ldc_I4_0);
            ilg.Emit(Conv_I);
            ilg.Emit(Call, _lua_tolstring);
            ilg.Emit(Stloc, keyPtr);

            return keyPtr;
        }
    }
}
