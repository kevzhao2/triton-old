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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Provides context for a generated metamethod.
    /// </summary>
    internal sealed class MetamethodContext
    {
        internal static readonly MethodInfo _matchMemberName = typeof(MetamethodContext).GetMethod(nameof(MatchMemberName))!;
        internal static readonly MethodInfo _errorMemberName = typeof(MetamethodContext).GetMethod(nameof(ErrorMemberName))!;
        internal static readonly MethodInfo _loadLuaValue = typeof(MetamethodContext).GetMethod(nameof(LoadValue))!;
        internal static readonly MethodInfo _loadLuaObject = typeof(MetamethodContext).GetMethod(nameof(LoadLuaObject))!;
        internal static readonly MethodInfo _loadClrEntity = typeof(MetamethodContext).GetMethod(nameof(LoadClrEntity))!;
        internal static readonly MethodInfo _loadClrTypes = typeof(MetamethodContext).GetMethod(nameof(LoadClrTypes))!;
        internal static readonly MethodInfo _pushValue = typeof(MetamethodContext).GetMethod(nameof(PushValue))!;
        internal static readonly MethodInfo _pushLuaObject = typeof(MetamethodContext).GetMethod(nameof(PushLuaObject))!;
        internal static readonly MethodInfo _pushClrEntity = typeof(MetamethodContext).GetMethod(nameof(PushClrEntity))!;
        internal static readonly MethodInfo _pushClrType = typeof(MetamethodContext).GetMethod(nameof(PushClrType))!;

        private readonly LuaEnvironment _environment;
        private readonly Dictionary<IntPtr, int> _memberNameToIndex = new Dictionary<IntPtr, int>();

        internal MetamethodContext(LuaEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Sets the context's members.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="members">The members.</param>
        public void SetMembers(IntPtr state, IReadOnlyList<MemberInfo> members)
        {
            Debug.Assert(members.All(n => n is { }));

            // The idea is to store the member names inside of a Lua table in the registry, as strings. Since all
            // strings in Lua are reused if possible, we can perform string comparisons by just checking the string
            // pointers! This saves us from doing extra work.
            //
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
        /// Matches the given string pointer to a member's name, returning the member's index (or <c>-1</c> if it does
        /// not exist).
        /// </summary>
        /// <param name="ptr">The string pointer.</param>
        /// <returns>The member's index (or <c>-1</c> if it does not exist).</returns>
        public int MatchMemberName(IntPtr ptr) =>
            _memberNameToIndex.TryGetValue(ptr, out var index) ? index : -1;

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
        /// Loads CLR types from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting CLR types.</returns>
        public Type[] LoadClrTypes(IntPtr state, int index, LuaType type)
        {
            if (type == LuaType.Userdata)
            {
                var clrType = LoadClrType(state, index);
                if (clrType is null)
                {
                    luaL_error(state, "attempt to index generics with non-type arg");
                }

                return new[] { clrType };
            }
            else if (type == LuaType.Table)
            {
                var length = (int)lua_rawlen(state, index);
                var types = new Type[length];
                for (var i = 1; i <= length; ++i)
                {
                    if (lua_rawgeti(state, index, i) != LuaType.Userdata)
                    {
                        luaL_error(state, "attempt to index generics with non-type arg");
                    }

                    var clrType = LoadClrType(state, -1);
                    if (clrType is null)
                    {
                        luaL_error(state, "attempt to index generics with non-type arg");
                    }

                    types[i - 1] = clrType;
                }

                lua_pop(state, length);
                return types;
            }
            else
            {
                throw new InvalidOperationException();
            }

            Type? LoadClrType(IntPtr state, int index) =>
               _environment.LoadClrEntity(state, index) switch
               {
                   ProxyClrType { Type: var type } => type,
                   ProxyGenericClrTypes { Types: var types } => types.FirstOrDefault(t => !t.IsGenericTypeDefinition),
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
        public void PushLuaObject(IntPtr state, LuaObject? obj)
        {
            if (obj is null)
            {
                lua_pushnil(state);
            }
            else
            {
                _environment.PushLuaObject(state, obj);
            }
        }

        /// <summary>
        /// Pushes the given CLR entity onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="entity">The CLR entity.</param>
        public void PushClrEntity(IntPtr state, object? entity)
        {
            if (entity is null)
            {
                lua_pushnil(state);
            }
            else
            {
                _environment.PushClrEntity(state, entity);
            }
        }

        /// <summary>
        /// Pushes the given CLR type onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        public void PushClrType(IntPtr state, Type type) =>
            _environment.PushClrEntity(state, new ProxyClrType(type));
    }
}
