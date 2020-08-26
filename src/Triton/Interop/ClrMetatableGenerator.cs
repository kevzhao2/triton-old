// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Triton.Interop.Extensions;
using static System.Reflection.Emit.OpCodes;
using static Triton.LuaValue;
using static Triton.NativeMethods;
using Debug = System.Diagnostics.Debug;

namespace Triton.Interop
{
    /// <summary>
    /// Generates metatables for CLR entities.
    /// </summary>
    internal sealed partial class ClrMetatableGenerator
    {
        private readonly LuaEnvironment _environment;

        // The `__index` metavalues are nested (i.e., the metavalue is a table which itself has an `__index`
        // metamethod).
        //
        // Normally, the table would be passed to the nested metamethod, but this is undesirable for object metamethods.
        //
        // In order to work around this, we have a higher order function which wraps the metavalue, producing a function
        // which will attempt to `rawget` the metavalue, and if that fails, calls the metavalue's `__index` metamethod,
        // passing the object instead.

        private readonly int _wrapObjectIndexRef;

        internal ClrMetatableGenerator(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            var status = luaL_loadstring(state, @"
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
            Debug.Assert(status == LuaStatus.Ok);

            _wrapObjectIndexRef = luaL_ref(state, LUA_REGISTRYINDEX);
        }

        /// <summary>
        /// Pushes the given CLR types' metatable onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="types">The CLR types.</param>
        public void PushTypesMetatable(IntPtr state, IReadOnlyList<Type> types)
        {
            var nonGenericType = types.SingleOrDefault(t => !t.IsGenericTypeDefinition);
            var hasNonGenericType = nonGenericType is { };

            lua_createtable(state, 0, hasNonGenericType ? 5 : 3);

            PushIndexMetavalue(state, types, isStatic: true);
            lua_setfield(state, -2, "__index");

            if (hasNonGenericType)
            {
                PushNewIndexMetamethod(state, nonGenericType!, isStatic: true);
                lua_setfield(state, -2, "__newindex");

                PushCallMetamethod(state, nonGenericType!, isStatic: true);
                lua_setfield(state, -2, "__call");
            }
        }

        /// <summary>
        /// Pushes the given CLR object type's metatable onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="objType">The CLR object type.</param>
        public void PushObjectMetatable(IntPtr state, Type objType)
        {
            var isDelegate = objType.IsSubclassOf(typeof(Delegate));

            lua_createtable(state, 0, isDelegate ? 5 : 4);

            lua_rawgeti(state, LUA_REGISTRYINDEX, _wrapObjectIndexRef);
            PushIndexMetavalue(state, new[] { objType }, isStatic: false);
            lua_pcall(state, 1, 1, 0);
            lua_setfield(state, -2, "__index");

            PushNewIndexMetamethod(state, objType, isStatic: false);
            lua_setfield(state, -2, "__newindex");

            if (isDelegate)
            {
                // TODO: __call
            }
        }

        /// <summary>
        /// Pushes the given methods' function onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="methods">The methods.</param>
        public void PushMethodsFunction(IntPtr state, IReadOnlyList<MethodInfo> methods)
        {
            Debug.Assert(methods.All(m => !m.IsGenericMethodDefinition),
                "Methods should not be generic");

            PushMethodsValue(state, methods, methods[0].IsStatic);
        }

        private void PushIndexMetavalue(IntPtr state, IReadOnlyList<Type> types, bool isStatic)
        {
            Debug.Assert(types.Count > 0);
            Debug.Assert(types.Count(t => !t.IsGenericTypeDefinition) <= 1);
            Debug.Assert(isStatic || types.Count == 1);

            var type = types.SingleOrDefault(t => !t.IsGenericTypeDefinition);
            var genericTypes = types.Where(t => t.IsGenericTypeDefinition).ToList();

            lua_newtable(state);
            if (type is { })
            {
                PopulateNonCacheableMembers(state, type, isStatic);
            }

            lua_createtable(state, 0, 1);
            PopulateMetatable(state, type, genericTypes, isStatic);
            lua_setmetatable(state, -2);
            return;

            void PopulateNonCacheableMembers(IntPtr state, Type type, bool isStatic)
            {
                if (isStatic)
                {
                    foreach (var constField in type.GetPublicFields(isStatic: true).Where(f => f.IsLiteral))
                    {
                        _environment.PushObject(state, constField.GetValue(null));
                        lua_setfield(state, -2, constField.Name);
                    }

                    foreach (var group in type.GetPublicNestedTypes().GroupBy(t => t.Name))
                    {
                        _environment.PushClrEntity(state, new ProxyClrTypes(group.ToArray()));
                        lua_setfield(state, -2, group.Key);
                    }
                }

                foreach (var @event in type.GetPublicEvents(isStatic))
                {
                    // TODO: events
                }

                foreach (var group in type.GetPublicMethods(isStatic).GroupBy(m => m.Name))
                {
                    PushMethodsValue(state, group.ToList(), isStatic);
                    lua_setfield(state, -2, group.Key);
                }
            }

            void PopulateMetatable(IntPtr state, Type? type, IReadOnlyList<Type> genericTypes, bool isStatic)
            {
                PushFunction(state, "__index", (ilg, context) =>
                {
                    var keyType = EmitDeclareKeyType(ilg);
                    if (type is { })
                    {
                        var fields = type.GetPublicFields(isStatic).Where(f => !f.IsLiteral);
                        var properties = type.GetPublicProperties(isStatic);

                        var members = fields.Cast<MemberInfo>()
                            .Concat(properties)
                            .ToList();
                        if (members.Count > 0)
                        {
                            context.SetMembers(state, members);

                            var isNotString = ilg.DefineLabel();

                            ilg.Emit(Ldloc, keyType);
                            ilg.Emit(Ldc_I4_4);
                            ilg.Emit(Bne_Un, isNotString);  // Not short form
                            {
                                var target = isStatic ? null : EmitDeclareTarget(ilg, type);

                                EmitAccessNonCacheableMembers(ilg, target, members);
                            }

                            ilg.MarkLabel(isNotString);
                        }

                        if (!isStatic)
                        {
                            if (type.IsSZArray)
                            {
                                var target = EmitDeclareTarget(ilg, type);
                                var index = EmitDeclareSzArrayIndex(ilg, keyType);

                                EmitAccessSzArray(ilg, target, index, type.GetElementType()!);
                            }
                            else if (type.IsArray)
                            {
                                // TODO: array access
                            }
                            else
                            {
                                // TODO: indexer access
                            }
                        }
                    }

                    if (genericTypes.Count > 0)
                    {
                        var typeArgs = EmitConstructTypeArgs(ilg, keyType);

                        EmitConstructGenericTypes(ilg, typeArgs, genericTypes);
                    }

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                    return;

                    static void EmitAccessNonCacheableMembers(
                        ILGenerator ilg, LocalBuilder? target, IReadOnlyList<MemberInfo> members)
                    {
                        var cases = ilg.DefineLabels(members.Count);

                        var isNonReadableProperty = LazyEmitErrorMemberName(
                            ilg, "attempt to get non-readable property '{0}'");
                        var isByRefLikeProperty = LazyEmitErrorMemberName(
                            ilg, "attempt to get byref-like property '{0}'");

                        ilg.Emit(Ldarg_0);
                        ilg.Emit(Ldarg_1);  // Lua state
                        ilg.Emit(Call, MetamethodContext._matchMemberName);
                        ilg.Emit(Switch, cases);

                        ilg.Emit(Ldc_I4_0);
                        ilg.Emit(Ret);

                        for (var i = 0; i < members.Count; ++i)
                        {
                            ilg.MarkLabel(cases[i]);

                            var member = members[i];
                            if (member is FieldInfo field)
                            {
                                using var temp = ilg.DeclareReusableLocal(field.FieldType);
                                ilg.EmitLdfld(target, field);
                                ilg.Emit(Stloc, temp);

                                EmitLuaPush(ilg, field.FieldType, temp);
                            }
                            else if (member is PropertyInfo property)
                            {
                                if (property.GetMethod?.IsPublic != true)
                                {
                                    ilg.Emit(Br, isNonReadableProperty.Value);  // Not short form
                                    continue;
                                }

                                if (property.PropertyType.IsByRefLike)
                                {
                                    ilg.Emit(Br, isByRefLikeProperty.Value);  // Not short form
                                    continue;
                                }

                                using var temp = ilg.DeclareReusableLocal(property.PropertyType);
                                ilg.EmitCall(target, property.GetMethod);
                                ilg.Emit(Stloc, temp);

                                EmitLuaPush(ilg, property.PropertyType, temp);
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        }
                    }

                    static void EmitAccessSzArray(
                        ILGenerator ilg, LocalBuilder target, LocalBuilder index, Type elementType)
                    {
                        using var temp = ilg.DeclareReusableLocal(elementType);
                        ilg.Emit(Ldloc, target);
                        ilg.Emit(Ldloc, index);
                        ilg.EmitLdelem(elementType);
                        ilg.Emit(Stloc, temp);

                        EmitLuaPush(ilg, elementType, temp);

                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Ret);
                    }

                    static void EmitConstructGenericTypes(
                        ILGenerator ilg, LocalBuilder typeArgs, IReadOnlyList<Type> genericTypes)
                    {
                        var arityToType = genericTypes.ToDictionary(t => t.GetGenericArguments().Length);
                        var minArity = arityToType.Keys.Min();
                        var maxArity = arityToType.Keys.Max();

                        var cases = ilg.DefineLabels(maxArity - minArity + 1);

                        ilg.Emit(Ldloc, typeArgs);
                        ilg.Emit(Ldlen);
                        ilg.Emit(Ldc_I4, minArity);
                        ilg.Emit(Sub);
                        ilg.Emit(Switch, cases);

                        ilg.MarkLabels(cases.Where((_, i) => !arityToType.ContainsKey(i + minArity)));

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldstr, "attempt to construct generic type with invalid arity");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);

                        foreach (var (arity, type) in arityToType)
                        {
                            ilg.MarkLabel(cases[arity - minArity]);

                            ilg.Emit(Ldarg_0);
                            ilg.Emit(Ldarg_1);  // Lua state
                            ilg.Emit(Ldtoken, type);
                            ilg.Emit(Ldloc, typeArgs);
                            ilg.Emit(Call, MetamethodContext._constructAndPushGenericType);

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        }
                    }



                    /*static void EmitAccessArrayIndexer(ILGenerator ilg)
                    {
                        // TODO: implement this

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldstr, "attempt to index array with invalid arguments");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);
                    }

                    void EmitAccessIndexers()
                    {
                        // TODO: implement this

                        /*var indexers = type.GetPublicIndexers().Select(i => i.GetMethod!).ToList();

                        var (numKeys, keyTypes) = EmitFlattenKey(ilg, keyType);

                        EmitCallMethods(
                            ilg, indexers,
                            ilg => ilg.Emit(Ldloc, numKeys),
                            (ilg, argIndex) =>
                            {
                                ilg.Emit(Ldloc, keyTypes);
                                ilg.Emit(Ldloc, argIndex);
                                ilg.Emit(Ldc_I4_1);
                                ilg.Emit(Sub);
                                ilg.Emit(Conv_I);
                                ilg.Emit(Ldc_I4_4);
                                ilg.Emit(Mul);
                                ilg.Emit(Add);
                                ilg.Emit(Ldind_I4);
                            },
                            (ilg, argIndex) =>
                            {
                                ilg.Emit(Ldloc, argIndex);
                                ilg.Emit(Ldc_I4_2);
                                ilg.Emit(Add);
                            },
                            (ilg, indexer, args, result) =>
                            {
                                ilg.Emit(Ldloc, target!);
                                foreach (var arg in args)
                                {
                                    ilg.Emit(Ldloc, arg);
                                }

                                ilg.Emit(Callvirt, (MethodInfo)indexer);
                                ilg.Emit(Stloc, result!);
                            });

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldstr, "attempt to call indexer with invalid arguments");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);
                    }*/
                });
                lua_setfield(state, -2, "__index");
            }
        }

        private void PushNewIndexMetamethod(IntPtr state, Type type, bool isStatic) =>
            PushFunction(state, "__newindex", (ilg, context) =>
            {
                var target = isStatic ? null : EmitDeclareTarget(ilg, type);
                var keyType = EmitDeclareKeyType(ilg);
                var valueType = EmitDeclareValueType(ilg);

                // Support indexing of the members.

                {
                    var fields = type.GetPublicFields(isStatic);
                    var events = type.GetPublicEvents(isStatic);
                    var properties = type.GetPublicProperties(isStatic);
                    var methods = type.GetPublicMethods(isStatic).GroupBy(m => m.Name).Select(g => g.First());
                    var nestedTypes = isStatic ? type.GetPublicNestedTypes() : Array.Empty<Type>();

                    var members = fields.Cast<MemberInfo>()
                        .Concat(events)
                        .Concat(properties)
                        .Concat(methods)
                        .Concat(nestedTypes)
                        .ToList();
                    context.SetMembers(state, members);

                    EmitNewIndexMembers(ilg, members,
                        ilg => ilg.Emit(Ldloc, keyType),
                        ilg => ilg.Emit(Ldloc, valueType),
                        (ilg, field, temp) =>
                        {
                            if (!isStatic)
                            {
                                ilg.Emit(Ldloc, target!);
                            }

                            ilg.Emit(Ldloc, temp);
                            ilg.Emit(isStatic ? Stsfld : Stfld, field);
                        },
                        (ilg, property, temp) =>
                        {
                            var propertyType = property.PropertyType;
                            var isByRef = property.PropertyType.IsByRef;

                            if (!isStatic)
                            {
                                ilg.Emit(Ldloc, target!);
                            }

                            if (isByRef)
                            {
                                ilg.Emit(isStatic ? Call : Callvirt, property.GetMethod!);
                                ilg.Emit(Ldloc, temp);
                                ilg.EmitStind(propertyType.GetElementType()!);
                            }
                            else
                            {
                                ilg.Emit(Ldloc, temp);
                                ilg.Emit(isStatic ? Call : Callvirt, property.SetMethod!);
                            }
                        });
                }

                // If not static, support indexing to access arrays and indexers.

                if (!isStatic)
                {
                    // TODO: array access
                    // TODO: indexer access
                }

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            });

        private void PushCallMetamethod(IntPtr state, Type type, bool isStatic) =>
            PushFunction(state, "__call", (ilg, _) =>
            {
                var constructors = type.GetConstructors();

                var argCount = EmitDeclareArgCount(ilg);

                EmitCallMethods(ilg, constructors,
                    ilg =>
                    {
                        ilg.Emit(Ldloc, argCount);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Sub);
                    },
                    (ilg, temp) =>
                    {
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldloc, temp);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Add);
                        ilg.Emit(Call, _lua_type);
                    },
                    (ilg, temp) =>
                    {
                        ilg.Emit(Ldloc, temp);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Add);
                    },
                    (ilg, clrMethodOrConstructor, args, temp) =>
                    {
                        if (type.IsValueType)
                        {
                            ilg.Emit(Ldloca, temp!);
                            foreach (var arg in args)
                            {
                                ilg.Emit(Ldloc, arg);
                            }

                            ilg.Emit(Call, (ConstructorInfo)clrMethodOrConstructor);
                        }
                        else
                        {
                            foreach (var arg in args)
                            {
                                ilg.Emit(Ldloc, arg);
                            }

                            ilg.Emit(Newobj, (ConstructorInfo)clrMethodOrConstructor);
                            ilg.Emit(Stloc, temp!);
                        }
                    });

                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldstr, "attempt to construct type with invalid arguments");
                ilg.Emit(Call, _luaL_error);
                ilg.Emit(Ret);
            });

        private void PushMethodsValue(IntPtr state, IReadOnlyList<MethodInfo> methods, bool isStatic)
        {
            Debug.Assert(methods.Count > 0,
                "Methods should not be empty");
            Debug.Assert(methods.Select(m => (m.Name, m.IsStatic)).Distinct().Count() == 1,
                "Methods should have the same name and static-ness");

            var nonGenericMethods = methods.Where(m => !m.IsGenericMethodDefinition).ToList();
            var genericMethods = methods.Where(m => m.IsGenericMethodDefinition).ToList();

            var hasNonGenericMethods = nonGenericMethods.Count > 0;
            var hasGenericMethods = genericMethods.Count > 0;

            var isNotStatic = !methods[0].IsStatic;

            // If there are generic methods, then the value is a table with an `__index` metamethod to support
            // instantiating generic methods and a `__call` metamethod to support calling the non-generic methods, if
            // there are any.
            //
            // Otherwise, the value is a function to support calling the non-generic methods.

            if (hasGenericMethods)
            {
                lua_newtable(state);

                lua_createtable(state, 0, hasNonGenericMethods ? 2 : 1);
                PushFunction(state, "__index", (ilg, _) =>
                {
                    var arityToMethods = genericMethods
                        .Where(m => m.IsGenericMethodDefinition)
                        .GroupBy(m => m.GetGenericArguments().Length)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    var minArity = arityToMethods.Keys.Min();
                    var maxArity = arityToMethods.Keys.Max();

                    var keyType = EmitDeclareKeyType(ilg);

                    EmitIndexTypeArgs(ilg,
                        ilg => ilg.Emit(Ldloc, keyType),
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

                                ilg.MarkLabels(cases.Where((_, i) => !arityToMethods.ContainsKey(i + minArity)));

                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldstr, "attempt to construct generic methods with invalid arity");
                                ilg.Emit(Call, _luaL_error);
                                ilg.Emit(Pop);
                                ilg.Emit(Leave, exit);  // Not short form

                                foreach (var (@case, methods) in cases
                                    .Select((@case, i) => (@case, type: arityToMethods.GetValueOrDefault(i + minArity)))
                                    .Where(t => t.type is { }))
                                {
                                    ilg.MarkLabel(@case);

                                    var constructedMethods = ilg.DeclareLocal(typeof(MethodInfo[]));

                                    ilg.Emit(Ldc_I4, methods.Count);
                                    ilg.Emit(Newarr, typeof(MethodInfo));
                                    ilg.Emit(Stloc, constructedMethods);

                                    for (var i = 0; i < methods.Count; ++i)
                                    {
                                        ilg.Emit(Ldloc, constructedMethods);
                                        ilg.Emit(Ldc_I4, i);
                                        ilg.Emit(Ldtoken, methods[i]);
                                        ilg.Emit(Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] { typeof(RuntimeMethodHandle) })!);
                                        ilg.Emit(Castclass, typeof(MethodInfo));
                                        ilg.Emit(Ldloc, typeArgs);
                                        ilg.Emit(Callvirt, typeof(MethodInfo).GetMethod(nameof(MethodInfo.MakeGenericMethod))!);
                                        ilg.Emit(Stelem_Ref);
                                    }

                                    ilg.Emit(Ldarg_0);
                                    ilg.Emit(Ldarg_1);
                                    ilg.Emit(Ldloc, constructedMethods);
                                    ilg.Emit(Call, MetamethodContext._pushClrMethods);
                                    ilg.Emit(Leave, exit);  // Not short form
                                }
                            }

                            ilg.BeginCatchBlock(typeof(ArgumentException));
                            {
                                ilg.Emit(Pop);
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldstr, "attempt to construct generic methods with invalid constraints");
                                ilg.Emit(Call, _luaL_error);
                                ilg.Emit(Pop);
                            }

                            ilg.EndExceptionBlock();

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        });

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);
                });
                lua_setfield(state, -2, "__index");
            }

            if (hasNonGenericMethods)
            {
                PushFunction(state, "__call", (ilg, _) =>
                {
                    // TODO: optimize this and below
                    if (hasGenericMethods)
                    {
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Call, _lua_remove);
                    }

                    var target = isNotStatic ? EmitDeclareTarget(ilg, methods[0].DeclaringType!) : null;

                    if (isNotStatic)
                    {
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Call, _lua_remove);
                    }

                    var argCount = EmitDeclareArgCount(ilg);

                    EmitCallMethods(ilg, nonGenericMethods,
                        ilg =>
                        {
                            ilg.Emit(Ldloc, argCount);
                        },
                        (ilg, argIndex) =>
                        {
                            ilg.Emit(Ldarg_1);
                            ilg.Emit(Ldloc, argIndex);
                            ilg.Emit(Call, _lua_type);
                        },
                        (ilg, argIndex) =>
                        {
                            ilg.Emit(Ldloc, argIndex);
                        },
                        (ilg, method, args, temp) =>
                        {
                            if (isNotStatic)
                            {
                                ilg.Emit(Ldloc, target!);
                            }

                            foreach (var arg in args)
                            {
                                ilg.Emit(Ldloc, arg);
                            }

                            ilg.EmitCall((MethodInfo)method);
                            if (temp is { })
                            {
                                ilg.Emit(Stloc, temp);
                            }
                        });

                    ilg.Emit(Ldarg_1);
                    ilg.Emit(Ldstr, "attempt to call method with invalid arguments");
                    ilg.Emit(Call, _luaL_error);
                    ilg.Emit(Ret);
                });

                if (hasGenericMethods)
                {
                    lua_setfield(state, -2, "__call");
                }
            }

            if (hasGenericMethods)
            {
                lua_setmetatable(state, -2);
            }
        }
    }
}
