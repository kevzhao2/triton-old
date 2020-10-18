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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Triton.Interop.Emit.Extensions;
using static System.Reflection.Emit.OpCodes;
using static Triton.Lua;

namespace Triton.Interop.Emit
{
    internal unsafe partial class DynamicMetavalueGenerator
    {
        private static readonly MethodInfo _lua_tolstring = typeof(Lua).GetMethod(nameof(lua_tolstring))!;

        private static readonly MethodInfo _lua_type = typeof(Lua).GetMethod(nameof(lua_type))!;

        private static readonly MethodInfo _luaEnvironmentLoadClrEntity =
            typeof(LuaEnvironment)
                .GetMethod(nameof(LuaEnvironment.LoadClrEntity), BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Emits code to generate a Lua error.
        protected static void EmitLuaError(ILGenerator ilg, string message)
        {
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldstr, message);
            ilg.Emit(Call, _luaL_error);
        }

        // Emits code to declare a `target` local variable, the target of the metamethod.
        protected static LocalBuilder EmitDeclareTarget(ILGenerator ilg, Type objType)
        {
            var isStruct = objType.IsClrStruct();

            // For CLR structs, we need to declare a byref target. This allows the struct to be mutated, as otherwise
            // only a copy of the struct would be mutated.

            var target = ilg.DeclareLocal(isStruct ? objType.MakeByRefType() : objType);
            ilg.Emit(Ldarg_1);  // Lua environment
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldc_I4_1);  // Target
            ilg.Emit(Call, _luaEnvironmentLoadClrEntity);
            ilg.Emit(isStruct ? Unbox : Unbox_Any, objType);

            return target;
        }

        // Emits code to declare a `keyType` local variable, the type of the key in the `__index` and `__newindex`
        // metamethods.
        protected static LocalBuilder EmitDeclareKeyType(ILGenerator ilg)
        {
            var keyType = ilg.DeclareLocal(typeof(LuaType));
            ilg.Emit(Ldarg_0);  // Lua state
            ilg.Emit(Ldc_I4_2);  // Key
            ilg.Emit(Call, _lua_type);
            ilg.Emit(Stloc, keyType);

            return keyType;
        }

        // Emits code to check the `keyType` local variable and perform a member access.
        protected static void EmitMemberAccess(
            ILGenerator ilg, LocalBuilder keyType, lua_State* state, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator, MemberInfo> emitMemberAccess,
            Action<ILGenerator> emitInvalidAccess)
        {
            var isNotString = ilg.DefineLabel();

            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Ldc_I4_4);
            ilg.Emit(Bne_Un, isNotString);  // Not short form
            {
                var keyPtr = ilg.DeclareLocal(typeof(nint));
                ilg.Emit(Ldarg_0);  // Lua state
                ilg.Emit(Ldc_I4_2);  // Key
                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Conv_I);
                ilg.Emit(Call, _lua_tolstring);
                ilg.Emit(Stloc, keyPtr);

                var ptrs = InternMemberNames();
                var labels = ilg.DefineLabels(members.Count);
                var defaultLabel = ilg.DefineLabel();
                EmitSwitch(keyPtr, ptrs.Zip(labels, (ptr, label) => (ptr, label)), defaultLabel);

                for (var i = 0; i < members.Count; ++i)
                {
                    ilg.MarkLabel(labels[i]);
                    emitMemberAccess(ilg, members[i]);
                }

                ilg.MarkLabel(defaultLabel);
                emitInvalidAccess(ilg);
            }

            ilg.MarkLabel(isNotString);
            return;

            // Interns the member names in the Lua state for efficient string comparisons.
            nint[] InternMemberNames()
            {
                lua_createtable(state, members.Count, 0);

                var result = new nint[members.Count];
                for (var i = 0; i < members.Count; ++i)
                {
                    result[i] = (nint)lua_pushstring(state, members[i].Name);
                    lua_rawseti(state, -2, i + 1);
                }

                _ = luaL_ref(state, LUA_REGISTRYINDEX);  // Prevents garbage collection

                return result;
            }

            // Emits the optimal branching logic for switching on the `ptr` local variable.
            void EmitSwitch(
                LocalBuilder keyPtr, IEnumerable<(nint ptr, Label label)> ptrsAndLabels, Label defaultLabel)
            {
                Helper(ptrsAndLabels.OrderBy(t => t.ptr).ToArray().AsSpan());
                return;

                void Helper(Span<(nint ptr, Label label)> ptrsAndLabels)
                {
                    // If there are three or fewer elements, it is best to perform a linear search: assuming a uniform
                    // distribution of pointers, the expected number of comparisons in a linear search is 2 and the
                    // expected number of comparisons in a binary search is 2.33.

                    if (ptrsAndLabels.Length < 4)
                    {
                        foreach (var (ptr, label) in ptrsAndLabels)
                        {
                            ilg.Emit(Ldloc, keyPtr);
                            ilg.Emit(Ldc_I8, ptr);
                            ilg.Emit(Conv_I);
                            ilg.Emit(Beq, label);  // Not short form
                        }

                        ilg.Emit(Br, defaultLabel);  // Not short form
                        return;
                    }

                    // Otherwise, perform a binary search: assuming a uniform distribution of pointers, the expected
                    // number of comparisons is O(log n).

                    var midpoint = ptrsAndLabels.Length / 2;

                    var isGreaterOrEqual = ilg.DefineLabel();

                    ilg.Emit(Ldloc, keyPtr);
                    ilg.Emit(Ldc_I8, ptrsAndLabels[midpoint].ptr);
                    ilg.Emit(Conv_I);
                    ilg.Emit(Bge_Un, isGreaterOrEqual);  // Not short form
                    {
                        Helper(ptrsAndLabels[0..midpoint]);
                    }

                    ilg.MarkLabel(isGreaterOrEqual);
                    {
                        Helper(ptrsAndLabels[midpoint..]);
                    }
                }
            }
        }
    }
}
