// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Triton.Interop.Extensions;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Generates metavalues for CLR entities.
    /// </summary>
    internal sealed partial class ClrMetavalueGenerator
    {
        private static readonly MethodInfo _typeGetTypeFromHandle =
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        private static readonly MethodInfo _typeMakeGenericType =
            typeof(Type).GetMethod(nameof(Type.MakeGenericType))!;

        private readonly LuaEnvironment _environment;

        private readonly int _wrapObjectIndexRef;

        internal ClrMetavalueGenerator(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

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
            _wrapObjectIndexRef = luaL_ref(state, LUA_REGISTRYINDEX);
        }

        /// <summary>
        /// Pushes the given CLR type's metatable onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        public void PushTypeMetatable(IntPtr state, Type type)
        {
            lua_createtable(state, 0, 5);

            PushTypeIndexMetavalue(state, type, null);
            lua_setfield(state, -2, "__index");

            PushTypeNewIndexMetavalue(state, type);
            lua_setfield(state, -2, "__newindex");

            PushTypeCallMetavalue(state, type);
            lua_setfield(state, -2, "__call");
        }

        /// <summary>
        /// Pushes the given generic CLR types' metatable onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="types">The generic CLR types.</param>
        public void PushGenericTypesMetatable(IntPtr state, Type[] types)
        {
            var maybeNonGenericType = types.SingleOrDefault(t => !t.IsGenericTypeDefinition);

            lua_createtable(state, 0, maybeNonGenericType is null ? 3 : 5);

            PushTypeIndexMetavalue(state, maybeNonGenericType, types);
            lua_setfield(state, -2, "__index");

            if (maybeNonGenericType is { } nonGenericType)
            {
                PushTypeNewIndexMetavalue(state, nonGenericType);
                lua_setfield(state, -2, "__newindex");

                PushTypeCallMetavalue(state, nonGenericType);
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
            lua_createtable(state, 0, 5);

            lua_rawgeti(state, LUA_REGISTRYINDEX, _wrapObjectIndexRef);
            PushObjectIndexMetavalue(state, objType);
            lua_pcall(state, 1, 1, 0);
            lua_setfield(state, -2, "__index");

            PushObjectNewIndexMetavalue(state, objType);
            lua_setfield(state, -2, "__newindex");
        }

        private void PushTypeIndexMetavalue(IntPtr state, Type? maybeNonGenericType, Type[]? maybeGenericTypes)
        {
            // If there is a non-generic type, then the metavalue should be a table with the cacheable members
            // pre-populated. Otherwise, the metavalue is just the `__index` metamethod.

            if (maybeNonGenericType is { } nonGenericType)
            {
                lua_newtable(state);

                foreach (var constField in nonGenericType.GetPublicStaticFields().Where(f => f.IsLiteral))
                {
                    _environment.PushObject(state, constField.GetValue(null));
                    lua_setfield(state, -2, constField.Name);
                }

                // TODO: pre-populate events

                foreach (var group in nonGenericType.GetPublicStaticMethods().GroupBy(m => m.Name))
                {
                    PushTypeMethodMetavalue(state, group.ToList());
                    lua_setfield(state, -2, group.Key);
                }

                // TODO: pre-populate nested types

                lua_createtable(state, 0, 1);
            }

            var metamethod = GenerateMetamethod("__index", (ilg, context) =>
            {
                var keyType = EmitDeclareKeyType(ilg);

                // If there is a non-generic type, then support indexing non-cacheable members.

                if (maybeNonGenericType is { })
                {
                    var fields = maybeNonGenericType.GetPublicStaticFields().Where(f => !f.IsLiteral);  // No consts
                    var properties = maybeNonGenericType.GetPublicStaticProperties();
                    var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                    context.SetMembers(state, members);

                    EmitIndexMembers(ilg, members,
                        ilg => ilg.Emit(Ldloc, keyType),
                        (ilg, field) =>
                        {
                            ilg.Emit(Ldsfld, field);
                        },
                        (ilg, property) =>
                        {
                            ilg.Emit(Call, property.GetMethod!);
                        });
                }

                // If there are generic types, then support indexing generics.

                if (maybeGenericTypes is { })
                {
                    var arityToType = maybeGenericTypes
                        .Where(t => t.IsGenericTypeDefinition)
                        .ToDictionary(t => t.GetGenericArguments().Length);
                    var minArity = arityToType.Keys.Min();
                    var maxArity = arityToType.Keys.Max();

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

                                ilg.MarkLabels(cases.Where((_, i) => !arityToType.ContainsKey(i + minArity)));

                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldstr, "attempt to construct generic type with invalid arity");
                                ilg.Emit(Call, _luaL_error);
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
                                    ilg.Emit(Call, _typeGetTypeFromHandle);
                                    ilg.Emit(Ldloc, typeArgs);
                                    ilg.Emit(Callvirt, _typeMakeGenericType);
                                    ilg.Emit(Call, MetamethodContext._pushClrType);
                                    ilg.Emit(Leave, exit);  // Not short form
                                }
                            }

                            ilg.BeginCatchBlock(typeof(ArgumentException));
                            {
                                ilg.Emit(Pop);
                                ilg.Emit(Ldarg_1);
                                ilg.Emit(Ldstr, "attempt to construct generic type with invalid constraints");
                                ilg.Emit(Call, _luaL_error);
                                ilg.Emit(Pop);
                            }

                            ilg.EndExceptionBlock();

                            ilg.Emit(Ldc_I4_1);
                            ilg.Emit(Ret);
                        });
                }

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            });
            lua_pushcfunction(state, metamethod);

            if (maybeNonGenericType is { })
            {
                lua_setfield(state, -2, "__index");

                lua_setmetatable(state, -2);
            }
        }

        private void PushTypeNewIndexMetavalue(IntPtr state, Type type)
        {
            var metamethod = GenerateMetamethod("__newindex", (ilg, context) =>
            {
                var keyType = EmitDeclareKeyType(ilg);
                var valueType = EmitDeclareValueType(ilg);

                {
                    var fields = type.GetPublicStaticFields();
                    var properties = type.GetPublicStaticProperties();
                    var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                    context.SetMembers(state, members);

                    EmitNewIndexMembers(ilg, members,
                        ilg => ilg.Emit(Ldloc, keyType),
                        ilg => ilg.Emit(Ldloc, valueType),
                        (ilg, field, temp) =>
                        {
                            ilg.Emit(Ldloc, temp);
                            ilg.Emit(Stsfld, field);
                        },
                        (ilg, property, temp) =>
                        {
                            var propertyType = property.PropertyType;

                            if (propertyType.IsByRef)
                            {
                                ilg.Emit(Call, property.GetMethod!);
                                ilg.Emit(Ldloc, temp);
                                ilg.EmitStind(propertyType.GetElementType()!);
                            }
                            else
                            {
                                ilg.Emit(Ldloc, temp);
                                ilg.Emit(Call, property.SetMethod!);
                            }
                        });
                }

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            });
            lua_pushcfunction(state, metamethod);
        }

        private void PushTypeCallMetavalue(IntPtr state, Type type)
        {
            var metamethod = GenerateMetamethod("__call", (ilg, _) =>
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
            lua_pushcfunction(state, metamethod);
        }

        private void PushTypeMethodMetavalue(IntPtr state, IReadOnlyList<MethodInfo> methods)
        {
            var metamethod = GenerateMetamethod("__call", (ilg, _) =>
            {
                var argCount = EmitDeclareArgCount(ilg);

                EmitCallMethods(ilg, methods,
                    ilg => ilg.Emit(Ldloc, argCount),
                    (ilg, temp) =>
                    {
                        ilg.Emit(Ldarg_1);
                        ilg.Emit(Ldloc, temp);
                        ilg.Emit(Call, _lua_type);
                    },
                    (ilg, temp) => ilg.Emit(Ldloc, temp),
                    (ilg, method, args, temp) =>
                    {
                        foreach (var arg in args)
                        {
                            ilg.Emit(Ldloc, arg);
                        }

                        ilg.Emit(Call, (MethodInfo)method);
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
            lua_pushcfunction(state, metamethod);
        }
    
        private void PushObjectIndexMetavalue(IntPtr state, Type objType)
        {
            lua_newtable(state);

            // TODO: pre-populate events

            foreach (var group in objType.GetPublicInstanceMethods().GroupBy(m => m.Name))
            {
                PushObjectMethodMetavalue(state, objType, group.ToList());
                lua_setfield(state, -2, group.Key);
            }

            lua_createtable(state, 0, 1);

            var metamethod = GenerateMetamethod("__index", (ilg, context) =>
            {
                var target = EmitDeclareTarget(ilg, objType);
                var keyType = EmitDeclareKeyType(ilg);

                {
                    var fields = objType.GetPublicInstanceFields();
                    var properties = objType.GetPublicInstanceProperties();
                    var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                    context.SetMembers(state, members);

                    EmitIndexMembers(ilg, members,
                        ilg => ilg.Emit(Ldloc, keyType),
                        (ilg, field) =>
                        {
                            ilg.Emit(Ldloc, target);
                            ilg.Emit(Ldfld, field);
                        },
                        (ilg, property) =>
                        {
                            ilg.Emit(Ldloc, target);
                            ilg.EmitCall(property.GetMethod!);
                        });
                }

                // TODO: support array indexers
                // TODO: support indexers

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            });
            lua_pushcfunction(state, metamethod);
            lua_setfield(state, -2, "__index");

            lua_setmetatable(state, -2);
        }

        private void PushObjectNewIndexMetavalue(IntPtr state, Type objType)
        {
            var metamethod = GenerateMetamethod("__newindex", (ilg, context) =>
            {
                var target = EmitDeclareTarget(ilg, objType);
                var keyType = EmitDeclareKeyType(ilg);
                var valueType = EmitDeclareValueType(ilg);

                {
                    var fields = objType.GetPublicInstanceFields();
                    var properties = objType.GetPublicInstanceProperties();
                    var members = fields.Cast<MemberInfo>().Concat(properties).ToList();
                    context.SetMembers(state, members);

                    EmitNewIndexMembers(ilg, members,
                        ilg => ilg.Emit(Ldloc, keyType),
                        ilg => ilg.Emit(Ldloc, valueType),
                        (ilg, field, temp) =>
                        {
                            ilg.Emit(Ldloc, target);
                            ilg.Emit(Ldloc, temp);
                            ilg.Emit(Stfld, field);
                        },
                        (ilg, property, temp) =>
                        {
                            var propertyType = property.PropertyType;

                            if (propertyType.IsByRef)
                            {
                                ilg.Emit(Ldloc, target);
                                ilg.EmitCall(property.GetMethod!);
                                ilg.Emit(Ldloc, temp);
                                ilg.EmitStind(propertyType.GetElementType()!);
                            }
                            else
                            {
                                ilg.Emit(Ldloc, target);
                                ilg.Emit(Ldloc, temp);
                                ilg.EmitCall(property.SetMethod!);
                            }
                        });
                }

                ilg.Emit(Ldc_I4_0);
                ilg.Emit(Ret);
            });
            lua_pushcfunction(state, metamethod);
        }

        private void PushObjectMethodMetavalue(IntPtr state, Type objType, IReadOnlyList<MethodInfo> methods)
        {
            var metamethod = GenerateMetamethod("__call", (ilg, _) =>
            {
                var target = EmitDeclareTarget(ilg, objType);
                var argCount = EmitDeclareArgCount(ilg);

                EmitCallMethods(ilg, methods,
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
                    (ilg, method, args, temp) =>
                    {
                        ilg.Emit(Ldloc, target);
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
            lua_pushcfunction(state, metamethod);
        }
    }
}
