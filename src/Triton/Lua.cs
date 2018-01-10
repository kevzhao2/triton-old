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
using System.Runtime.InteropServices;
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
        private readonly Dictionary<IntPtr, WeakReference> _cachedLuaReferences = new Dictionary<IntPtr, WeakReference>();
        private readonly GCHandle _handle;
        private readonly Dictionary<IntPtr, int> _pointerToReferenceId = new Dictionary<IntPtr, int>();

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lua"/> class.
        /// </summary>
        public Lua() {
            State = LuaApi.NewState();
            LuaApi.OpenLibs(State);

            _handle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
            ObjectBinder.InitializeMetatables(this, _handle);
            this["using"] = new Action<string>(ImportNamespace);
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
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public object this[string name] {
            get {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                ThrowIfDisposed();

                return GetGlobalInternal(name);
            }
            set {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                ThrowIfDisposed();

                SetGlobalInternal(name, value);
            }
        }

        internal IntPtr State { get; }

        /// <summary>
        /// Creates a <see cref="LuaTable"/>.
        /// </summary>
        /// <returns>The resulting <see cref="LuaTable"/>.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaTable CreateTable() {
            ThrowIfDisposed();

            LuaApi.CreateTable(State);
            var pointer = LuaApi.ToPointer(State, -1);
            var referenceId = LuaApi.Ref(State, LuaApi.RegistryIndex);
            var table = new LuaTable(this, referenceId);
            _cachedLuaReferences[pointer] = new WeakReference(table);
            return table;
        }

        /// <summary>
        /// Creates a <see cref="LuaThread"/> that will run the given <see cref="LuaFunction"/>.
        /// </summary>
        /// <param name="function">The <see cref="LuaFunction"/>.</param>
        /// <returns>The resulting <see cref="LuaThread"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaThread CreateThread(LuaFunction function) {
            if (function == null) {
                throw new ArgumentNullException(nameof(function));
            }
            ThrowIfDisposed();

            var threadState = LuaApi.NewThread(State);
            function.PushOnto(threadState);
            var referenceId = LuaApi.Ref(State, LuaApi.RegistryIndex);
            var thread = new LuaThread(this, referenceId, threadState);
            _cachedLuaReferences[threadState] = new WeakReference(thread);
            return thread;
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

            LoadStringInternal(s);
            return Call(new object[0]);
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
            var types = assemblies.SelectMany(a => a.GetTypes());
            foreach (var type in types.Where(t => t.IsPublic && t.Namespace == @namespace)) {
                ImportTypeInternal(type);
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

            ImportTypeInternal(type);
        }

        /// <summary>
        /// Loads the given string as a Lua chunk.
        /// </summary>
        /// <returns>The resulting <see cref="LuaFunction"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> environment is disposed.</exception>
        public LuaFunction LoadString(string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            ThrowIfDisposed();

            LoadStringInternal(s);
            var pointer = LuaApi.ToPointer(State, -1);
            var referenceId = LuaApi.Ref(State, LuaApi.RegistryIndex);
            var function = new LuaFunction(this, referenceId);
            _cachedLuaReferences[pointer] = new WeakReference(function);
            return function;
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            ThrowIfDisposed();

            result = GetGlobalInternal(binder.Name);
            return true;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            ThrowIfDisposed();

            SetGlobalInternal(binder.Name, value);
            return true;
        }
#endif

        internal void PushObject(object obj, IntPtr? stateOverride = null) {
            var state = stateOverride ?? State;
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

            // UInt64 is a special case since we want to avoid OverflowExceptions.
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

            if (obj is IntPtr pointer) {
                LuaApi.PushLightUserdata(state, pointer);
            } else if (obj is LuaReference lr) {
                lr.PushOnto(state);
            } else {
                ObjectBinder.PushNetObject(state, obj);
            }
        }

        internal object ToObject(int index, LuaType? typeHint = null, IntPtr? stateOverride = null) {
            var state = stateOverride ?? State;
            var type = typeHint ?? LuaApi.Type(state, index);
            Debug.Assert(type == LuaApi.Type(state, index), "Type hint did not match type.");

            switch (type) {
            case LuaType.None:
            case LuaType.Nil:
                return null;

            case LuaType.Boolean:
                return LuaApi.ToBoolean(state, index);

            case LuaType.LightUserdata:
                return LuaApi.ToUserdata(state, index);

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
            return luaReference;
        }

        internal object[] ToObjects(int startIndex, int endIndex, IntPtr? stateOverride = null) {
            if (startIndex > endIndex) {
                return new object[0];
            }

            var objs = new object[endIndex - startIndex + 1];
            for (var i = 0; i < objs.Length; ++i) {
                objs[i] = ToObject(startIndex + i, null, stateOverride);
            }
            return objs;
        }

        internal object[] Call(object[] args, IntPtr? stateOverride = null, bool isResuming = false) {
            var state = stateOverride ?? State;
            if (!isResuming) {
                Debug.Assert(LuaApi.Type(state, -1) == LuaType.Function, "Stack doesn't have function on top.");
            }

            var numArgs = args.Length;
            if (!LuaApi.CheckStack(state, numArgs)) {
                throw new LuaException("Not enough stack space for arguments.");
            }

            var oldTop = isResuming ? 0 : LuaApi.GetTop(state) - 1;
            try {
                foreach (var arg in args) {
                    PushObject(arg, state);
                }

                // Because calls tend to take a long time, let's clean references now.
                CleanReferences();
                var status = isResuming ? LuaApi.Resume(state, State, numArgs) : LuaApi.PCallK(state, numArgs);
                if (status != LuaStatus.Ok && status != LuaStatus.Yield) {
                    var errorMessage = LuaApi.ToString(state, -1);
                    throw new LuaException(errorMessage);
                }
                if (!LuaApi.CheckStack(state, 1)) {
                    throw new LuaException("Not enough scratch stack space.");
                }

                var top = LuaApi.GetTop(state);
                return ToObjects(oldTop + 1, top, state);
            } finally {
                LuaApi.SetTop(state, oldTop);
            }
        }

        internal void CleanReferences() {
            var deadPointers = _cachedLuaReferences.Where(kvp => !kvp.Value.IsAlive).Select(kvp => kvp.Key).ToList();
            foreach (var pointer in deadPointers) {
                var referenceId = _pointerToReferenceId[pointer];
                _cachedLuaReferences.Remove(pointer);
                LuaApi.Unref(State, LuaApi.RegistryIndex, referenceId);
                _pointerToReferenceId.Remove(pointer);
            }
        }

        private object GetGlobalInternal(string name) {
            var type = LuaApi.GetGlobal(State, name);
            var result = ToObject(-1, type);
            LuaApi.Pop(State, 1);
            return result;
        }

        private void SetGlobalInternal(string name, object value) {
            PushObject(value);
            LuaApi.SetGlobal(State, name);
        }

        private void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _handle.Free();
                GC.SuppressFinalize(this);
            }

            LuaApi.Close(State);
            _isDisposed = true;
        }

        private void ImportTypeInternal(Type type) {
            ObjectBinder.PushNetObject(State, new TypeWrapper(type));
            var cleanName = type.Name.Split('`')[0];
            LuaApi.SetGlobal(State, cleanName);
        }

        private void LoadStringInternal(string s) {
            if (LuaApi.LoadString(State, s) != LuaStatus.Ok) {
                var errorMessage = LuaApi.ToString(State, -1);
                LuaApi.Pop(State, 1);
                throw new LuaException(errorMessage);
            }
        }

        private void ThrowIfDisposed() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
