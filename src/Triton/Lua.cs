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
#if NETSTANDARD || NET40
using System.Dynamic;
#endif
using Triton.Binding;
using Triton.Interop;

[assembly: CLSCompliant(true)]

namespace Triton {
    /// <summary>
    /// Acts as a managed wrapper around a Lua instance.
    /// </summary>
#if NETSTANDARD || NET40
    public sealed class Lua : DynamicObject, IDisposable {
#else
    public sealed class Lua : IDisposable {
#endif
        private readonly IntPtr _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lua"/> class.
        /// </summary>
        public Lua() {
            _state = LuaApi.NewState();
            LuaApi.OpenLibs(_state);

            ObjectBinder.InitializeMetatables(_state);
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

                return GetGlobalInternal(name);
            }
            set {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                if (IsDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                SetGlobalInternal(name, value);
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

            LuaApi.CreateTable(_state);
            var tableReference = LuaApi.Ref(_state, LuaApi.RegistryIndex);
            return new LuaTable(_state, tableReference);
        }

        /// <summary>
        /// Creates a <see cref="LuaThread"/> that will run the given <see cref="LuaFunction"/>.
        /// </summary>
        /// <returns>The <see cref="LuaThread"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the <see cref="Lua"/> instance is disposed or <paramref name="function"/> is disposed.
        /// </exception>
        public LuaThread CreateThread(LuaFunction function) {
            if (function == null) {
                throw new ArgumentNullException(nameof(function));
            }
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (function.IsDisposed) {
                throw new ObjectDisposedException(function.GetType().FullName);
            }

            var thread = LuaApi.NewThread(_state);
            LuaApi.RawGetI(thread, LuaApi.RegistryIndex, function.Reference);
            var threadReference = LuaApi.Ref(_state, LuaApi.RegistryIndex);
            return new LuaThread(_state, threadReference, thread);
        }

        /// <summary>
        /// Disposes the <see cref="Lua"/> instance.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

            using (var function = LoadStringInternal(s)) {
                return function.Call();
            }
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

            return LoadStringInternal(s);
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            result = GetGlobalInternal(binder.Name);
            return true;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">The <see cref="Lua"/> instance is disposed.</exception>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            if (IsDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            
            SetGlobalInternal(binder.Name, value);
            return true;
        }
#endif

        private object GetGlobalInternal(string name) {
            var type = LuaApi.GetGlobal(_state, name);
            var result = LuaApi.ToObject(_state, -1, type);
            LuaApi.Pop(_state, 1);
            return result;
        }

        private void SetGlobalInternal(string name, object value) {
            LuaApi.PushObject(_state, value);
            LuaApi.SetGlobal(_state, name);
        }

        private void Dispose(bool disposing) {
            if (IsDisposed) {
                return;
            }
            
            LuaApi.Close(_state);
            IsDisposed = true;
        }

        private void ImportTypeInternal(Type type) {
            ObjectBinder.PushNetObject(_state, new TypeWrapper(type));
            var cleanName = type.Name.Split('`')[0];
            LuaApi.SetGlobal(_state, cleanName);
        }

        private LuaFunction LoadStringInternal(string s) {
            if (LuaApi.LoadString(_state, s) != LuaStatus.Ok) {
                var errorMessage = LuaApi.ToString(_state, -1);
                LuaApi.Pop(_state, 1);
                throw new LuaException(errorMessage);
            }

            var functionReference = LuaApi.Ref(_state, LuaApi.RegistryIndex);
            return new LuaFunction(_state, functionReference);
        }
    }
}
