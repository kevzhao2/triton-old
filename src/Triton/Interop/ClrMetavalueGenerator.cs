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
using static System.Reflection.Emit.OpCodes;
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    internal sealed class ClrMetavalueGenerator
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

                // Const and readonly fields
                foreach (var field in maybeNonGenericType.GetPublicStaticFields().Where(f => f.IsLiteral || f.IsInitOnly))
                {
                    _environment.PushObject(state, field.GetValue(null));
                    lua_setfield(state, -2, field.Name);
                }

                // TODO: populate events
                // TODO: populate methods

                // Nested types
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

            LuaCFunction GenerateIndexMetamethod(Type? nonGenericType, Type[]? maybeGenericTypes)
            {
                var metamethod = new DynamicMethod(
                    "__index", typeof(int), new[] { typeof(MetamethodContext), typeof(IntPtr) }, typeof(MetamethodContext));
                var ilg = metamethod.GetILGenerator();

                var keyType = ilg.DeclareLocal(typeof(LuaType));

                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                ilg.Emit(Call, _lua_type);
                ilg.Emit(Stloc, keyType);

                // If there are generic types, then support generic type construction.
                //
                if (maybeGenericTypes is { })
                {
                    var tempType = ilg.DeclareLocal(typeof(Type));
                    var typeArguments = ilg.DeclareLocal(typeof(Type[]));

                    var isNotUserdata = ilg.DefineLabel();
                    var isNotTable = ilg.DefineLabel();
                    var constructGenericType = ilg.DefineLabel();

                    ilg.Emit(Ldloc, keyType);
                    ilg.Emit(Ldc_I4_7);
                    ilg.Emit(Bne_Un_S, isNotUserdata);
                    {
                        var isType = ilg.DefineLabel();

                        ilg.Emit(Ldarg_0);
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldc_I4_2);
                        ilg.Emit(Call, MetamethodContext._toClrType);
                        ilg.Emit(Stloc, tempType);

                        ilg.Emit(Ldloc, tempType);
                        ilg.Emit(Brtrue_S, isType);
                        {
                            ilg.Emit(Ldarg_1);
                            ilg.Emit(Ldstr, "attempt to construct generic type with non-type arg");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Ret);
                        }

                        ilg.MarkLabel(isType);

                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Newarr, typeof(Type));
                        ilg.Emit(Dup);
                        ilg.Emit(Ldc_I4_0);
                        ilg.Emit(Ldloc, tempType);
                        ilg.Emit(Stelem_Ref);
                        ilg.Emit(Stloc, typeArguments);

                        ilg.Emit(Br_S, constructGenericType);
                    }

                    ilg.MarkLabel(isNotUserdata);

                    ilg.Emit(Ldloc, keyType);
                    ilg.Emit(Ldc_I4_5);
                    ilg.Emit(Bne_Un, isNotTable);  // Not short form since it branches over variable-length code
                    {
                        var length = ilg.DeclareLocal(typeof(int));
                        var i = ilg.DeclareLocal(typeof(int));

                        var loopBegin = ilg.DefineLabel();

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldc_I4_2);
                        ilg.Emit(Call, _lua_rawlen);
                        ilg.Emit(Conv_I4);
                        ilg.Emit(Stloc, length);

                        ilg.Emit(Ldloc, length);
                        ilg.Emit(Newarr, typeof(Type));
                        ilg.Emit(Stloc, typeArguments);

                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Stloc, i);
                        ilg.Emit(Br_S, loopBegin);
                        {
                            var loopBody = ilg.DefineLabel();
                            var isUserdata = ilg.DefineLabel();
                            var isType = ilg.DefineLabel();

                            ilg.MarkLabel(loopBody);

                            ilg.Emit(Ldarg_1);
                            ilg.Emit(Ldc_I4_2);
                            ilg.Emit(Ldloc, i);
                            ilg.Emit(Call, _lua_rawgeti);
                            ilg.Emit(Ldc_I4_7);
                            ilg.Emit(Beq_S, isUserdata);
                            {
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldstr, "attempt to construct generic type with non-type arg");
                                ilg.Emit(Call, _luaL_error);
                                ilg.Emit(Ret);
                            }

                            ilg.MarkLabel(isUserdata);

                            ilg.Emit(Ldarg_0);
                            ilg.Emit(Ldarg_1);
                            ilg.Emit(Ldc_I4_M1);
                            ilg.Emit(Call, MetamethodContext._toClrType);
                            ilg.Emit(Stloc, tempType);

                            ilg.Emit(Ldloc, tempType);
                            ilg.Emit(Brtrue_S, isType);
                            {
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldstr, "attempt to construct generic type with non-type arg");
                                ilg.Emit(Call, _luaL_error);
                                ilg.Emit(Ret);
                            }

                            ilg.MarkLabel(isType);

                            ilg.Emit(Ldloc, typeArguments);
                            ilg.Emit(Ldloc, i);
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Sub);
                            ilg.Emit(Ldloc, tempType);
                            ilg.Emit(Stelem_Ref);

                            ilg.Emit(Ldloc, i);
                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Add);
                            ilg.Emit(Stloc, i);

                            ilg.MarkLabel(loopBegin);

                            ilg.Emit(Ldloc, i);
                            ilg.Emit(Ldloc, length);
                            ilg.Emit(Ble_S, loopBody);
                        }

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldloc, length);
                        ilg.Emit(Call, _lua_pop);

                        ilg.MarkLabel(constructGenericType);

                        var exit = ilg.BeginExceptionBlock();
                        {
                            var arityToType = maybeGenericTypes
                                .Where(t => t.IsGenericTypeDefinition)
                                .ToDictionary(t => t.GetGenericArguments().Length);
                            var minArity = arityToType.Keys.Min();
                            var maxArity = arityToType.Keys.Max();

                            var cases = ilg.DefineLabels(maxArity - minArity + 1);

                            ilg.Emit(Ldloc, typeArguments);
                            ilg.Emit(Ldlen);
                            ilg.Emit(Ldc_I4, minArity);
                            ilg.Emit(Sub);
                            ilg.Emit(Switch, cases);

                            ilg.MarkLabels(cases.Where((_, i) => !arityToType.ContainsKey(i + minArity)));

                            ilg.Emit(Ldarg_1);
                            ilg.Emit(Ldstr, "attempt to construct generic type with invalid arity");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Pop);
                            ilg.Emit(Leave, exit);  // Not short form since it branches over variable-length code

                            foreach (var (@case, type) in cases
                                .Select((@case, i) => (@case, type: arityToType.GetValueOrDefault(i + minArity)))
                                .Where(t => t.type is { }))
                            {
                                ilg.MarkLabel(@case);

                                ilg.Emit(Ldarg_0);
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldtoken, type);
                                ilg.Emit(Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                                ilg.Emit(Ldloc, typeArguments);
                                ilg.Emit(Callvirt, typeof(Type).GetMethod(nameof(Type.MakeGenericType)));
                                ilg.Emit(Call, MetamethodContext._pushClrType);
                                ilg.Emit(Leave, exit);  // Not short form since it branches over variable-length code
                            }
                        }

                        ilg.BeginCatchBlock(typeof(ArgumentException));
                        {
                            ilg.Emit(Pop);
                            ilg.Emit(Ldarg_1);
                            ilg.Emit(Ldstr, "attempt to construct generic type with invalid constraints");
                            ilg.Emit(Call, _luaL_error);
                            ilg.Emit(Pop);
                            ilg.Emit(Leave_S, exit);
                        }

                        ilg.EndExceptionBlock();

                        ilg.Emit(Ldc_I4_1);
                        ilg.Emit(Ret);
                    }

                    ilg.MarkLabel(isNotTable);
                }

                IReadOnlyList<string> memberNames = Array.Empty<string>();

                // If there is a non-generic type, then support getting non-cacheable members.
                //
                if (nonGenericType is { })
                {
                    var isNotString = ilg.DefineLabel();

                    ilg.Emit(Ldloc, keyType);
                    ilg.Emit(Ldc_I4_4);
                    ilg.Emit(Bne_Un, isNotString);  // Not short form since it branches over variable-length code
                    {
                        var fields = nonGenericType.GetPublicStaticFields().Where(f => !f.IsLiteral && !f.IsInitOnly);
                        var properties = nonGenericType.GetPublicStaticProperties();
                        var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                        memberNames = members.Select(m => m.Name).ToList();

                        var cases = ilg.DefineLabels(members.Count);

                        ilg.Emit(Ldarg_0);
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldc_I4_2);
                        ilg.Emit(Call, _lua_tostring);
                        ilg.Emit(Call, MetamethodContext._matchMemberName);
                        ilg.Emit(Switch, cases);

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldstr, "attempt to get invalid member");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);

                        for (var i = 0; i < members.Count; ++i)
                        {
                            ilg.MarkLabel(cases[i]);

                            var member = members[i];
                            if (member is FieldInfo { FieldType: var fieldType } field)
                            {
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldsfld, field);
                                ilg.EmitLuaPush(fieldType);
                                ilg.Emit(Ldc_I4_1);
                                ilg.Emit(Ret);
                            }
                            else if (member is PropertyInfo { PropertyType: var propertyType } property)
                            {
                                if (!property.CanRead || !property.GetMethod.IsPublic)
                                {
                                    ilg.Emit(Ldarg_1);
                                    ilg.Emit(Ldstr, "attempt to get non-readable property");
                                    ilg.Emit(Call, _luaL_error);
                                    ilg.Emit(Ret);
                                    continue;
                                }

                                if (propertyType.IsByRefLike)
                                {
                                    ilg.Emit(Ldarg_1);
                                    ilg.Emit(Ldstr, "attempt to get byref-like property");
                                    ilg.Emit(Call, _luaL_error);
                                    ilg.Emit(Ret);
                                    continue;
                                }

                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Call, property.GetMethod);

                                if (propertyType.IsByRef)
                                {
                                    propertyType = propertyType.GetElementType();
                                    ilg.EmitLdind(propertyType);
                                }

                                ilg.EmitLuaPush(propertyType);
                                ilg.Emit(Ldc_I4_1);
                                ilg.Emit(Ret);
                            }
                        }
                    }

                    ilg.MarkLabel(isNotString);
                }

                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldstr, "attempt to index with invalid key");
                ilg.Emit(Call, _luaL_error);
                ilg.Emit(Ret);

                var context = new MetamethodContext(_environment, memberNames);
                return (LuaCFunction)metamethod.CreateDelegate(typeof(LuaCFunction), context);
            }
        }

        private void PushObjectIndexMetavalue(IntPtr state, Type objType)
        {
            lua_newtable(state);

            // TODO: events
            // TODO: methods

            lua_createtable(state, 0, 1);

            var indexMetamethod = ProtectedCall(GenerateIndexMetamethod(objType));
            _generatedMetamethods.Add(indexMetamethod);
            lua_pushcfunction(state, indexMetamethod);
            lua_setfield(state, -2, "__index");

            lua_setmetatable(state, -2);

            LuaCFunction GenerateIndexMetamethod(Type objType)
            {
                var metamethod = new DynamicMethod(
                    "__index", typeof(int), new[] { typeof(MetamethodContext), typeof(IntPtr) }, typeof(MetamethodContext));
                var ilg = metamethod.GetILGenerator();

                // Use a by-ref local for structs to eliminate copying.
                //
                var isStruct = objType.IsValueType;
                var instance = ilg.DeclareLocal(isStruct ? objType.MakeByRefType() : objType);
                var keyType = ilg.DeclareLocal(typeof(LuaType));

                ilg.Emit(Ldarg_0);
                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_1);
                ilg.Emit(Call, MetamethodContext._toClrEntity);
                ilg.Emit(isStruct ? Unbox : Castclass, objType);
                ilg.Emit(Stloc, instance);

                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldc_I4_2);
                ilg.Emit(Call, _lua_type);
                ilg.Emit(Stloc, keyType);

                IReadOnlyList<string> memberNames = Array.Empty<string>();

                {
                    var isNotString = ilg.DefineLabel();

                    ilg.Emit(Ldloc, keyType);
                    ilg.Emit(Ldc_I4_4);
                    ilg.Emit(Bne_Un, isNotString);  // Not short form since it branches over variable-length code
                    {
                        var fields = objType.GetPublicInstanceFields();
                        var properties = objType.GetPublicInstanceProperties();
                        var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                        memberNames = members.Select(m => m.Name).ToList();

                        var cases = ilg.DefineLabels(members.Count);

                        ilg.Emit(Ldarg_0);
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldc_I4_2);
                        ilg.Emit(Call, _lua_tostring);
                        ilg.Emit(Call, MetamethodContext._matchMemberName);
                        ilg.Emit(Switch, cases);

                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldstr, "attempt to get invalid member");
                        ilg.Emit(Call, _luaL_error);
                        ilg.Emit(Ret);

                        for (var i = 0; i < members.Count; ++i)
                        {
                            ilg.MarkLabel(cases[i]);

                            var member = members[i];
                            if (member is FieldInfo { FieldType: var fieldType } field)
                            {
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldloc, instance);
                                ilg.Emit(Ldfld, field);
                                ilg.EmitLuaPush(fieldType);
                                ilg.Emit(Ldc_I4_1);
                                ilg.Emit(Ret);
                            }
                            else if (member is PropertyInfo { PropertyType: var propertyType } property)
                            {
                                if (!property.CanRead || !property.GetMethod.IsPublic)
                                {
                                    ilg.Emit(Ldarg_1);
                                    ilg.Emit(Ldstr, "attempt to get non-readable property");
                                    ilg.Emit(Call, _luaL_error);
                                    ilg.Emit(Ret);
                                    continue;
                                }

                                if (propertyType.IsByRefLike)
                                {
                                    ilg.Emit(Ldarg_1);
                                    ilg.Emit(Ldstr, "attempt to get byref-like property");
                                    ilg.Emit(Call, _luaL_error);
                                    ilg.Emit(Ret);
                                    continue;
                                }

                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldloc, instance);
                                ilg.Emit(Callvirt, property.GetMethod);

                                if (propertyType.IsByRef)
                                {
                                    propertyType = propertyType.GetElementType();
                                    ilg.EmitLdind(propertyType);
                                }

                                ilg.EmitLuaPush(propertyType);
                                ilg.Emit(Ldc_I4_1);
                                ilg.Emit(Ret);
                            }
                        }
                    }

                    // TODO: support indexers (which requires overloading)
                    // TODO: support array indexing

                    ilg.MarkLabel(isNotString);
                }

                ilg.Emit(Ldarg_1);
                ilg.Emit(Ldstr, "attempt to index with invalid key");
                ilg.Emit(Call, _luaL_error);
                ilg.Emit(Ret);

                var context = new MetamethodContext(_environment, memberNames);
                return (LuaCFunction)metamethod.CreateDelegate(typeof(LuaCFunction), context);
            }
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
