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
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    internal sealed class MetamethodContext
    {
        internal static readonly MethodInfo _matchMemberName = typeof(MetamethodContext).GetMethod(nameof(MatchMemberName))!;
        internal static readonly MethodInfo _pushLuaObject = typeof(MetamethodContext).GetMethod(nameof(PushLuaObject))!;
        internal static readonly MethodInfo _pushClrEntity = typeof(MetamethodContext).GetMethod(nameof(PushClrEntity))!;
        internal static readonly MethodInfo _pushClrType = typeof(MetamethodContext).GetMethod(nameof(PushClrType))!;
        internal static readonly MethodInfo _toClrEntity = typeof(MetamethodContext).GetMethod(nameof(ToClrEntity))!;
        internal static readonly MethodInfo _toClrTypes = typeof(MetamethodContext).GetMethod(nameof(ToClrTypes))!;

        private readonly LuaEnvironment _environment;

        private readonly Dictionary<string, int> _memberNames;

        internal MetamethodContext(LuaEnvironment environment)
        {
            _environment = environment;

            _memberNames = new Dictionary<string, int>();
        }

        public int MatchMemberName(string memberName) => _memberNames.TryGetValue(memberName, out var i) ? i : -1;

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

        public void PushClrType(IntPtr state, Type type) =>
            _environment.PushClrEntity(state, new ClrTypeProxy(type));

        public object ToClrEntity(IntPtr state, int index) =>
            _environment.ToClrEntity(state, index);

        public Type[] ToClrTypes(IntPtr state, int index, LuaType keyType)
        {
            switch (keyType)
            {
            case LuaType.Userdata:
            {
                var type = ToClrType(state, index);
                if (type is null)
                {
                    luaL_error(state, "attempt to use generics with non-type arg");
                }

                return new[] { type };
            }

            case LuaType.Table:
                var length = (int)lua_rawlen(state, index);
                var types = new Type[length];
                for (var i = 1; i <= length; ++i)
                {
                    if (lua_rawgeti(state, index, i) != LuaType.Userdata)
                    {
                        luaL_error(state, "attempt to use generics with non-type arg");
                    }

                    var type = ToClrType(state, -1);
                    if (type is null)
                    {
                        luaL_error(state, "attempt to use generics with non-type arg");
                    }

                    types[i - 1] = type;
                }

                lua_pop(state, length);

                return types;

            default:
                throw new InvalidOperationException();
            }

            Type? ToClrType(IntPtr state, int index) =>
               _environment.ToClrEntity(state, index) switch
               {
                   ClrTypeProxy { Type: var type } => type,
                   ClrGenericTypesProxy { Types: var types } => types.FirstOrDefault(t => !t.IsGenericTypeDefinition),
                   _ => null
               };
        }

        internal void SetMembers(IReadOnlyList<MemberInfo> members)
        {
            for (var i = 0; i < members.Count; ++i)
            {
                _memberNames.Add(members[i].Name, i);
            }
        }
    }
}
