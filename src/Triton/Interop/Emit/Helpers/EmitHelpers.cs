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
using static Triton.Lua.LuaType;

namespace Triton.Interop.Emit.Helpers
{
    /// <summary>
    /// Provides helper methods for emitting metamethods.
    /// </summary>
    internal static class EmitHelpers
    {
        private static readonly MethodInfo _lua_tolstring = typeof(Lua).GetMethod(nameof(lua_tolstring))!;
        private static readonly MethodInfo _lua_tostring = typeof(Lua).GetMethod(nameof(lua_tostring))!;
        private static readonly MethodInfo _lua_type = typeof(Lua).GetMethod(nameof(lua_type))!;

        private static readonly MethodInfo _luaL_error = typeof(Lua).GetMethod(nameof(luaL_error))!;

        private static readonly MethodInfo _lua_getenvironment = typeof(Lua).GetMethod(nameof(lua_getenvironment))!;

        private static readonly MethodInfo _tryLoadNdArrayIndices =
            typeof(EmitHelpers)
                .GetMethod(nameof(TryLoadNdArrayIndices), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo _luaEnvironmentLoadClrEntity =
            typeof(LuaEnvironment)
                .GetMethod(nameof(LuaEnvironment.LoadClrEntity), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly ConstructorInfo _spanIntCtor =
            typeof(Span<int>).GetConstructor(new[] { typeof(void*), typeof(int) })!;

        private static readonly MethodInfo _spanIntItemGet =
            typeof(Span<int>).GetProperty("Item")!.GetMethod!;

        private static readonly MethodInfo _stringFormat1 =
            typeof(string).GetMethod(nameof(string.Format), new[] { typeof(string), typeof(object) })!;

        /// <summary>
        /// Emits code to raise a Lua error with the given message.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="message">The error message to raise.</param>
        public static void LuaError(
            ILGenerator ilg, string message)
        {
            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Ldstr, message);
            ilg.Emit(Call, _luaL_error);
        }

        /// <summary>
        /// Emits code to raise a Lua error with the given message, formatted with one argument.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="message">The error message to raise.</param>
        /// <param name="emitLoadArg1">A callback that emits code to load the first argument.</param>
        public static void LuaError(
            ILGenerator ilg, string message,
            Action<ILGenerator> emitLoadArg1)
        {
            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Ldstr, message);
            emitLoadArg1(ilg);
            ilg.Emit(Call, _stringFormat1);
            ilg.Emit(Call, _luaL_error);
        }

        /// <summary>
        /// Emits code to push a value of the given type onto the Lua satck.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type of the value to push.</param>
        /// <param name="emitLoadValue">A callback that emits code to load the value.</param>
        public static void LuaPush(
            ILGenerator ilg, Type type,
            Action<ILGenerator> emitLoadValue)
        {
            ilg.Emit(Ldarg_0);  // `state`
            emitLoadValue(ilg);
            ilg.Emit(Call, LuaPushHelpers.Get(type));
        }

        /// <summary>
        /// Emits code to try to load a value of the given type from the Lua satck.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type of the value to try to load.</param>
        /// <param name="emitLoadIndex">A callback that emits code to load the index.</param>
        /// <param name="emitLoadAddress">A callback that emits code to load the address.</param>
        /// <param name="emitInvalidValue">A callback that emits code in the case that the value is invalid.</param>
        public static void LuaTryLoad(
            ILGenerator ilg, Type type,
            Action<ILGenerator> emitLoadIndex,
            Action<ILGenerator> emitLoadAddress,
            Action<ILGenerator> emitInvalidValue)
        {
            var isValidValue = ilg.DefineLabel();

            ilg.Emit(Ldarg_0);  // `state`
            emitLoadIndex(ilg);
            emitLoadAddress(ilg);
            ilg.Emit(Call, LuaTryLoadHelpers.Get(type));
            ilg.Emit(Brtrue_S, isValidValue);
            {
                emitInvalidValue(ilg);
            }

            ilg.MarkLabel(isValidValue);
        }

        /// <summary>
        /// Emits code to declare a local variable containing the target of the metamethod.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type of the target.</param>
        /// <returns>A local variable containing the target of the metamethod.</returns>
        public static LocalBuilder DeclareTarget(ILGenerator ilg, Type type)
        {
            var isStruct = type.IsClrStruct();

            // For CLR structs, we declare a by-ref target. This allows the struct to be mutated.
            //
            var target = ilg.DeclareLocal(isStruct ? type.MakeByRefType() : type);
            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Call, _lua_getenvironment);
            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Ldc_I4_1);  // target
            ilg.Emit(Call, _luaEnvironmentLoadClrEntity);
            ilg.Emit(isStruct ? Unbox : Unbox_Any, type);
            ilg.Emit(Stloc, target);

            return target;
        }

        /// <summary>
        /// Emits code to load the target of the metamethod, if applicable.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The target of the metamethod, or <see langword="null"/> if there is none.</param>
        public static void MaybeLoadTarget(ILGenerator ilg, LocalBuilder? target)
        {
            if (target is not null)
            {
                ilg.Emit(target.LocalType.IsByRef ? Ldloca : Ldloc, target);
            }
        }

        /// <summary>
        /// Emits code to perform member accesses.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type containing the members.</param>
        /// <param name="members">The members to access.</param>
        /// <param name="emitMemberAccess">A callback that emits code to access the member.</param>
        public static unsafe void MemberAccesses(
            lua_State* state, ILGenerator ilg, Type type, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator, MemberInfo> emitMemberAccess)
        {
            var isNotString = ilg.DefineLabel();

            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Ldc_I4_2);  // key
            ilg.Emit(Call, _lua_type);
            ilg.Emit(Ldc_I4, (int)LUA_TSTRING);
            ilg.Emit(Bne_Un, isNotString);
            {
                var ptrs = InternMemberNames();
                var labels = ilg.DefineLabels(members.Count);

                var keyPtr = ilg.DeclareLocal(typeof(nint));
                ilg.Emit(Ldarg_0);  // `state`
                ilg.Emit(Ldc_I4_2);  // key
                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Conv_I);
                ilg.Emit(Call, _lua_tolstring);
                ilg.Emit(Stloc, keyPtr);

                EmitSwitch(ilg, keyPtr, ptrs.Zip(labels));

                LuaError(
                    ilg, $"attempt to index invalid member '{type.Name}.{{0}}'",
                    ilg =>
                    {
                        ilg.Emit(Ldarg_0);  // `state`
                        ilg.Emit(Ldc_I4_2);  // key
                        ilg.Emit(Call, _lua_tostring);
                    });
                ilg.Emit(Ret);

                foreach (var (member, label) in members.Zip(labels))
                {
                    ilg.MarkLabel(label);

                    emitMemberAccess(ilg, member);
                }
            }

            ilg.MarkLabel(isNotString);

            return;

            // Interns the member names in the Lua state, allowing for efficient string comparisons.
            //
            nint[] InternMemberNames()
            {
                lua_createtable(state, members.Count, 0);

                var result = new nint[members.Count];
                for (var i = 0; i < members.Count; ++i)
                {
                    result[i] = (nint)lua_pushstring(state, members[i].Name);
                    lua_rawseti(state, -2, i + 1);
                }

                _ = luaL_ref(state, LUA_REGISTRYINDEX);

                return result;
            }

            // Emits optimal code for switching on the key pointer.
            //
            void EmitSwitch(
                ILGenerator ilg, LocalBuilder keyPtr, IEnumerable<(nint ptr, Label label)> ptrsAndLabels)
            {
                var defaultLabel = ilg.DefineLabel();

                Helper(ilg, ptrsAndLabels.OrderBy(t => t.ptr).ToArray().AsSpan());

                ilg.MarkLabel(defaultLabel);
                return;

                void Helper(ILGenerator ilg, Span<(nint ptr, Label label)> ptrsAndLabels)
                {
                    // If there are 3 or fewer elements, use linear search.
                    //
                    if (ptrsAndLabels.Length <= 3)
                    {
                        foreach (var (ptr, label) in ptrsAndLabels)
                        {
                            ilg.Emit(Ldloc, keyPtr);
                            ilg.Emit(Ldc_I8, ptr);
                            ilg.Emit(Conv_I);
                            ilg.Emit(Beq, label);
                        }

                        ilg.Emit(Br, defaultLabel);
                        return;
                    }

                    // Otherwise, use binary search. This will result in O(log n) comparisons.
                    //
                    var midpoint = ptrsAndLabels.Length / 2;

                    var isGreaterOrEqual = ilg.DefineLabel();

                    ilg.Emit(Ldloc, keyPtr);
                    ilg.Emit(Ldc_I8, ptrsAndLabels[midpoint].ptr);
                    ilg.Emit(Conv_I);
                    ilg.Emit(Bge_Un, isGreaterOrEqual);
                    {
                        Helper(ilg, ptrsAndLabels[0..midpoint]);
                    }

                    ilg.MarkLabel(isGreaterOrEqual);
                    {
                        Helper(ilg, ptrsAndLabels[midpoint..]);
                    }
                }
            }
        }

        /// <summary>
        /// Emits code to declare a local variable containing the index into a single-dimensional, zero-based array.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <returns>A local variable containing the index into a single-dimensional, zero-based array.</returns>
        public static LocalBuilder DeclareSzArrayIndex(ILGenerator ilg)
        {
            var isValidIndex = ilg.DefineLabel();

            var index = ilg.DeclareLocal(typeof(int));
            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Ldc_I4_2);  // key
            ilg.Emit(Ldloca, index);
            ilg.Emit(Call, LuaTryLoadHelpers.Get(typeof(int)));
            ilg.Emit(Brtrue_S, isValidIndex);
            {
                LuaError(
                    ilg, "attempt to index an array with an invalid index");
                ilg.Emit(Ret);
            }

            ilg.MarkLabel(isValidIndex);

            return index;
        }

        /// <summary>
        /// Emits code to declare a local variable containing the indices into a multi-dimensional array.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="rank">The rank of the multi-dimensional array.</param>
        /// <returns>A local variable containing the indices into a multi-dimensional array.</returns>
        public static LocalBuilder DeclareNdArrayIndices(ILGenerator ilg, int rank)
        {
            // The array rank has an upper bound of 32, so stack-allocating the span (upper bound of 128 bytes) should
            // be safe.
            //
            var indices = ilg.DeclareLocal(typeof(Span<int>));
            ilg.Emit(Ldc_I4, 4 * rank);
            ilg.Emit(Conv_U);
            ilg.Emit(Localloc);
            ilg.Emit(Ldc_I4, rank);
            ilg.Emit(Newobj, _spanIntCtor);
            ilg.Emit(Stloc, indices);

            var isValidIndices = ilg.DefineLabel();

            ilg.Emit(Ldarg_0);  // `state`
            ilg.Emit(Ldc_I4_2);  // key
            ilg.Emit(Ldloc, indices);
            ilg.Emit(Call, _tryLoadNdArrayIndices);
            ilg.Emit(Brtrue_S, isValidIndices);
            {
                LuaError(
                    ilg, "attempt to index a multi-dimensional array with invalid indices");
                ilg.Emit(Ret);
            }

            ilg.MarkLabel(isValidIndices);

            return indices;
        }

        /// <summary>
        /// Emits code to load the indices into a multi-dimensional array.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="rank">The rank of the multi-dimensional array.</param>
        /// <param name="indices">The indices into a multi-dimensional array.</param>
        public static void LoadNdArrayIndices(ILGenerator ilg, int rank, LocalBuilder indices)
        {
            for (var i = 0; i < rank; ++i)
            {
                ilg.Emit(Ldloca, indices);
                ilg.Emit(Ldc_I4, i);
                ilg.Emit(Call, _spanIntItemGet);
                ilg.Emit(Ldind_I4);
            }
        }

        internal static unsafe bool TryLoadNdArrayIndices(lua_State* state, int index, Span<int> arrayIndices)
        {
            var type = lua_type(state, index);

            if (type == LUA_TNUMBER)
            {
                if (arrayIndices.Length != 1 || !lua_isinteger(state, index))
                {
                    return false;
                }

                var integer = lua_tointeger(state, index);
                if ((ulong)(integer - int.MinValue) > uint.MaxValue)
                {
                    return false;
                }

                arrayIndices[0] = (int)integer;
                return true;
            }
            else if (type == LUA_TTABLE)
            {
                if (arrayIndices.Length != (int)lua_rawlen(state, index))
                {
                    return false;
                }

                for (var i = 0; i < arrayIndices.Length; ++i)
                {
                    // This will push stuff onto the Lua stack, but the stack will be cleaned up after the metamethod
                    // returns.
                    //
                    if (lua_rawgeti(state, index, i + 1) != LUA_TNUMBER || !lua_isinteger(state, -1))
                    {
                        return false;
                    }

                    var integer = lua_tointeger(state, -1);
                    if ((ulong)(integer - int.MinValue) > uint.MaxValue)
                    {
                        return false;
                    }

                    arrayIndices[i] = (int)integer;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
