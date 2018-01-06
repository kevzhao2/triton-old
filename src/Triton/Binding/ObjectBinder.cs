// Copyright (c) 2018 Kevin Zhao
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
using System.Runtime.InteropServices;
using Triton.Interop;

namespace Triton.Binding {
    /// <summary>
    /// Handles object binding, allowing Lua to access .NET objects and types.
    /// </summary>
    internal sealed class ObjectBinder : IDisposable {
        public const string ObjectMetatable = "$__object";
        public const string TypeMetatable = "$__type";
        
        private readonly GCHandle _luaHandle;

        private static readonly Dictionary<Type, TypeBindingInfo> TypeBindingInfoCache = new Dictionary<Type, TypeBindingInfo>();
        private static readonly object TypeBindingLock = new object();

        private static readonly Dictionary<string, LuaCFunction> ObjectMetamethods = new Dictionary<string, LuaCFunction> {
            ["__add"] = AddObject,
            ["__sub"] = SubObject,
            ["__mul"] = MulObject,
            ["__div"] = DivObject,
            ["__mod"] = ModObject,
            ["__band"] = BandObject,
            ["__bor"] = BorObject,
            ["__bxor"] = BxorObject,
            ["__shr"] = ShrObject,
            ["__shl"] = ShlObject,
            ["__eq"] = EqObject,
            ["__lt"] = LtObject,
            ["__le"] = LeObject,
            ["__unm"] = UnmObject,
            ["__bnot"] = BnotObject,
            ["__call"] = CallObject,
            ["__gc"] = Gc,
            ["__index"] = IndexObject,
            ["__newindex"] = NewIndexObject,
            ["__tostring"] = ToString
        };

        private static readonly Dictionary<string, LuaCFunction> TypeMetamethods = new Dictionary<string, LuaCFunction> {
            ["__call"] = CallType,
            ["__gc"] = Gc,
            ["__index"] = IndexType,
            ["__newindex"] = NewIndexType,
            ["__tostring"] = ToString
        };

        private static readonly LuaCFunction ProxyCallTypeDelegate = ProxyCallType;
        private static readonly LuaCFunction ProxyCallObjectDelegate = ProxyCallObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectBinder"/> class for the given <see cref="Lua"/> instance and its Lua state
        /// pointer.
        /// </summary>
        /// <param name="lua">The <see cref="Lua"/> instance.</param>
        /// <param name="state">The Lua state pointer.</param>
        public ObjectBinder(Lua lua, IntPtr state) {
            // The simplest thing to do would be to callback into an instance method, where we can then access the Lua instance easily.
            // However, some platforms don't support this, so the workaround is to close on a weak GCHandle to the Lua instance.
            _luaHandle = GCHandle.Alloc(lua, GCHandleType.Weak);

            NewMetatable(ObjectMetatable, ObjectMetamethods);
            NewMetatable(TypeMetatable, TypeMetamethods);

            LuaApi.Pop(state, 2);

            void NewMetatable(string name, Dictionary<string, LuaCFunction> metamethods) {
                LuaApi.NewMetatable(state, name);
                
                foreach (var kvp in metamethods) {
                    LuaApi.PushString(state, kvp.Key);
                    LuaApi.PushHandle(state, _luaHandle);
                    LuaApi.PushCClosure(state, kvp.Value, 1);
                    LuaApi.SetTable(state, -3);
                }

                // Setting __metatable to false hides the metatable, protecting it from getmetatable() and setmetatable().
                LuaApi.PushString(state, "__metatable");
                LuaApi.PushBoolean(state, false);
                LuaApi.SetTable(state, -3);
            }
        }

        /// <summary>
        /// Disposes the <see cref="ObjectBinder"/>.
        /// </summary>
        public void Dispose() {
            // This is potentially unsafe. But because Dispose is only called from Lua.Dispose, the handle will always allocated.
            _luaHandle.Free();
        }

        internal static T ResolveMethodCall<T>(object[] objs, IEnumerable<T> methods, out object[] args) where T : MethodBase {
            // To resolve overloads, we will utilize a scoring system to pick the best method out of the given methods.
            var bestScore = int.MinValue;
            T bestMethod = null;

            args = null;
            foreach (var method in methods) {
                var score = TryCoerce(objs, method.GetParameters(), out var methodArgs);
                if (score > bestScore) {
                    bestScore = score;
                    bestMethod = method;
                    args = methodArgs;
                }
            }

            return bestMethod;
        }

        internal static int TryCoerce(object[] objs, ParameterInfo[] @params, out object[] args) {
            args = new object[@params.Length];

            // The objects will be 'scored' on how well they fit into the given parameters. In general, the more positional parameters
            // satisfied, the higher the score, and the more implicit parameters, the lower the score.
            const int explicitBonus = 65536;
            const int implicitBonus = -1;
            var score = 0;

            var objIndex = 0;
            for (var i = 0; i < @params.Length; ++i) {
                var param = @params[i];
                if (param.IsOut) {
                    continue;
                }

                var type = param.ParameterType;
                if (i == @params.Length - 1 && type.IsArray && param.IsDefined(typeof(ParamArrayAttribute), false)) {
                    var elementType = type.GetElementType();
                    var array = Array.CreateInstance(elementType, objs.Length - objIndex);

                    // If we can't form an array out of the remaining objects, it's possible an actual array was passed instead.
                    var createdArray = true;
                    for (var j = objIndex; j < objs.Length; ++j) {
                        if (!objs[j].TryCoerce(elementType, out var value)) {
                            createdArray = false;
                            break;
                        }

                        array.SetValue(value, j - objIndex);
                    }

                    if (createdArray) {
                        args[i] = array;
                        score += implicitBonus;
                        return score;
                    }
                }

                // If we still have more objects to look through, try to coerce them. Otherwise, see if we can use default values.
                if (objIndex < objs.Length) {
                    if (!objs[objIndex].TryCoerce(type, out args[i])) {
                        return int.MinValue;
                    }

                    ++objIndex;
                    score += explicitBonus;
                } else {
                    if (!param.IsOptional) {
                        return int.MinValue;
                    }

                    args[i] = param.DefaultValue;
                    score += implicitBonus;
                }
            }

            // Ensure that all of the objects were used.
            return objIndex == objs.Length ? score : int.MinValue;
        }

        private static TypeBindingInfo GetTypeBindingInfo(Type type) {
            lock (TypeBindingLock) {
                if (!TypeBindingInfoCache.TryGetValue(type, out var info)) {
                    info = TypeBindingInfo.Construct(type);
                    TypeBindingInfoCache[type] = info;
                }
                return info;
            }
        }

        private static int AddObject(IntPtr state) => BinaryOpShared(state, "add", "op_Addition");
        private static int SubObject(IntPtr state) => BinaryOpShared(state, "subtract", "op_Subtraction");
        private static int MulObject(IntPtr state) => BinaryOpShared(state, "multiply", "op_Multiply");
        private static int DivObject(IntPtr state) => BinaryOpShared(state, "divide", "op_Division");
        private static int ModObject(IntPtr state) => BinaryOpShared(state, "modulus", "op_Modulus");
        private static int BandObject(IntPtr state) => BinaryOpShared(state, "bitwise AND", "op_BitwiseAnd");
        private static int BorObject(IntPtr state) => BinaryOpShared(state, "bitwise OR", "op_BitwiseOr");
        private static int BxorObject(IntPtr state) => BinaryOpShared(state, "multiply", "op_ExclusiveOr");
        private static int EqObject(IntPtr state) => BinaryOpShared(state, "compare", "op_Equality");
        private static int LtObject(IntPtr state) => BinaryOpShared(state, "compare", "op_LessThan");
        private static int LeObject(IntPtr state) => BinaryOpShared(state, "compare", "op_LessThanOrEqual");
        private static int ShlObject(IntPtr state) => BinaryOpShared(state, "left shift", "op_LeftShift");
        private static int ShrObject(IntPtr state) => BinaryOpShared(state, "right shift", "op_RightShift");

        private static int BinaryOpShared(IntPtr state, string operation, string methodName) {
            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;

            // We need to use lua.GetObject here since binary operators can occur with one of the operands not being .NET objects.
            var operand1 = lua.GetObject(1, null, state);
            var operand2 = lua.GetObject(2, null, state);
            var info1 = GetTypeBindingInfo(operand1.GetType());
            var info2 = GetTypeBindingInfo(operand2.GetType());

            // Binary operators can be declared on either of the operands' types.
            var ops = info1.GetOperators(methodName);
            if (info2 != info1) {
                ops = ops.Concat(info2.GetOperators(methodName));
            }
            
            var op = ResolveMethodCall(new[] { operand1, operand2 }, ops, out var args);
            if (op == null) {
                throw LuaApi.Error(state, $"attempt to {operation} two objects");
            }

            object result;
            try {
                result = op.Invoke(null, args);
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(state, $"attempt to {operation} threw:\n{e.InnerException}");
            }
            
            // Operators must always have exactly one return value!
            lua.PushObject(result, state);
            return 1;
        }
        
        private static int UnmObject(IntPtr state) => UnaryOpShared(state, "negate", "op_UnaryNegation");
        private static int BnotObject(IntPtr state) => UnaryOpShared(state, "bitwise NOT", "op_OnesComplement");

        private static int UnaryOpShared(IntPtr state, string operation, string methodName) {
            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;
            var operand = LuaApi.ToHandle(state, 1).Target;
            var info = GetTypeBindingInfo(operand.GetType());

            var op = info.GetOperators(methodName).SingleOrDefault();
            if (op == null) {
                throw LuaApi.Error(state, $"attempt to {operation} an object");
            }

            object result;
            try {
                result = op.Invoke(null, new[] { operand });
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(state, $"attempt to {operation} threw:\n{e.InnerException}");
            }
            
            // Operators must always have exactly one return value!
            lua.PushObject(result, state);
            return 1;
        }
        
        private static int CallObject(IntPtr state) {
            var obj = LuaApi.ToHandle(state, 1).Target;
            if (!(obj is Delegate @delegate)) {
                throw LuaApi.Error(state, "attempt to call non-delegate");
            }

            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;
            var top = LuaApi.GetTop(state);
            var objs = lua.GetObjects(2, top, state);

#if NETSTANDARD
            var method = @delegate.GetMethodInfo();
#else
            var method = @delegate.Method;
#endif
            if (TryCoerce(objs, method.GetParameters(), out var args) == int.MinValue) {
                throw LuaApi.Error(state, "attempt to call delegate with invalid args");
            }

            object result;
            try {
                result = method.Invoke(@delegate.Target, args);
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(state, $"attempt to call delegate threw:\n{e.InnerException}");
            }

            // Since delegate calls tend to be expensive and we're transitioning back into Lua, let's process some queued unrefs.
            lua.ProcessQueuedUnrefs();

            var numResults = 0;
            if (method.ReturnType != typeof(void)) {
                ++numResults;
                lua.PushObject(result, state);
            }
            foreach (var param in method.GetParameters().Where(p => p.ParameterType.IsByRef)) {
                ++numResults;
                lua.PushObject(args[param.Position], state);
            }
            return numResults;
        }

        private static int CallType(IntPtr state) {
            var type = (Type)LuaApi.ToHandle(state, 1).Target;
#if NETSTANDARD
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif

            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;
            var top = LuaApi.GetTop(state);
            var objs = lua.GetObjects(2, top, state);

            // If the type contains unresolved generic parameters, like List<> or Dictionary<,>, then we try to construct it using the
            // supplied type arguments. Otherwise, we just try to construct an instance of it.
            object result;
            if (typeInfo.ContainsGenericParameters) {
                var typeArgs = typeInfo.GetGenericArguments();
                if (objs.Length != typeArgs.Length) {
                    throw LuaApi.Error(state, "attempt to construct generic type with incorrect number of type args");
                }

                for (var i = 0; i < typeArgs.Length; ++i) {
                    if (!(objs[i] is Type typeArg)) {
                        throw LuaApi.Error(state, "attempt to construct generic type with non-type arg");
                    }
#if NETSTANDARD
                    if (typeArg.GetTypeInfo().ContainsGenericParameters) {
#else
                    if (typeArg.ContainsGenericParameters) {
#endif
                        throw LuaApi.Error(state, "attempt to construct generic type with generic type arg");
                    }

                    typeArgs[i] = typeArg;
                }

                // Types return a wrapper object to signify that static members may be accessed.
                try {
                    result = new TypeWrapper(type.MakeGenericType(typeArgs));
                } catch (ArgumentException) {
                    throw LuaApi.Error(state, "attempt to construct generic type threw: type constraints");
                }
            } else {
                if (typeInfo.IsAbstract) {
                    throw LuaApi.Error(state, "attempt to instantiate abstract type");
                }

                var info = GetTypeBindingInfo(type);
                var ctors = info.GetConstructors();
                if (ctors.Count == 0) {
                    throw LuaApi.Error(state, "attempt to instantiate type with no constructors");
                }
                
                var ctor = ResolveMethodCall(objs, ctors, out var args);
                if (ctor == null) {
                    throw LuaApi.Error(state, "attempt to instantiate type with invalid args");
                }

                try {
                    result = ctor.Invoke(args);
                } catch (TargetInvocationException e) {
                    throw LuaApi.Error(state, $"attempt to instantiate type threw:\n{e.InnerException}");
                }
            }

            lua.PushObject(result, state);
            return 1;
        }
        
        private static int Gc(IntPtr state) {
            var handle = LuaApi.ToHandle(state, 1);
            handle.Free();
            return 0;
        }
        
        private static int IndexObject(IntPtr state) {
            var obj = LuaApi.ToHandle(state, 1).Target;
            return IndexShared(state, obj, obj.GetType());
        }
        
        private static int IndexType(IntPtr state) {
            var type = (Type)LuaApi.ToHandle(state, 1).Target;
#if NETSTANDARD
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif
            
            if (typeInfo.IsInterface) {
                throw LuaApi.Error(state, "attempt to index interface");
            }
            if (typeInfo.ContainsGenericParameters) {
                throw LuaApi.Error(state, "attempt to index generic type");
            }

            return IndexShared(state, null, type);
        }

        private static int IndexShared(IntPtr state, object obj, Type type) {
            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;

            var keyType = LuaApi.Type(state, 2);
            if (keyType == LuaType.String) {
                var name = LuaApi.ToString(state, 2);
                var info = GetTypeBindingInfo(type);
                var isStatic = obj == null;

                var member = info.GetMember(name, isStatic);
                if (member == null) {
                    throw LuaApi.Error(state, "attempt to index invalid member");
                }

                if (member is MethodInfo method) {
                    // Methods return a function that then handles the overload resolution.
                    LuaApi.PushValue(state, LuaApi.UpvalueIndex(1));
                    LuaApi.PushValue(state, 1);
                    LuaApi.PushValue(state, 2);
                    LuaApi.PushInteger(state, 0);
                    LuaApi.PushCClosure(state, isStatic ? ProxyCallTypeDelegate : ProxyCallObjectDelegate, 4);
                } else if (member is PropertyInfo property) {
                    // Indexed properties return a wrapper object that handles getting/setting.
                    if (property.GetIndexParameters().Length > 0) {
                        lua.PushObject(new IndexedPropertyWrapper(state, obj, property), state);
                    } else {
                        if (property.GetGetMethod() == null) {
                            throw LuaApi.Error(state, "attempt to get property without getter");
                        }

                        try {
                            lua.PushObject(property.GetValue(obj, null), state);
                        } catch (TargetInvocationException e) {
                            throw LuaApi.Error(state, $"attempt to get property threw:\n{e.InnerException}");
                        }
                    }
                } else if (member is FieldInfo field) {
                    lua.PushObject(field.GetValue(obj), state);
                } else if (member is EventInfo @event) {
                    // Events return a wrapper object that handles adding/removing.
                    var eventWrapper = new EventWrapper(state, obj, @event);
                    lua.PushObject(eventWrapper, state);
                } else {
                    // Nested types return a wrapper object to signify that static members may be accessed.
#if NETSTANDARD
                    lua.PushObject(new TypeWrapper(((TypeInfo)member).AsType()), state);
#else
                    lua.PushObject(new TypeWrapper((Type)member), state);
#endif
                }
                return 1;
            }
            
            if (obj is Array array && LuaApi.IsInteger(state, 2)) {
                if (array.Rank != 1) {
                    throw LuaApi.Error(state, "attempt to index multi-dimensional array");
                }

                var index = (int)LuaApi.ToInteger(state, 2);
                if (index < 0 || index >= array.Length) {
                    throw LuaApi.Error(state, "attempt to index array with out-of-bounds index");
                }

                lua.PushObject(array.GetValue(index), state);
                return 1;
            }

            throw LuaApi.Error(state, "attempt to index with invalid key");
        }

        private static int ProxyCallObject(IntPtr state) {
            var obj = LuaApi.ToHandle(state, LuaApi.UpvalueIndex(2)).Target;
            return ProxyCallShared(state, obj, obj.GetType());
        }

        private static int ProxyCallType(IntPtr state) {
            var type = LuaApi.ToHandle(state, LuaApi.UpvalueIndex(2)).Target as Type;
            return ProxyCallShared(state, null, type);
        }

        private static int ProxyCallShared(IntPtr state, object obj, Type type) {
            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;
            var name = LuaApi.ToString(state, LuaApi.UpvalueIndex(3));
            var info = GetTypeBindingInfo(type);
            var isStatic = obj == null;

            var numTypeArgs = LuaApi.ToInteger(state, LuaApi.UpvalueIndex(4));
            var top = LuaApi.GetTop(state);

            // The arguments will start at 2 only if the call is an instance call and it's not a generic call. This is because the
            // obj:Method syntax will automatically pass obj as the first argument. A generic obj:Method call will not have obj as the
            // first argument because only the first "invocation" with the types will have obj as the first argument.
            var objs = lua.GetObjects(isStatic || numTypeArgs > 0 ? 1 : 2, top, state);
            
            var methods = info.GetMethods(name, isStatic, (int)numTypeArgs);
            if (numTypeArgs > 0) {
                var typeArgs = new Type[numTypeArgs];
                for (var i = 0; i < typeArgs.Length; ++i) {
                    if (!(lua.GetObject(LuaApi.UpvalueIndex(5 + i)) is Type typeArg)) {
                        throw LuaApi.Error(state, "attempt to construct generic method with non-type arg");
                    }
#if NETSTANDARD
                    if (typeArg.GetTypeInfo().ContainsGenericParameters) {
#else
                    if (typeArg.ContainsGenericParameters) {
#endif
                        throw LuaApi.Error(state, "attempt to construct generic method with generic type arg");
                    }

                    typeArgs[i] = typeArg;
                }

                // "Resolve" all generic methods into non-generic methods, ignoring any type constraint issues.
                var realMethods = new List<MethodInfo>();
                foreach (var genericMethod in methods) {
                    try {
                        realMethods.Add(genericMethod.MakeGenericMethod(typeArgs));
                    } catch (ArgumentException) {
                    }
                }
                if (realMethods.Count == 0) {
                    throw LuaApi.Error(state, "attempt to construct generic method threw: type constraints");
                }
                methods = realMethods;
            }
            
            var method = ResolveMethodCall(objs, methods, out var args);
            if (method == null) {
                if (numTypeArgs > 0) {
                    throw LuaApi.Error(state, "attempt to call generic method with invalid args");
                }

                // If the first attempt at resolving the method failed and there is at least one argument, then we're possibly dealing
                // with a generic method call, where the arguments are type. We must return a function which handles the generic overload
                // resolution!
                var genericMethods = info.GetMethods(name, isStatic, objs.Length);
                if (objs.Length == 0 || !genericMethods.Any()) {
                    throw LuaApi.Error(state, "attempt to call method with invalid args");
                }
                
                LuaApi.PushValue(state, LuaApi.UpvalueIndex(1));
                LuaApi.PushValue(state, LuaApi.UpvalueIndex(2));
                LuaApi.PushValue(state, LuaApi.UpvalueIndex(3));
                LuaApi.PushInteger(state, objs.Length);
                for (var i = isStatic ? 1 : 2; i <= top; ++i) {
                    LuaApi.PushValue(state, i);
                }
                LuaApi.PushCClosure(state, isStatic ? ProxyCallTypeDelegate : ProxyCallObjectDelegate, isStatic ? top + 4 : top + 3);
                return 1;
            }

            object result;
            try {
                result = method.Invoke(obj, args);
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(state, $"attempt to call {(numTypeArgs > 0 ? "generic " : "")}method threw:\n{e.InnerException}");
            }

            // Since method calls tend to be expensive and we're transitioning back into Lua, let's process some queued unrefs.
            lua.ProcessQueuedUnrefs();

            var numResults = 0;
            if (method.ReturnType != typeof(void)) {
                ++numResults;
                lua.PushObject(result, state);
            }
            foreach (var param in method.GetParameters().Where(p => p.ParameterType.IsByRef)) {
                ++numResults;
                lua.PushObject(args[param.Position], state);
            }
            return numResults;
        }
        
        private static int NewIndexObject(IntPtr state) {
            var obj = LuaApi.ToHandle(state, 1).Target;
            return NewIndexShared(state, obj, obj.GetType());
        }
        
        private static int NewIndexType(IntPtr state) {
            var type = (Type)LuaApi.ToHandle(state, 1).Target;
#if NETSTANDARD
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif

            if (typeInfo.IsInterface) {
                throw LuaApi.Error(state, "attempt to index interface");
            }
            if (typeInfo.ContainsGenericParameters) {
                throw LuaApi.Error(state, "attempt to index generic type");
            }

            return NewIndexShared(state, null, type);
        }

        private static int NewIndexShared(IntPtr state, object obj, Type type) {
            var lua = (Lua)LuaApi.ToHandle(state, LuaApi.UpvalueIndex(1)).Target;
            var value = lua.GetObject(3, null, state);
            
            var keyType = LuaApi.Type(state, 2);
            if (keyType == LuaType.String) {
                var name = LuaApi.ToString(state, 2);
                var info = GetTypeBindingInfo(type);
                var isStatic = obj == null;

                var member = info.GetMember(name, isStatic);
                if (member == null) {
                    throw LuaApi.Error(state, "attempt to set invalid member");
                }

                if (member is PropertyInfo property) {
                    if (property.GetSetMethod() == null) {
                        throw LuaApi.Error(state, "attempt to set property without setter");
                    }
                    if (property.GetIndexParameters().Length > 0) {
                        throw LuaApi.Error(state, "attempt to set indexed property");
                    }
                    if (!value.TryCoerce(property.PropertyType, out value)) {
                        throw LuaApi.Error(state, "attempt to set property with invalid value");
                    }

                    try {
                        property.SetValue(obj, value, null);
                    } catch (TargetInvocationException e) {
                        throw LuaApi.Error(state, $"attempt to set property threw:\n{e.InnerException}");
                    }
                } else if (member is FieldInfo field) {
                    if (field.IsLiteral) {
                        throw LuaApi.Error(state, "attempt to set constant field");
                    }
                    if (!value.TryCoerce(field.FieldType, out value)) {
                        throw LuaApi.Error(state, "attempt to set field with invalid value");
                    }

                    field.SetValue(obj, value);
                } else if (member is EventInfo @event) {
                    throw LuaApi.Error(state, "attempt to set event");
                } else if (member is MethodInfo method) {
                    throw LuaApi.Error(state, "attempt to set method");
                } else {
                    throw LuaApi.Error(state, "attempt to set nested type");
                }
                return 0;
            }

            if (obj is Array array && LuaApi.IsInteger(state, 2)) {
                if (array.Rank != 1) {
                    throw LuaApi.Error(state, "attempt to index multi-dimensional array");
                }

                var index = (int)LuaApi.ToInteger(state, 2);
                if (index < 0 || index >= array.Length) {
                    throw LuaApi.Error(state, "attempt to index array with out-of-bounds index");
                }
                if (!value.TryCoerce(type.GetElementType(), out value)) {
                    throw LuaApi.Error(state, "attempt to set array with invalid value");
                }

                array.SetValue(value, index);
                return 0;
            }

            throw LuaApi.Error(state, "attempt to index with invalid key");
        }
        
        private static int ToString(IntPtr state) {
            var obj = LuaApi.ToHandle(state, 1).Target;
            LuaApi.PushString(state, obj.ToString());
            return 1;
        }
    }
}
