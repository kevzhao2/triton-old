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
using Triton.Interop.Extensions;
using static System.Reflection.Emit.OpCodes;
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    internal sealed partial class ClrMetavalueGenerator
    {
        private readonly LuaEnvironment _environment;

        private readonly int _wrapObjectIndexReference;

        private readonly List<LuaCFunction> _generatedMetamethods;

        internal ClrMetavalueGenerator(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            // Set up a function to wrap the `__index` metavalue for CLR objects. This allows us to forward the CLR
            // object to the `__index` metamethod for the metavalue; otherwise, the `__index` metamethod would receive
            // the metavalue as the first argument, instead.
            //
            // Note that this is not required for CLR types since the type object is not required.
            //
            luaL_loadstring(state, @"
                local t = ...
                local __index = getmetatable(t).__index
                return function(obj, key)
                    local v = rawget(t, key)
                    if v ~= nil then
                        return v
                    else
                        return __index(obj, key)
                    end
                end");
            _wrapObjectIndexReference = luaL_ref(state, LUA_REGISTRYINDEX);

            // Set up a container of generated metamethods to ensure that the metamethods do not get garbage collected
            // by the CLR.
            //
            _generatedMetamethods = new List<LuaCFunction>();
        }

        internal void PushTypeMetatable(IntPtr state, Type type)
        {
            lua_createtable(state, 0, 3);  // Include space for the `__gc` and `__tostring` metamethods

            PushTypeIndexMetavalue(state, type, null);
            lua_setfield(state, -2, "__index");

            // TODO: __newindex
            // TODO: __call
        }

        internal void PushGenericTypesMetatable(IntPtr state, Type[] types)
        {
            var maybeNonGenericType = types.FirstOrDefault(t => !t.IsGenericTypeDefinition);

            lua_createtable(state, 0, 3);  // Include space for the `__gc` and `__tostring` metamethods

            PushTypeIndexMetavalue(state, maybeNonGenericType, types);
            lua_setfield(state, -2, "__index");

            // TODO: __newindex
            // TODO: __call
        }

        internal void PushObjectMetatable(IntPtr state, Type objType)
        {
            lua_createtable(state, 0, 3);  // Include space for the `__gc` and `__tostring` metamethods

            lua_rawgeti(state, LUA_REGISTRYINDEX, _wrapObjectIndexReference);
            PushObjectIndexMetavalue(state, objType);
            lua_pcall(state, 1, 1, 0);
            lua_setfield(state, -2, "__index");

            // TODO: __newindex
            // TODO: metamethod operators
        }

        private void PushTypeIndexMetavalue(IntPtr state, Type? maybeNonGenericType, Type[]? maybeGenericTypes)
        {
            // If there is a non-generic type, then the metavalue should be a table with the cacheable members
            // pre-populated.
            //
            if (maybeNonGenericType is { })
            {
                lua_newtable(state);

                foreach (var field in maybeNonGenericType.GetPublicStaticFields().Where(f => f.IsLiteral))
                {
                    _environment.PushObject(state, field.GetValue(null));
                    lua_setfield(state, -2, field.Name);
                }

                // TODO: pre-populate events

                // TODO: pre-populate methods

                foreach (var nestedType in maybeNonGenericType.GetPublicNestedTypes())
                {
                    _environment.PushClrEntity(state, new ClrTypeProxy(nestedType));
                    lua_setfield(state, -2, nestedType.Name);
                }

                lua_createtable(state, 0, 1);
            }

            var indexMetamethod = ProtectedCall(GenerateIndexMetamethod(maybeNonGenericType, maybeGenericTypes));
            _generatedMetamethods.Add(indexMetamethod);
            lua_pushcfunction(state, indexMetamethod);

            if (maybeNonGenericType is { })
            {
                lua_setfield(state, -2, "__index");

                lua_setmetatable(state, -2);
            }

            LuaCFunction GenerateIndexMetamethod(Type? maybeNonGenericType, Type[]? maybeGenericTypes)
            {
                var context = new MetamethodContext(_environment);
                var metamethod = new DynamicMethod(
                    "__index", typeof(int), new[] { typeof(MetamethodContext), typeof(IntPtr) }, typeof(MetamethodContext));
                var ilg = metamethod.GetILGenerator();

                var keyType = EmitGetKeyType(ilg);

                // If there is a non-generic type, then support indexing non-cacheable members.
                //
                if (maybeNonGenericType is { })
                {
                    var fields = maybeNonGenericType.GetPublicStaticFields().Where(f => !f.IsLiteral);  // No consts
                    var properties = maybeNonGenericType.GetPublicStaticProperties();
                    var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                    context.SetMembers(members);

                    EmitIndexNonCacheableMembers(ilg, keyType,
                        members,
                        (ilg, field) =>
                        {
                            EmitLuaPush(ilg, field.FieldType,
                                ilg =>
                                {
                                    ilg.Emit(Ldsfld, field);
                                });
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        },
                        (ilg, property) =>
                        {
                            if (!property.CanRead || !property.GetMethod.IsPublic)
                            {
                                EmitLuaError(ilg, "attempt to get non-readable property");
                                ilg.Emit(Ret);
                                return;
                            }

                            if (property.PropertyType.IsByRefLike)
                            {
                                EmitLuaError(ilg, "attempt to get byref-like property");
                                ilg.Emit(Ret);
                                return;
                            }

                            EmitLuaPush(ilg, property.PropertyType,
                                ilg =>
                                {
                                    ilg.Emit(Call, property.GetMethod);
                                });
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        });
                }

                // If there are generic types, then support indexing generic types.
                //
                if (maybeGenericTypes is { })
                {
                    var arityToType = maybeGenericTypes
                        .Where(t => t.IsGenericTypeDefinition)
                        .ToDictionary(t => t.GetGenericArguments().Length);
                    var minArity = arityToType.Keys.Min();
                    var maxArity = arityToType.Keys.Max();

                    EmitIndexGenerics(ilg, keyType,
                        (ilg, typeArgs) =>
                        {
                            var exit = ilg.BeginExceptionBlock();
                            {
                                var cases = ilg.DefineLabels(maxArity - minArity + 1);

                                ilg.Emit(Ldloc, typeArgs);
                                ilg.Emit(Ldlen);
                                ilg.Emit(Ldc_I4, minArity);
                                ilg.Emit(Sub);
                                ilg.Emit(Switch, cases);

                                ilg.MarkLabels(cases.Where((_, i) => !arityToType.ContainsKey(i + minArity)));

                                EmitLuaError(ilg, "attempt to construct generic type with invalid arity");
                                ilg.Emit(Pop);
                                ilg.Emit(Leave, exit);  // Not short form

                                foreach (var (@case, type) in cases
                                    .Select((@case, i) => (@case, type: arityToType.GetValueOrDefault(i + minArity)))
                                    .Where(t => t.type is { }))
                                {
                                    ilg.MarkLabel(@case);

                                    ilg.Emit(Ldarg_0);
                                    ilg.Emit(Ldarg_1);
                                    ilg.Emit(Ldtoken, type);
                                    ilg.Emit(Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                                    ilg.Emit(Ldloc, typeArgs);
                                    ilg.Emit(Callvirt, typeof(Type).GetMethod(nameof(Type.MakeGenericType)));
                                    ilg.Emit(Call, MetamethodContext._pushClrType);
                                    ilg.Emit(Leave, exit);  // Not short form
                                }
                            }

                            ilg.BeginCatchBlock(typeof(ArgumentException));
                            {
                                ilg.Emit(Pop);
                                EmitLuaError(ilg, "attempt to construct generic type with invalid constraints");
                                ilg.Emit(Pop);
                                ilg.Emit(Leave_S, exit);
                            }

                            ilg.EndExceptionBlock();

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        });
                }

                EmitLuaError(ilg, "attempt to index with invalid key");
                ilg.Emit(Ret);

                return (LuaCFunction)metamethod.CreateDelegate(typeof(LuaCFunction), context);
            }
        }

        private void PushObjectIndexMetavalue(IntPtr state, Type objType)
        {
            lua_newtable(state);

            // TODO: pre-populate events

            // TODO: pre-populate methods

            lua_createtable(state, 0, 1);

            var indexMetamethod = ProtectedCall(GenerateIndexMetamethod(objType));
            _generatedMetamethods.Add(indexMetamethod);
            lua_pushcfunction(state, indexMetamethod);
            lua_setfield(state, -2, "__index");

            lua_setmetatable(state, -2);

            LuaCFunction GenerateIndexMetamethod(Type objType)
            {
                var context = new MetamethodContext(_environment);
                var metamethod = new DynamicMethod(
                    "__index", typeof(int), new[] { typeof(MetamethodContext), typeof(IntPtr) }, typeof(MetamethodContext));
                var ilg = metamethod.GetILGenerator();

                var obj = EmitGetObject(ilg, objType);
                var keyType = EmitGetKeyType(ilg);

                {
                    var fields = objType.GetPublicInstanceFields();
                    var properties = objType.GetPublicInstanceProperties();
                    var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                    context.SetMembers(members);

                    EmitIndexNonCacheableMembers(ilg, keyType,
                        members,
                        (ilg, field) =>
                        {
                            EmitLuaPush(ilg, field.FieldType,
                                ilg =>
                                {
                                    ilg.Emit(Ldloc, obj);
                                    ilg.Emit(Ldfld, field);
                                });
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        },
                        (ilg, property) =>
                        {
                            if (!property.CanRead || !property.GetMethod.IsPublic)
                            {
                                EmitLuaError(ilg, "attempt to get non-readable property");
                                ilg.Emit(Ret);
                                return;
                            }

                            if (property.PropertyType.IsByRefLike)
                            {
                                EmitLuaError(ilg, "attempt to get byref-like property");
                                ilg.Emit(Ret);
                                return;
                            }

                            EmitLuaPush(ilg, property.PropertyType,
                                ilg =>
                                {
                                    ilg.Emit(Ldloc, obj);
                                    ilg.Emit(Callvirt, property.GetMethod);
                                });
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        });
                }

                // TODO: support indexers (which requires overload support)

                // TODO: support array indexing

                EmitLuaError(ilg, "attempt to index with invalid key");
                ilg.Emit(Ret);

                return (LuaCFunction)metamethod.CreateDelegate(typeof(LuaCFunction), context);
            }
        }

        private LocalBuilder EmitGetObject(ILGenerator ilg, Type objType)
        {
            var isStruct = objType.IsValueType;
            var obj = ilg.DeclareLocal(isStruct ? objType.MakeByRefType() : objType);

            ilg.Emit(Ldarg_0);
            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_1);
            ilg.Emit(Call, MetamethodContext._toClrEntity);
            ilg.Emit(isStruct ? Unbox : Castclass, objType);
            ilg.Emit(Stloc, obj);

            return obj;
        }

        private LocalBuilder EmitGetKeyType(ILGenerator ilg)
        {
            var keyType = ilg.DeclareLocal(typeof(LuaType));

            ilg.Emit(Ldarg_1);
            ilg.Emit(Ldc_I4_2);
            ilg.Emit(Call, _lua_type);
            ilg.Emit(Stloc, keyType);

            return keyType;
        }

        private void EmitIndexNonCacheableMembers(
            ILGenerator ilg, LocalBuilder keyType, IReadOnlyList<MemberInfo> members,
            Action<ILGenerator, FieldInfo> fieldAction,
            Action<ILGenerator, PropertyInfo> propertyAction)
        {
            var isNotString = ilg.DefineLabel();
            var cases = ilg.DefineLabels(members.Count);

            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Ldc_I4_4);
            ilg.Emit(Bne_Un, isNotString);  // Not short form
            {
                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                ilg.Emit(Call, _lua_tostring);
                ilg.Emit(Call, MetamethodContext._matchMemberName);
                ilg.Emit(Switch, cases);

                EmitLuaError(ilg, "attempt to get invalid member");
                ilg.Emit(Ret);

                for (var i = 0; i < members.Count; ++i)
                {
                    ilg.MarkLabel(cases[i]);

                    switch (members[i])
                    {
                    case FieldInfo field:
                        fieldAction(ilg, field);
                        break;

                    case PropertyInfo property:
                        propertyAction(ilg, property);
                        break;

                    default:
                        throw new InvalidOperationException();
                    }
                }
            }

            ilg.MarkLabel(isNotString);
        }

        private void EmitIndexGenerics(
            ILGenerator ilg, LocalBuilder keyType,
            Action<ILGenerator, LocalBuilder> action)
        {
            var isUserdata = ilg.DefineLabel();
            var isNotTable = ilg.DefineLabel();

            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Ldc_I4_7);
            ilg.Emit(Beq_S, isUserdata);

            ilg.Emit(Ldloc, keyType);
            ilg.Emit(Ldc_I4_5);
            ilg.Emit(Bne_Un, isNotTable);  // Not short form
            {
                var typeArgs = ilg.DeclareLocal(typeof(Type[]));

                ilg.MarkLabel(isUserdata);

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                ilg.Emit(Ldloc, keyType);
                ilg.Emit(Call, MetamethodContext._toClrTypes);
                ilg.Emit(Stloc, typeArgs);

                action(ilg, typeArgs);
            }

            ilg.MarkLabel(isNotTable);
        }

        private LuaCFunction ProtectedCall(LuaCFunction callback) =>
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
    }
}
