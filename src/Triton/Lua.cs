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
using System.Diagnostics;
#if NETSTANDARD || NET40
using System.Dynamic;
#endif
using System.Linq;
using System.Reflection;
using Triton.Binding;
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Acts as a managed wrapper around a Lua environment.
    /// </summary>
#if NETSTANDARD || NET40
    public sealed class Lua : DynamicObject, IDisposable {
#else
	public sealed class Lua : IDisposable {
#endif
        private const int CleanReferencesPeriod = 1000;
        private const string TempGlobal = "Triton$__temp";
        
        private static readonly object[] EmptyObjectArray = new object[0];

        private readonly ObjectBinder _binder;
        private readonly Dictionary<IntPtr, WeakReference> _cachedLuaReferences = new Dictionary<IntPtr, WeakReference>();
        private readonly LuaHook _cleanReferencesDelegate;
        private readonly Dictionary<IntPtr, int> _pointerToReferenceId = new Dictionary<IntPtr, int>();

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lua"/> class.
        /// </summary>
        public Lua() {
            MainState = LuaApi.NewState();
            LuaApi.OpenLibs(MainState);

            // To clean references, we set a hook that executes after a certain number of instructions have been executed.
            _cleanReferencesDelegate = CleanReferences;
            LuaApi.SetHook(MainState, _cleanReferencesDelegate, LuaHookMask.Count, CleanReferencesPeriod);

            _binder = new ObjectBinder(this);
            this["using"] = CreateFunction(new Action<string>(ImportNamespace));
        }

        /// <summary>
        /// Finalizes the <see cref="Lua"/> environment.
        /// </summary>
        ~Lua() => Dispose(false);

        /// <summary>
        /// Gets or sets the global with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The value of the global.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is a <see cref="LuaReference"/> which is tied to a different <see cref="Lua"/> environment.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public object this[string name] {
            get {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                ThrowIfDisposed();

                var type = LuaApi.GetGlobal(MainState, name);
                var result = ToObject(-1, type);
                LuaApi.Pop(MainState, 1);
                return result;
            }
            set {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                ThrowIfDisposed();

                PushObject(value);
                LuaApi.SetGlobal(MainState, name);
            }
        }
        
        internal IntPtr MainState { get; }

        /// <summary>
        /// Creates a <see cref="LuaFunction"/> from the given delegate.
        /// </summary>
        /// <param name="delegate">The delegate.</param>
        /// <returns>The resulting <see cref="LuaFunction"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="delegate"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaFunction CreateFunction(Delegate @delegate) {
            if (@delegate == null) {
                throw new ArgumentNullException(nameof(@delegate));
            }
            ThrowIfDisposed();

            this[TempGlobal] = @delegate;
            var result = (LuaFunction)DoString($@"
                local temp = _G['{TempGlobal}']
                return function(...)
                    return temp(...)
                end")[0];
            this[TempGlobal] = null;
            return result;
        }

        /// <summary>
        /// Creates a <see cref="LuaFunction"/> from the given string as a Lua chunk.
        /// </summary>
        /// <returns>The resulting <see cref="LuaFunction"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaFunction CreateFunction(string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            ThrowIfDisposed();

            if (LuaApi.LoadString(MainState, s) != LuaStatus.Ok) {
                var errorMessage = LuaApi.ToString(MainState, -1);
                LuaApi.Pop(MainState, 1);
                throw new LuaException(errorMessage);
            }

            var result = (LuaFunction)ToObject(-1, LuaType.Function);
            LuaApi.Pop(MainState, 1);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="LuaTable"/>.
        /// </summary>
        /// <returns>The resulting <see cref="LuaTable"/>.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaTable CreateTable() {
            ThrowIfDisposed();

            LuaApi.CreateTable(MainState);
            var result = (LuaTable)ToObject(-1, LuaType.Table);
            LuaApi.Pop(MainState, 1);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="LuaThread"/> that will run the given <see cref="LuaFunction"/>.
        /// </summary>
        /// <param name="function">The <see cref="LuaFunction"/>.</param>
        /// <returns>The resulting <see cref="LuaThread"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="function"/> is tied to a different <see cref="Lua"/> environment.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaThread CreateThread(LuaFunction function) {
            if (function == null) {
                throw new ArgumentNullException(nameof(function));
            }
            ThrowIfDisposed();

            var threadState = LuaApi.NewThread(MainState);
            function.PushOnto(threadState);
            var result = (LuaThread)ToObject(-1, LuaType.Thread);
            LuaApi.Pop(MainState, 1);
            return result;
        }

        /// <summary>
        /// Disposes the <see cref="Lua"/> enviroment.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Executes the given string as a Lua chunk.
        /// </summary>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public object[] DoString(string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            ThrowIfDisposed();

            if (LuaApi.LoadString(MainState, s) != LuaStatus.Ok) {
                var errorMessage = LuaApi.ToString(MainState, -1);
                LuaApi.Pop(MainState, 1);
                throw new LuaException(errorMessage);
            }

            return Call(EmptyObjectArray);
        }

        /// <summary>
        /// Imports all of the types in the given namespace as globals, allowing Lua to access their static members.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <exception cref="ArgumentNullException"><paramref name="namespace"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public void ImportNamespace(string @namespace) {
            if (@namespace == null) {
                throw new ArgumentNullException(nameof(@namespace));
            }
            ThrowIfDisposed();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(GetTypes);
            foreach (var type in types.Where(t => t.IsPublic && t.Namespace == @namespace)) {
                ImportType(type);
            }

            IEnumerable<Type> GetTypes(Assembly assembly) {
                try {
                    return assembly.GetTypes();
                } catch (ReflectionTypeLoadException e) {
                    return e.Types.Where(t => t != null);
                }
            }
        }

        /// <summary>
        /// Imports the given type as a global, allowing Lua to access its static members.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public void ImportType(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            ThrowIfDisposed();

            ObjectBinder.PushNetObject(MainState, new TypeWrapper(type));
            var cleanName = type.Name.Split('`')[0];
            LuaApi.SetGlobal(MainState, cleanName);
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = this[binder.Name];
            return true;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">
        /// The supplied value is a <see cref="LuaReference"/> which is tied to a different <see cref="Lua"/> environment.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            this[binder.Name] = value;
            return true;
        }
#endif

        internal void PushObject(object obj, IntPtr? stateOverride = null) {
            var state = stateOverride ?? MainState;
            Debug.Assert(LuaApi.GetMainState(state) == MainState, "State override did not match main state.");

            if (obj == null) {
                LuaApi.PushNil(state);
                return;
            }

            var typeCode = Convert.GetTypeCode(obj);
            switch (typeCode) {
            case TypeCode.Boolean:
                LuaApi.PushBoolean(state, (bool)obj);
                return;

            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
                LuaApi.PushInteger(state, Convert.ToInt64(obj));
                return;

            case TypeCode.UInt64:
                LuaApi.PushInteger(state, (long)((ulong)obj));
                return;

            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                LuaApi.PushNumber(state, Convert.ToDouble(obj));
                return;

            case TypeCode.Char:
            case TypeCode.String:
                LuaApi.PushString(state, obj.ToString());
                return;
            }

            if (obj is LuaReference lr) {
                lr.PushOnto(state);
            } else {
                ObjectBinder.PushNetObject(state, obj);
            }
        }

        internal object ToObject(int index, LuaType? typeHint = null, IntPtr? stateOverride = null) {
            var state = stateOverride ?? MainState;
            Debug.Assert(LuaApi.GetMainState(state) == MainState, "State override did not match main state.");
            var type = typeHint ?? LuaApi.Type(state, index);
            Debug.Assert(type == LuaApi.Type(state, index), "Type hint did not match type.");

            switch (type) {
            case LuaType.None:
            case LuaType.Nil:
                return null;

            case LuaType.Boolean:
                return LuaApi.ToBoolean(state, index);

            case LuaType.Number:
                var isInteger = LuaApi.IsInteger(state, index);
                return isInteger ? LuaApi.ToInteger(state, index) : (object)LuaApi.ToNumber(state, index);

            case LuaType.String:
                return LuaApi.ToString(state, index);

            case LuaType.Userdata:
                var handle = LuaApi.ToHandle(state, index);
                return handle.Target;
            }

            LuaReference luaReference = null;
            var pointer = LuaApi.ToPointer(state, index);
            if (_cachedLuaReferences.TryGetValue(pointer, out var weakReference)) {
                luaReference = (LuaReference)weakReference.Target;
                if (luaReference != null) {
                    return luaReference;
                }
            }

            LuaApi.PushValue(state, index);
            var referenceId = LuaApi.Ref(state, LuaApi.RegistryIndex);

            switch (type) {
            case LuaType.Table:
                luaReference = new LuaTable(this, referenceId);
                break;

            case LuaType.Function:
                luaReference = new LuaFunction(this, referenceId);
                break;

            case LuaType.Thread:
                luaReference = new LuaThread(this, referenceId, pointer);
                break;
            }

            if (weakReference != null) {
                weakReference.Target = luaReference;
            } else {
                _cachedLuaReferences[pointer] = new WeakReference(luaReference);
            }
            _pointerToReferenceId[pointer] = referenceId;
            return luaReference;
        }

        internal object[] ToObjects(int startIndex, int endIndex, IntPtr? stateOverride = null) {
            var objs = new object[endIndex - startIndex + 1];
            for (var i = 0; i < objs.Length; ++i) {
                objs[i] = ToObject(startIndex + i, null, stateOverride);
            }
            return objs;
        }

        internal object[] Call(object[] args, IntPtr? stateOverride = null, bool isResuming = false) {
            var state = stateOverride ?? MainState;
            Debug.Assert(LuaApi.GetMainState(state) == MainState, "State override did not match main state.");
            Debug.Assert(isResuming || LuaApi.Type(state, -1) == LuaType.Function, "Stack doesn't have function on top.");

            var oldTop = isResuming ? 0 : LuaApi.GetTop(state) - 1;
            var numArgs = args.Length;
            if (oldTop + numArgs > LuaApi.MinStackSize && !LuaApi.CheckStack(state, numArgs)) {
                throw new LuaException("Not enough stack space for arguments.");
            }

            foreach (var arg in args) {
                PushObject(arg, state);
            }
            
            var status = isResuming ? LuaApi.Resume(state, MainState, numArgs) : LuaApi.PCallK(state, numArgs);
            if (status != LuaStatus.Ok && status != LuaStatus.Yield) {
                var errorMessage = LuaApi.ToString(state, -1);
                LuaApi.Pop(state, 1);
                throw new LuaException(errorMessage);
            }

            // This is a fast path for functions returning nothing since we avoid a SetTop call.
            var top = LuaApi.GetTop(state);
            if (top == oldTop) {
                return EmptyObjectArray;
            }

            if (top + 1 > LuaApi.MinStackSize && !LuaApi.CheckStack(state, 1)) {
                throw new LuaException("Not enough scratch stack space.");
            }

            var results = ToObjects(oldTop + 1, top, state);
            LuaApi.SetTop(state, oldTop);
            return results;
        }

        private void CleanReferences(IntPtr state, IntPtr debug) {
            var deadPointers = new List<IntPtr>();
            foreach (var kvp in _cachedLuaReferences) {
                if (!kvp.Value.IsAlive) {
                    deadPointers.Add(kvp.Key);
                }
            }

            foreach (var deadPointer in deadPointers) {
                _cachedLuaReferences.Remove(deadPointer);
                var referenceId = _pointerToReferenceId[deadPointer];
                LuaApi.Unref(state, LuaApi.RegistryIndex, referenceId);
                _pointerToReferenceId.Remove(deadPointer);
            }
        }

        private void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                GC.SuppressFinalize(this);
            }
            
            LuaApi.Close(MainState);
            _isDisposed = true;
        }
        
        private void ThrowIfDisposed() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
