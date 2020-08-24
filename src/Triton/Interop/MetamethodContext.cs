// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        internal static readonly MethodInfo _errorMemberName =
            typeof(MetamethodContext).GetMethod(nameof(ErrorMemberName))!;

        internal static readonly MethodInfo _matchMemberName =
            typeof(MetamethodContext).GetMethod(nameof(MatchMemberName))!;

        internal static readonly MethodInfo _loadValue =
            typeof(MetamethodContext).GetMethod(nameof(LoadValue))!;

        internal static readonly MethodInfo _loadLuaObject =
            typeof(MetamethodContext).GetMethod(nameof(LoadLuaObject))!;

        internal static readonly MethodInfo _loadClrEntity =
            typeof(MetamethodContext).GetMethod(nameof(LoadClrEntity))!;

        internal static readonly MethodInfo _loadClrTypes =
            typeof(MetamethodContext).GetMethod(nameof(LoadClrTypes))!;

        internal static readonly MethodInfo _pushValue =
            typeof(MetamethodContext).GetMethod(nameof(PushValue))!;

        internal static readonly MethodInfo _pushLuaObject =
            typeof(MetamethodContext).GetMethod(nameof(PushLuaObject))!;

        internal static readonly MethodInfo _pushClrEntity =
            typeof(MetamethodContext).GetMethod(nameof(PushClrEntity))!;

        internal static readonly MethodInfo _pushGenericClrType =
            typeof(MetamethodContext).GetMethod(nameof(PushGenericClrType))!;

        internal static readonly MethodInfo _pushClrMethods =
            typeof(MetamethodContext).GetMethod(nameof(PushClrMethods))!;

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
        /// Raises an error using the member name.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="format">The format string.</param>
        /// <returns>The number of results.</returns>
        [DoesNotReturn]
        public int ErrorMemberName(IntPtr state, string format) =>
            luaL_error(state, string.Format(format, lua_tostring(state, 2)));

        /// <summary>
        /// Matches the member name, returning the member's index (or <c>-1</c> if it does not exist).
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <returns>The member's index (or <c>-1</c> if it does not exist).</returns>
        public int MatchMemberName(IntPtr state) =>
            _memberNameToIndex.TryGetValue(lua_tolstring(state, 2, IntPtr.Zero), out var index) ? index : -1;

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
        /// Loads CLR types from the keys on the stack.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="startIndex"></param>
        /// <param name="numKeys"></param>
        /// <param name="keyTypes"></param>
        /// <returns></returns>
        public unsafe Type[] LoadClrTypes(IntPtr state, int startIndex, int numKeys, LuaType* keyTypes)
        {
            var types = new Type[numKeys];
            for (var i = 0; i < numKeys; ++i)
            {
                if (keyTypes[i] != LuaType.Userdata)
                {
                    luaL_error(state, "attempt to index generics with non-type arg");
                }

                var type = LoadClrType(state, startIndex + i);
                if (type is null)
                {
                    luaL_error(state, "attempt to index generics with non-type arg");
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

        public void PushGenericClrType(IntPtr state, RuntimeTypeHandle type, Type[] typeArgs)
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

        /// <summary>
        /// Pushes the given CLR methods onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="methods">The CLR methods.</param>
        public void PushClrMethods(IntPtr state, IReadOnlyList<MethodInfo> methods) =>
            _metavalueGenerator.PushMethodsFunction(state, methods);
    }
}
