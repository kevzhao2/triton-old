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

namespace Triton.Interop
{
    internal sealed class MetamethodContext
    {
        internal static readonly MethodInfo _matchMemberName = typeof(MetamethodContext).GetMethod(nameof(MatchMemberName))!;
        internal static readonly MethodInfo _pushClrType = typeof(MetamethodContext).GetMethod(nameof(PushClrType))!;
        internal static readonly MethodInfo _toClrType = typeof(MetamethodContext).GetMethod(nameof(ToClrType))!;

        private readonly LuaEnvironment _environment;
        private readonly Dictionary<string, int> _memberNames;

        internal MetamethodContext(LuaEnvironment environment, IReadOnlyList<string> memberNames)
        {
            _environment = environment;

            _memberNames = new Dictionary<string, int>();
            for (var i = 0; i < memberNames.Count; ++i)
            {
                _memberNames.Add(memberNames[i], i);
            }
        }

        public int MatchMemberName(string memberName) => _memberNames.TryGetValue(memberName, out var i) ? i : -1;

        public void PushLuaObject(IntPtr state, LuaObject obj) =>
            _environment.PushLuaObject(state, obj);

        public void PushClrEntity(IntPtr state, object entity) =>
            _environment.PushClrEntity(state, entity);

        public void PushClrType(IntPtr state, Type type) =>
            _environment.PushClrEntity(state, new ClrTypeProxy(type));

        public Type? ToClrType(IntPtr state, int index) =>
            _environment.ToClrEntity(state, index) switch
            {
                ClrTypeProxy { Type: var type } => type,
                ClrGenericTypesProxy { Types: var types } => types.FirstOrDefault(t => !t.IsGenericTypeDefinition),
                _ => null
            };
    }
}
