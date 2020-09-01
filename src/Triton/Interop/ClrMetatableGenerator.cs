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
                PushCallMetamethod(state, objType, isStatic: false);
                lua_setfield(state, -2, "__call");
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
            var type = types.SingleOrDefault(t => !t.IsGenericTypeDefinition);
            var genericTypes = types.Where(t => t.IsGenericTypeDefinition).ToList();

            lua_newtable(state);
            if (type is not null)
            {
                PopulateCacheableMembers(state, type, isStatic);
            }

            lua_createtable(state, 0, 1);
            PopulateMetatable(state, type, genericTypes, isStatic);
            lua_setmetatable(state, -2);
            return;

            void PopulateCacheableMembers(IntPtr state, Type type, bool isStatic)
            {
                if (isStatic)
                {
                    foreach (var constField in type.GetPublicFields(isStatic: true).Where(f => f.IsLiteral))
                    {
                        _environment.PushObject(state, constField.GetValue(null));
                        lua_setfield(state, -2, constField.Name);
                    }

                    foreach (var (name, nestedTypes) in type.GetPublicNestedTypes().GroupBy(t => t.Name))
                    {
                        _environment.PushClrEntity(state, new ProxyClrTypes(nestedTypes.ToArray()));
                        lua_setfield(state, -2, name);
                    }
                }

                foreach (var @event in type.GetPublicEvents(isStatic))
                {
                    // TODO: events
                }

                foreach (var (name, methods) in type.GetPublicMethods(isStatic).GroupBy(m => m.Name))
                {
                    PushMethodsValue(state, methods.ToList(), isStatic);
                    lua_setfield(state, -2, name);
                }
            }

            void PopulateMetatable(IntPtr state, Type? type, IReadOnlyList<Type> genericTypes, bool isStatic)
            {
                PushFunction(state, "__index", (ilg, context) =>
                {
                    var target = !isStatic ? EmitDeclareTarget(ilg, type!) : null;
                    var keyType = EmitDeclareKeyType(ilg);

                    if (type is not null)
                    {
                        var members = Enumerable.Empty<MemberInfo>()
                            .Concat(type.GetPublicFields(isStatic).Where(f => !f.IsLiteral))
                            .Concat(type.GetPublicProperties(isStatic))
                            .ToList();
                        if (members.Count > 0)
                        {
                            context.SetMembers(state, members);

                            var (index, isNotMember) = EmitDeclareMemberIndex(ilg, keyType);
                            {
                                EmitAccessNonCacheableMembers(ilg, target, index, members);
                            }

                            ilg.MarkLabel(isNotMember);
                        }

                        if (!isStatic)
                        {
                            if (type.IsSZArray)
                            {
                                var elementType = type.GetElementType()!;

                                var index = EmitDeclareSzArrayIndex(ilg, keyType);

                                EmitAccessArray(ilg, elementType,
                                    (ilg, value) =>
                                    {
                                        ilg.Emit(Ldloc, target!);
                                        ilg.Emit(Ldloc, index);
                                        ilg.EmitLdelem(elementType);
                                        ilg.Emit(Stloc, value);
                                    });
                            }
                            else if (type.IsArray)
                            {
                                var rank = type.GetArrayRank();
                                var elementType = type.GetElementType()!;

                                var indices = EmitDeclareArrayIndices(ilg, keyType, rank);

                                EmitAccessArray(ilg, elementType,
                                    (ilg, value) =>
                                    {
                                        ilg.Emit(Ldloc, target!);
                                        for (var i = 0; i < rank; ++i)
                                        {
                                            ilg.Emit(Ldloca, indices);
                                            ilg.Emit(Ldc_I4, i);
                                            ilg.Emit(Call, typeof(Span<int>).GetProperty("Item")!.GetMethod!);
                                            ilg.Emit(Ldind_I4);
                                        }
                                        ilg.Emit(Callvirt, type.GetMethod("Get")!);
                                        ilg.Emit(Stloc, value);
                                    });
                            }
                            else
                            {
                                var indexers = type.GetPublicIndexers()
                                    .Where(i => i.GetMethod is not null)
                                    .Select(i => i.GetMethod!)
                                    .ToList();
                                if (indexers.Count > 0)
                                {
                                    var argCount = EmitDeclareIndexerArgCount(ilg, keyType, null);
                                    var argTypes = EmitDeclareIndexerArgTypes(ilg, keyType, null, argCount);

                                    EmitCallMethods(ilg, target, indexers, argCount, argTypes);

                                    EmitLuaError(ilg, "attempt to index object with invalid args");
                                    ilg.Emit(Ret);
                                }
                            }
                        }
                    }

                    if (genericTypes.Count > 0)
                    {
                        var typeArgs = EmitDeclareTypeArgs(ilg, keyType);

                        EmitConstructGenericType(ilg, typeArgs, genericTypes);
                    }

                    EmitLuaError(ilg, $"attempt to index {(target is not null ? "object" : "type")} with invalid key");
                    ilg.Emit(Ret);
                    return;

                    static void EmitAccessNonCacheableMembers(
                        ILGenerator ilg, LocalBuilder? target, LocalBuilder index, IReadOnlyList<MemberInfo> members)
                    {
                        var cases = ilg.DefineLabels(members.Count);

                        ilg.Emit(Ldloc, index);
                        ilg.Emit(Switch, cases);

                        ilg.Emit(Ldc_I4_0);
                        ilg.Emit(Ret);

                        for (var i = 0; i < members.Count; ++i)
                        {
                            ilg.MarkLabel(cases[i]);

                            var member = members[i];
                            if (member is FieldInfo field)
                            {
                                var fieldType = field.FieldType;

                                using var value = ilg.DeclareReusableLocal(fieldType);
                                if (target is not null)
                                {
                                    ilg.Emit(Ldloc, target);
                                }
                                ilg.Emit(target is not null ? Ldfld : Ldsfld, field);
                                ilg.Emit(Stloc, value);

                                EmitLuaPush(ilg, fieldType, value);
                            }
                            else if (member is PropertyInfo property)
                            {
                                var propertyType = property.PropertyType;
                                if (property.GetMethod?.IsPublic != true)
                                {
                                    EmitLuaError(ilg, "attempt to get non-readable property");
                                    ilg.Emit(Ret);
                                    continue;
                                }

                                if (propertyType.IsByRefLike)
                                {
                                    EmitLuaError(ilg, "attempt to get byref-like property");
                                    ilg.Emit(Ret);
                                    continue;
                                }

                                using var temp = ilg.DeclareReusableLocal(propertyType);
                                if (target is not null)
                                {
                                    ilg.Emit(Ldloc, target);
                                }
                                ilg.Emit(target is not null ? Callvirt : Call, property.GetMethod);
                                ilg.Emit(Stloc, temp);

                                EmitLuaPush(ilg, propertyType, temp);
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        }
                    }

                    static void EmitAccessArray(
                        ILGenerator ilg, Type elementType, Action<ILGenerator, LocalBuilder> getArray)
                    {
                        using var value = ilg.DeclareReusableLocal(elementType);
                        getArray(ilg, value);
                        EmitLuaPush(ilg, elementType, value);

                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Ret);
                    } 

                    static void EmitConstructGenericType(
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

                        EmitLuaError(ilg, "attempt to construct generic type of invalid arity");
                        ilg.Emit(Ret);

                        foreach (var (arity, type) in arityToType)
                        {
                            ilg.MarkLabel(cases[arity - minArity]);

                            ilg.Emit(Ldarg_0);
                            ilg.Emit(Ldarg_1);  // Lua state
                            ilg.Emit(Ldtoken, type);
                            ilg.Emit(Ldloc, typeArgs);
                            ilg.Emit(Call, typeof(MetamethodContext).GetMethod(nameof(MetamethodContext.PushGenericType))!);

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        }
                    }
                });
                lua_setfield(state, -2, "__index");
            }
        }

        private void PushNewIndexMetamethod(IntPtr state, Type type, bool isStatic) =>
            PushFunction(state, "__newindex", (ilg, context) =>
            {
                var target = !isStatic ? EmitDeclareTarget(ilg, type) : null;
                var keyType = EmitDeclareKeyType(ilg);
                var valueType = EmitDeclareValueType(ilg);
                var valueIndex = EmitDeclareValueIndex(ilg);

                var members = Enumerable.Empty<MemberInfo>()
                    .Concat(type.GetPublicFields(isStatic))
                    .Concat(type.GetPublicEvents(isStatic))
                    .Concat(type.GetPublicProperties(isStatic))
                    .Concat(type.GetPublicMethods(isStatic).GroupBy(m => m.Name).Select(g => g.First()))
                    .Concat(isStatic ? type.GetPublicNestedTypes() : Enumerable.Empty<Type>())
                    .ToList();
                if (members.Count > 0)
                {
                    context.SetMembers(state, members);

                    var (index, isNotMember) = EmitDeclareMemberIndex(ilg, keyType);
                    {
                        EmitAccessMembers(ilg, target, index, members);
                    }

                    ilg.MarkLabel(isNotMember);
                }

                if (!isStatic)
                {
                    if (type.IsSZArray)
                    {
                        var elementType = type.GetElementType()!;

                        var index = EmitDeclareSzArrayIndex(ilg, keyType);

                        EmitAccessArray(ilg, elementType,
                            (ilg, value) =>
                            {
                                ilg.Emit(Ldloc, target!);
                                ilg.Emit(Ldloc, index);
                                ilg.Emit(Ldloc, value);
                                ilg.EmitStelem(elementType);
                            });
                    }
                    else if (type.IsArray)
                    {
                        var rank = type.GetArrayRank();
                        var elementType = type.GetElementType()!;

                        var indices = EmitDeclareArrayIndices(ilg, keyType, rank);

                        EmitAccessArray(ilg, elementType,
                            (ilg, value) =>
                            {
                                ilg.Emit(Ldloc, target!);
                                for (var i = 0; i < rank; ++i)
                                {
                                    ilg.Emit(Ldloca, indices);
                                    ilg.Emit(Ldc_I4, i);
                                    ilg.Emit(Call, typeof(Span<int>).GetProperty("Item")!.GetMethod!);
                                    ilg.Emit(Ldind_I4);
                                }
                                ilg.Emit(Ldloc, value);
                                ilg.Emit(Callvirt, type.GetMethod("Set")!);
                            });
                    }
                    else
                    {
                        var indexers = type.GetPublicIndexers()
                            .Where(i => i.SetMethod is not null)
                            .Select(i => i.SetMethod!)
                            .ToList();
                        if (indexers.Count > 0)
                        {
                            var argCount = EmitDeclareIndexerArgCount(ilg, keyType, valueType);
                            var argTypes = EmitDeclareIndexerArgTypes(ilg, keyType, valueType, argCount);

                            EmitCallMethods(ilg, target, indexers, argCount, argTypes);

                            EmitLuaError(ilg, "attempt to index object with invalid args");
                            ilg.Emit(Ret);
                        }
                    }
                }

                EmitLuaError(ilg, $"attempt to index {(target is not null ? "object" : "type")} with invalid key");
                ilg.Emit(Ret);
                return;

                void EmitAccessMembers(
                    ILGenerator ilg, LocalBuilder? target, LocalBuilder index, IReadOnlyList<MemberInfo> members)
                {
                    var cases = ilg.DefineLabels(members.Count);

                    var invalidFieldValue = ilg.DefineLabel();
                    var invalidPropertyValue = ilg.DefineLabel();

                    ilg.Emit(Ldloc, index);
                    ilg.Emit(Switch, cases);

                    EmitLuaError(ilg, "attempt to set invalid member");
                    ilg.Emit(Ret);

                    for (var i = 0; i < members.Count; ++i)
                    {
                        ilg.MarkLabel(cases[i]);

                        var member = members[i];
                        if (member is FieldInfo field)
                        {
                            var fieldType = field.FieldType;
                            if (field.IsLiteral || field.IsInitOnly)
                            {
                                EmitLuaError(ilg, "attempt to set non-writable field");
                                ilg.Emit(Ret);
                                continue;
                            }

                            using var value = ilg.DeclareReusableLocal(fieldType);
                            EmitLuaLoad(ilg, fieldType, value, valueType, valueIndex, invalidFieldValue);

                            if (target is not null)
                            {
                                ilg.Emit(Ldloc, target);
                            }
                            ilg.Emit(Ldloc, value);
                            ilg.Emit(target is not null ? Stfld : Stsfld, field);
                        }
                        else if (member is PropertyInfo property)
                        {
                            var propertyType = property.PropertyType;
                            var isByRef = property.PropertyType.IsByRef;
                            if (isByRef)
                            {
                                propertyType = propertyType.GetElementType()!;
                            }

                            if ((isByRef ? property.GetMethod : property.SetMethod)?.IsPublic != true)
                            {
                                EmitLuaError(ilg, "attempt to set non-writable property");
                                ilg.Emit(Ret);
                                continue;
                            }

                            if (propertyType.IsByRefLike)
                            {
                                EmitLuaError(ilg, "attempt to set byref-like property");
                                ilg.Emit(Ret);
                                continue;
                            }

                            using var value = ilg.DeclareReusableLocal(propertyType);
                            EmitLuaLoad(ilg, propertyType, value, valueType, valueIndex, invalidPropertyValue);

                            if (target is not null)
                            {
                                ilg.Emit(Ldloc, target);
                            }

                            if (isByRef)
                            {
                                ilg.Emit(target is not null ? Callvirt : Call, property.GetMethod!);
                                ilg.Emit(Ldloc, value);
                                ilg.EmitStind(propertyType);
                            }
                            else
                            {
                                ilg.Emit(Ldloc, value);
                                ilg.Emit(target is not null ? Callvirt : Call, property.SetMethod!);
                            }
                        }
                        else
                        {
                            EmitLuaError(ilg, member switch
                            {
                                EventInfo  => "attempt to set event",
                                MethodInfo => "attempt to set method",
                                Type       => "attempt to set nested type",
                                _          => throw new InvalidOperationException()
                            });
                            ilg.Emit(Ret);
                            continue;
                        }

                        ilg.Emit(Ldc_I4_0);
                        ilg.Emit(Ret);
                    }

                    ilg.MarkLabel(invalidFieldValue);
                    EmitLuaError(ilg, "attempt to set field with invalid value");
                    ilg.Emit(Ret);

                    ilg.MarkLabel(invalidPropertyValue);
                    EmitLuaError(ilg, "attempt to set property with invalid value");
                    ilg.Emit(Ret);
                }

                void EmitAccessArray(
                    ILGenerator ilg, Type elementType, Action<ILGenerator, LocalBuilder> setArray)
                {
                    var isInvalidValue = ilg.DefineLabel();

                    using var value = ilg.DeclareReusableLocal(elementType);
                    EmitLuaLoad(ilg, elementType, value, valueType, valueIndex, isInvalidValue);
                    setArray(ilg, value);

                    ilg.Emit(Ldc_I4_0);
                    ilg.Emit(Ret);

                    ilg.MarkLabel(isInvalidValue);
                    EmitLuaError(ilg, "attempt to set array with invalid value");
                    ilg.Emit(Ret);
                }
            });

        private void PushCallMetamethod(IntPtr state, Type type, bool isStatic) =>
            PushFunction(state, "__call", (ilg, _) =>
            {
                var target = !isStatic ? EmitDeclareTarget(ilg, type!) : null;
                var argCount = EmitDeclareArgCount(ilg, 1);
                var argTypes = EmitDeclareArgTypes(ilg, argCount);

                if (isStatic)
                {
                    var constructors = type.GetConstructors();

                    EmitCallMethods(ilg, null, constructors, argCount, argTypes);

                    EmitLuaError(ilg, "attempt to construct type with invalid args");
                    ilg.Emit(Ret);
                }
                else
                {
                    var isInvalidCall = ilg.DefineLabel();

                    EmitCallMethod(ilg, target, type.GetMethod("Invoke")!, argCount, argTypes, isInvalidCall);

                    ilg.MarkLabel(isInvalidCall);
                    EmitLuaError(ilg, "attempt to call delegate with invalid args");
                    ilg.Emit(Ret);
                }
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
