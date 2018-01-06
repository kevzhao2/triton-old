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
using Triton.Binding;
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Acts as a managed wrapper around a Lua instance.
    /// </summary>
    public sealed class Lua : IDisposable {
        private readonly ObjectBinder _binder;
        private readonly Dictionary<IntPtr, WeakReference> _references = new Dictionary<IntPtr, WeakReference>();
        private readonly IntPtr _state;
        private readonly object _unrefLock = new object();

        private List<KeyValuePair<int, IntPtr>> _unrefs = new List<KeyValuePair<int, IntPtr>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Lua"/> class.
        /// </summary>
        public Lua() {
            _state = LuaApi.NewState();
            LuaApi.OpenLibs(_state);

            _binder = new ObjectBinder(this, _state);
            this["import"] = new Action<string>(ImportType);
        }

        /// <summary>
        /// Finalizes the <see cref="Lua"/> instance.
        /// </summary>
        ~Lua() => Dispose(false);
        
        /// <summary>
        /// Gets a value indicating whether the <see cref="Lua"/> instance is disposed.
        /// </summary>
        /// <value>A value indicating whether the <see cref="Lua"/> instance is disposed.</value>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets or sets the global with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The global.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public object this[string name] {
            get {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                if (IsDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                try {
                    var type = LuaApi.GetGlobal(_state, name);
                    return GetObject(-1, type);
                } finally {
                    LuaApi.SetTop(_state, 0);
                }
            }
            set {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                if (IsDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                PushObject(value);
                LuaApi.SetGlobal(_state, name);

                Debug.Assert(LuaApi.GetTop(_state) == 0, "Stack not level.");
            }
        }

        /// <summary>
        /// Creates a <see cref="LuaTable"/>.
        /// </summary>
        /// <returns>The <see cref="LuaTable"/>.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public LuaTable CreateTable() {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            try {
                LuaApi.CreateTable(_state);
                return (LuaTable)GetObject(-1, LuaType.Table);
            } finally {
                LuaApi.SetTop(_state, 0);
            }
        }

        /// <summary>
        /// Disposes the <see cref="Lua"/> instance.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Loads and runs the given string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public object[] DoString(string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            LoadStringInternal(s);
            return Call(new object[0]);
        }

        /// <summary>
        /// Imports the given type as a global.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        public void ImportType(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            ImportTypeInternal(type);
        }

        /// <summary>
        /// Imports the given type name as a global.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <exception cref="ArgumentException"><paramref name="typeName"/> does not correspond to a valid type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="typeName"/> is <c>null</c>.</exception>
        public void ImportType(string typeName) {
            if (typeName == null) {
                throw new ArgumentNullException(nameof(typeName));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var type = Type.GetType(typeName);
            if (type == null) {
                throw new ArgumentException("Invalid type.", nameof(typeName));
            }

            ImportTypeInternal(type);
        }

        /// <summary>
        /// Loads the given string as a <see cref="LuaFunction"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="LuaFunction"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public LuaFunction LoadString(string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            try {
                LoadStringInternal(s);
                return (LuaFunction)GetObject(-1, LuaType.Function);
            } finally {
                LuaApi.SetTop(_state, 0);
            }
        }
        
        internal object[] Call(object[] args) {
            Debug.Assert(!IsDisposed, "Lua instance is disposed.");
            Debug.Assert(args != null, $"{nameof(args)} is null.");
            Debug.Assert(LuaApi.GetTop(_state) == 1, "Stack not correct.");
            Debug.Assert(LuaApi.Type(_state, -1) == LuaType.Function, "Stack doesn't have function on top.");

            // Ensure that we have enough stack space for the function currently on the stack and its arguments.
            if (args.Length + 1 >= LuaApi.MinStackSize && !LuaApi.CheckStack(_state, args.Length)) {
                throw new LuaException("Not enough stack space for function and arguments.");
            }

            try {
                foreach (var arg in args) {
                    PushObject(arg);
                }

                // Since we're transitioning into Lua, let's try processing some unrefs.
                ProcessQueuedUnrefs();
                if (LuaApi.PCallK(_state, args.Length) != LuaStatus.Ok) {
                    var errorMessage = LuaApi.ToString(_state, -1);
                    throw new LuaException(errorMessage);
                }

                // Ensure that we have enough stack space for GetObjects.
                var numResults = LuaApi.GetTop(_state);
                if (numResults >= LuaApi.MinStackSize && !LuaApi.CheckStack(_state, 1)) {
                    throw new LuaException("Not enough scratch stack space.");
                }

                return GetObjects(1, numResults);
            } finally {
                LuaApi.SetTop(_state, 0);
            }
        }
        
        internal object GetObject(int index, LuaType? typeHint = null) {
            Debug.Assert(!IsDisposed, "Lua instance is disposed.");

            // Using a type hint allows us to save P/Invoke call. This might occur when getting a table value, or when getting a global.
            var type = typeHint ?? LuaApi.Type(_state, index);
            Debug.Assert(type == LuaApi.Type(_state, index), "Type hint did not match type.");

            switch (type) {
            case LuaType.None:
            case LuaType.Nil:
                return null;

            case LuaType.Boolean:
                return LuaApi.ToBoolean(_state, index);

            case LuaType.LightUserdata:
                return LuaApi.ToUserdata(_state, index);

            case LuaType.Number:
                var isInteger = LuaApi.IsInteger(_state, index);
                return isInteger ? LuaApi.ToInteger(_state, index) : (object)LuaApi.ToNumber(_state, index);

            case LuaType.String:
                return LuaApi.ToString(_state, index);

            case LuaType.Userdata:
                return _binder.GetNetObject(index);
            }

            // Try to get a cached LuaReference using the pointer. By returning the same LuaReferences for the same pointers, we create
            // as few finalizable objects as possible and can also easily compare LuaReferences.
            LuaReference luaReference = null;
            var pointer = LuaApi.ToPointer(_state, index);
            if (_references.TryGetValue(pointer, out var weakReference)) {
                luaReference = (LuaReference)weakReference.Target;
                if (luaReference != null) {
                    return luaReference;
                }
            }

            LuaApi.PushValue(_state, index);
            var reference = LuaApi.Ref(_state, LuaApi.RegistryIndex);

            switch (type) {
            case LuaType.Table:
                luaReference = new LuaTable(this, _state, reference, pointer);
                break;

            case LuaType.Function:
                luaReference = new LuaFunction(this, _state, reference, pointer);
                break;

            case LuaType.Thread:
                luaReference = new LuaThread(this, _state, reference, pointer);
                break;
            }

            // If we have a WeakReference object, then we can just reuse it instead of creating a new one.
            if (weakReference != null) {
                weakReference.Target = luaReference;
            } else {
                _references[pointer] = new WeakReference(luaReference);
            }
            return luaReference;
        }

        internal object[] GetObjects(int startIndex, int endIndex) {
            Debug.Assert(!IsDisposed, "Lua instance is disposed.");

            if (startIndex > endIndex) {
                return new object[0];
            }

            var objs = new object[endIndex - startIndex + 1];
            for (var i = 0; i < objs.Length; ++i) {
                objs[i] = GetObject(startIndex + i);
            }
            return objs;
        }

        internal void PushObject(object obj) {
            Debug.Assert(!IsDisposed, "Lua instance is disposed.");

            if (obj == null) {
                LuaApi.PushNil(_state);
                return;
            }
            
            var typeCode = Convert.GetTypeCode(obj);
            switch (typeCode) {
            case TypeCode.Boolean:
                LuaApi.PushBoolean(_state, (bool)obj);
                return;

            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
                LuaApi.PushInteger(_state, Convert.ToInt64(obj));
                return;

            case TypeCode.UInt64:
                // UInt64 is a special case since we want to avoid OverflowExceptions.
                LuaApi.PushInteger(_state, (long)((ulong)obj));
                return;

            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                LuaApi.PushNumber(_state, Convert.ToDouble(obj));
                return;

            case TypeCode.Char:
            case TypeCode.String:
                LuaApi.PushString(_state, obj.ToString());
                return;
            }

            if (obj is IntPtr pointer) {
                LuaApi.PushLightUserdata(_state, pointer);
            } else if (obj is LuaReference lr) {
                lr.PushSelf();
            } else {
                _binder.PushNetObject(obj);
            }
        }

        internal void Unref(int reference, IntPtr pointer, bool disposing) {
            if (IsDisposed) {
                return;
            }

            if (disposing) {
                LuaApi.Unref(_state, LuaApi.RegistryIndex, reference);
                _references.Remove(pointer);
            } else {
                // Since this is called from the finalizer, we have to queue up an unref in a thread-safe manner since Lua is not
                // thread-safe. We don't need to check for uniqueness; calling Unref on the same reference multiple times is completely
                // fine.
                var kvp = new KeyValuePair<int, IntPtr>(reference, pointer);
                lock (_unrefLock) {
                    _unrefs.Add(kvp);
                }
            }
        }

        internal void ProcessQueuedUnrefs() {
            List<KeyValuePair<int, IntPtr>> kvps;
            lock (_unrefLock) {
                kvps = _unrefs;
                _unrefs = new List<KeyValuePair<int, IntPtr>>();
            }

            foreach (var kvp in kvps) {
                var reference = kvp.Key;
                var pointer = kvp.Value;
                LuaApi.Unref(_state, LuaApi.RegistryIndex, reference);
                _references.Remove(pointer);
            }
        }

        private void Dispose(bool disposing) {
            if (IsDisposed) {
                return;
            }

            if (disposing) {
                _binder.Dispose();
                GC.SuppressFinalize(this);
            }

            LuaApi.Close(_state);
            IsDisposed = true;
        }

        private void ImportTypeInternal(Type type) {
            _binder.PushNetObject(new TypeWrapper(type));
            var cleanName = type.Name.Split('`')[0];
            LuaApi.SetGlobal(_state, cleanName);
        }

        private void LoadStringInternal(string s) {
            if (LuaApi.LoadString(_state, s) != LuaStatus.Ok) {
                var errorMessage = LuaApi.ToString(_state, -1);
                LuaApi.Pop(_state, 1);
                throw new LuaException(errorMessage);
            }
        }
    }
}
