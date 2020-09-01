// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Provides context for a metamethod along with helper functions.
    /// </summary>
    internal sealed class MetamethodContext
    {
        internal static readonly MethodInfo _loadValue =
            typeof(MetamethodContext).GetMethod(nameof(LoadValue))!;

        internal static readonly MethodInfo _loadLuaObject =
            typeof(MetamethodContext).GetMethod(nameof(LoadLuaObject))!;

        internal static readonly MethodInfo _loadClrEntity =
            typeof(MetamethodContext).GetMethod(nameof(LoadClrEntity))!;

        internal static readonly MethodInfo _pushValue =
            typeof(MetamethodContext).GetMethod(nameof(PushValue))!;

        internal static readonly MethodInfo _pushLuaObject =
            typeof(MetamethodContext).GetMethod(nameof(PushLuaObject))!;

        internal static readonly MethodInfo _pushClrEntity =
            typeof(MetamethodContext).GetMethod(nameof(PushClrEntity))!;

        internal static readonly MethodInfo _pushClrMethods =
            typeof(MetamethodContext).GetMethod(nameof(PushClrMethods))!;

        internal static readonly MethodInfo _getNumKeys =
            typeof(MetamethodContext).GetMethod(nameof(GetNumKeys))!;

        internal static readonly MethodInfo _constructTypeArgs =
            typeof(MetamethodContext).GetMethod(nameof(ConstructTypeArgs))!;

        internal static readonly MethodInfo _getArrayIndex =
            typeof(MetamethodContext).GetMethod(nameof(GetSzArrayIndex))!;

        internal static readonly MethodInfo _getArrayIndices =
            typeof(MetamethodContext).GetMethod(nameof(GetArrayIndices))!;

        internal static readonly MethodInfo _getTypeArgs =
            typeof(MetamethodContext).GetMethod(nameof(GetTypeArgs))!;

        internal static readonly MethodInfo _constructAndPushGenericType =
            typeof(MetamethodContext).GetMethod(nameof(PushGenericType))!;

        private readonly LuaEnvironment _environment;
        private readonly ClrMetatableGenerator _metavalueGenerator;
        private readonly Dictionary<IntPtr, int> _memberNameToIndex = new Dictionary<IntPtr, int>();

        internal MetamethodContext(LuaEnvironment environment, ClrMetatableGenerator metavalueGenerator)
        {
            _environment = environment;
            _metavalueGenerator = metavalueGenerator;
        }

        /// <summary>
        /// Sets the context's members.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="members">The members.</param>
        public void SetMembers(IntPtr state, IReadOnlyList<MemberInfo> members)
        {
            Debug.Assert(members.All(n => n is { }));

            // We store the member names inside of a Lua atble in the registry as strings. Since all strings in Lua are
            // reused if possible, we can then perform string comparisons by checking the string pointers, which is
            // extremely efficient.

            lua_createtable(state, members.Count, 0);
            for (var i = 0; i < members.Count; ++i)
            {
                var ptr = lua_pushstring(state, members[i].Name);
                lua_rawseti(state, -2, i + 1);
                _memberNameToIndex.Add(ptr, i);
            }
            _ = luaL_ref(state, LUA_REGISTRYINDEX);
        }

        /// <summary>
        /// Loads a Lua value from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting Lua value.</returns>
        public LuaValue LoadValue(IntPtr state, int index, LuaType type)
        {
            _environment.LoadValue(state, index, type, out var value);
            return value;
        }

        /// <summary>
        /// Loads a Lua object from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting Lua object.</returns>
        public LuaObject LoadLuaObject(IntPtr state, int index, LuaType type) =>
            _environment.LoadLuaObject(state, index, type);

        /// <summary>
        /// Loads a CLR entity from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <returns>The resulting CLR entity.</returns>
        public object LoadClrEntity(IntPtr state, int index) =>
            _environment.LoadClrEntity(state, index);

        /// <summary>
        /// Pushes the given Lua value onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The Lua value.</param>
        public void PushValue(IntPtr state, LuaValue value) =>
            _environment.PushValue(state, value);

        /// <summary>
        /// Pushes the given Lua object onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The Lua object.</param>
        public void PushLuaObject(IntPtr state, LuaObject obj) =>
            _environment.PushLuaObject(state, obj);

        /// <summary>
        /// Pushes the given CLR entity onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="entity">The CLR entity.</param>
        public void PushClrEntity(IntPtr state, object entity) =>
            _environment.PushClrEntity(state, entity);

        /// <summary>
        /// Pushes the given CLR methods onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="methods">The CLR methods.</param>
        public void PushClrMethods(IntPtr state, IReadOnlyList<MethodInfo> methods) =>
            _metavalueGenerator.PushMethodsFunction(state, methods);

        public int GetNumKeys(IntPtr state, LuaType type) =>
            type switch
            {
                LuaType.Table => (int)lua_rawlen(state, 2),
                _             => 1
            };

        public unsafe Type[] ConstructTypeArgs(IntPtr state, int startIndex, int numKeys, LuaType* keyTypes)
        {
            var types = new Type[numKeys];
            for (var i = 0; i < numKeys; ++i)
            {
                if (keyTypes[i] != LuaType.Userdata)
                {
                    luaL_error(state, "attempt to construct generic with non-type arg");
                }

                var type = LoadClrType(state, startIndex + i);
                if (type is null)
                {
                    luaL_error(state, "attempt to construct generic with non-type arg");
                }

                types[i] = type;
            }

            return types;

            Type? LoadClrType(IntPtr state, int index) =>
               _environment.LoadClrEntity(state, index) switch
               {
                   ProxyClrTypes { Types: var types } => types.FirstOrDefault(t => !t.IsGenericTypeDefinition),
                   _ => null
               };
        }

        public static void GetArgTypes(IntPtr state, Span<LuaType> argTypes)
        {
            for (var i = 0; i < argTypes.Length; ++i)
            {
                argTypes[i] = lua_type(state, i - argTypes.Length);
            }
        }

        public static int GetIndexerArgCount(IntPtr state, int index, LuaType keyType) =>
            keyType switch
            {
                LuaType.Table => (int)lua_rawlen(state, index),
                _             => 1
            };

        public static void GetKeyTypes(IntPtr state, int index, LuaType keyType, LuaType valueType, Span<LuaType> types)
        {
            if (keyType == LuaType.Table)
            {
                var length = types.Length - (valueType != LuaType.None ? 1 : 0);
                for (var i = 1; i <= length; ++i)
                {
                    types[i - 1] = lua_rawgeti(state, index, i);
                }

                if (valueType != LuaType.None)
                {
                    lua_pushvalue(state, 3);
                }
            }
            else
            {
                types[0] = keyType;
            }

            if (valueType != LuaType.None)
            {
                types[^1] = valueType;
            }
        }

        public int GetMemberIndex(IntPtr state, int index) =>
            _memberNameToIndex.TryGetValue(lua_tolstring(state, index, IntPtr.Zero), out var memberIndex)
                ? memberIndex
                : -1;

        public static int GetSzArrayIndex(IntPtr state, int index, LuaType keyType) =>
            (keyType == LuaType.Number && lua_isinteger(state, index))
                ? (int)lua_tointeger(state, index)
                : luaL_error(state, "attempt to access array with non-integer index");

        public static void GetArrayIndices(IntPtr state, int index, LuaType keyType, Span<int> indices)
        {
            if (keyType == LuaType.Number)
            {
                if (1 != indices.Length)
                {
                    luaL_error(state, "attempt to access array with incorrect number of indices");
                }

                indices[0] = lua_isinteger(state, index)
                    ? (int)lua_tointeger(state, index)
                    : luaL_error(state, "attempt to access array with non-integer index");
            }
            else if (keyType == LuaType.Table)
            {
                if ((int)lua_rawlen(state, index) != indices.Length)
                {
                    luaL_error(state, "attempt to access array with incorrect number of indices");
                }

                for (var i = 0; i < indices.Length; ++i)
                {
                    indices[i] = (lua_rawgeti(state, index, i + 1) == LuaType.Number && lua_isinteger(state, -1))
                        ? (int)lua_tointeger(state, -1)
                        : luaL_error(state, "attempt to access array with non-integer index");
                }
            }
            else
            {
                luaL_error(state, "attempt to access array with non-integer index");
            }
        }

        public Type[] GetTypeArgs(IntPtr state, int index, LuaType keyType)
        {
            if (keyType == LuaType.Userdata)
            {
                var type = ToClrType(state, index);
                if (type is null)
                {
                    luaL_error(state, "attempt to construct generic with non-type arg");
                    return null!;
                }

                return new[] { type };
            }
            else if (keyType == LuaType.Table)
            {
                var types = new Type[(int)lua_rawlen(state, index)];
                for (var i = 0; i < types.Length; ++i)
                {
                    if (lua_rawgeti(state, index, i + 1) != LuaType.Userdata)
                    {
                        luaL_error(state, "attempt to construct generic with non-type arg");
                        return null!;
                    }

                    var type = ToClrType(state, index);
                    if (type is null)
                    {
                        luaL_error(state, "attempt to construct generic with non-type arg");
                        return null!;
                    }

                    types[i] = type;
                }

                return types;
            }
            else
            {
                luaL_error(state, "attempt to construct generic with non-type arg");
                return null!;
            }

            Type? ToClrType(IntPtr state, int index) =>
                _environment.LoadClrEntity(state, index) is ProxyClrTypes { Types: var types }
                    ? types.FirstOrDefault(t => !t.IsGenericTypeDefinition)
                    : null;
        }

        public void PushGenericType(IntPtr state, RuntimeTypeHandle type, Type[] typeArgs)
        {
            try
            {
                var constructedType = Type.GetTypeFromHandle(type).MakeGenericType(typeArgs);
                _environment.PushClrEntity(state, new ProxyClrTypes(new[] { constructedType }));
            }
            catch (ArgumentException)
            {
                luaL_error(state, "attempt to construct generic type with invalid constraints");
            }
        }
    }
}
