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
using System.Runtime.InteropServices;
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Generates Lua metavalues for CLR types and objects.
    /// </summary>
    internal sealed class ClrMetavalueGenerator
    {
        private static readonly MethodInfo _lua_tostring = typeof(NativeMethods).GetMethod("lua_tostring")!;

        private static readonly MethodInfo _matchMemberName =
            typeof(MetamethodContext).GetMethod("MatchMemberName", NonPublic | Instance)!;

        private static readonly lua_CFunction _gcMetamethod = GcMetamethod;
        private static readonly lua_CFunction _tostringMetamethod = ProtectedCall(ToStringMetamethod);

        private readonly LuaEnvironment _environment;
        private readonly List<lua_CFunction> _generatedCallbacks;  // Used to prevent garbage collection of delegates

        /// <summary>
        /// Initializes a new instance of the <see cref="ClrMetavalueGenerator"/> class with the specified Lua
        /// <paramref name="environment"/>.
        /// </summary>
        /// <param name="environment">The Lua environment.</param>
        internal ClrMetavalueGenerator(LuaEnvironment environment)
        {
            _environment = environment;
            _generatedCallbacks = new List<lua_CFunction>();
        }

        // Creates a wrapper `lua_CFunction` that performs a "protected" call of a `lua_CFunction`, raising uncaught CLR
        // exceptions as Lua errors. This is required since exceptions should _NOT_ be thrown in reverse P/Invokes.
        //
        private static lua_CFunction ProtectedCall(lua_CFunction callback) =>
            state =>
            {
                try
                {
                    return callback(state);
                }
                catch (Exception ex)
                {
                    return luaL_error(state, $"unhandled CLR exception:\n{ex}");
                }
            };

        private static int GcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr).Unmarked());
            handle.Free();
            return 0;
        }

        private static int ToStringMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr).Unmarked());
            _ = lua_pushstring(state, handle.Target.ToString());
            return 1;
        }

        private static DynamicMethod CreateDynamicMetamethod(string name) =>
            new DynamicMethod(
                name, typeof(int), new[] { typeof(MetamethodContext), typeof(IntPtr) }, typeof(MetamethodContext));
        
        private static (List<string> memberNames, Dictionary<string, MemberInfo> members) GenerateMemberIndices(
            IEnumerable<MemberInfo> allMembers)
        {
            var memberNames = new List<string>();
            var members = new Dictionary<string, MemberInfo>();
            foreach (var member in allMembers)
            {
                memberNames.Add(member.Name);
                members.Add(member.Name, member);
            }

            return (memberNames, members);
        }

        /// <summary>
        /// Pushes the <c>__gc</c> metamethod onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        internal void PushGc(IntPtr state) => lua_pushcfunction(state, _gcMetamethod);

        /// <summary>
        /// Pushes the <c>__tostring</c> metamethod onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        internal void PushToString(IntPtr state) => lua_pushcfunction(state, _tostringMetamethod);

        /// <summary>
        /// Pushes the <c>__index</c> metavalue for the given CLR <paramref name="type"/> onto the stack of the Lua
        /// <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        internal void PushTypeIndex(IntPtr state, Type type)
        {
            // The metavalue is a table with entries for const fields, static events, static methods, and nested
            // types. This table then has an `__index` metamethod which resolves non-const static fields and
            // static properties.
            //
            // Essentially, we are caching all cacheable members, which greatly improves performance as there are fewer
            // unmanaged <-> managed transitions.
            //
            lua_newtable(state);

            foreach (var constField in type.GetAllConstFields())
            {
                var value = LuaValue.FromObject(constField.GetValue(null));
                _environment.PushValue(state, value);
                lua_setfield(state, -2, constField.Name);
            }

            foreach (var nestedType in type.GetAllNestedTypes())
            {
                _environment.PushClrType(state, nestedType);
                lua_setfield(state, -2, nestedType.Name);
            }

            lua_newtable(state);

            var indexMetamethod = ProtectedCall(GenerateIndexMetamethod());
            _generatedCallbacks.Add(indexMetamethod);
            lua_pushcfunction(state, indexMetamethod);
            lua_setfield(state, -2, "__index");

            lua_setmetatable(state, -2);

            lua_CFunction GenerateIndexMetamethod()
            {
                var staticFields = type.GetAllStaticFields();
                var staticProperties = type.GetAllStaticProperties();
                var (memberNames, members) = GenerateMemberIndices(
                    ((IEnumerable<MemberInfo>)staticFields).Concat(staticProperties));

                var context = new MetamethodContext(_environment, memberNames);
                var method = CreateDynamicMetamethod("__index");
                var ilg = method.GetILGenerator();
                var labels = ilg.DefineLabels(memberNames.Count);

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                ilg.Emit(Call, _lua_tostring);
                ilg.Emit(Call, _matchMemberName);
                ilg.Emit(Switch, labels);

                ilg.EmitLuaError("attempt to index invalid member");
                ilg.Emit(Ret);

                for (var i = 0; i < memberNames.Count; ++i)
                {
                    ilg.MarkLabel(labels[i]);

                    var member = members[memberNames[i]];
                    if (member is FieldInfo { FieldType: var fieldType } field)
                    {
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldsfld, field);
                        ilg.EmitLuaPush(fieldType);
                    }
                    else if (member is PropertyInfo { PropertyType: var propertyType } property)
                    {
                        if (!property.CanRead)
                        {
                            ilg.EmitLuaError("attempt to index non-readable property");
                            ilg.Emit(Ret);
                            continue;
                        }

                        if (propertyType.IsByRefLike)
                        {
                            ilg.EmitLuaError("attempt to index by-ref like property");
                            ilg.Emit(Ret);
                            continue;
                        }

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Call, property.GetMethod);

                        // Support ref-returning properties by emitting an indirect load.
                        if (propertyType.IsByRef)
                        {
                            propertyType = propertyType.GetElementType();
                            ilg.EmitLoadIndirect(propertyType);
                        }

                        ilg.EmitLuaPush(propertyType);
                    }

                    ilg.Emit(Ldc_I4_1);
                    ilg.Emit(Ret);
                }

                return (lua_CFunction)method.CreateDelegate(typeof(lua_CFunction), context);
            }
        }

        /// <summary>
        /// Pushes the <c>__index</c> metavalue for the given CLR <paramref name="objType"/> onto the stack of the Lua
        /// <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="objType">The CLR object type.</param>
        internal void PushObjectIndex(IntPtr state, Type objType)
        {
            // The metavalue is a table with entries for instance events and instance methods. This table then has an
            // `__index` metamethod which resolves instance fields and instance properties.
            //
            // Essentially, we are caching all cacheable members, which greatly improves performance as there are fewer
            // unmanaged <-> managed transitions.
            //
            lua_newtable(state);
        }
    }
}
