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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Triton.Interop;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment, the entrypoint for embedding Lua into a CLR application.
    /// </summary>
    /// <remarks>
    /// This class is <i>not</i> thread-safe, and <i>must</i> be disposed of to prevent a memory leak.
    /// </remarks>
    public sealed unsafe class LuaEnvironment : IDisposable
    {
        private readonly lua_State* _state;
        private readonly MetatableGenerator _metatableGenerator = new();

        // Stores a mapping from namespaces to tables. This reduces the 
        private readonly Dictionary<string, LuaTable> _namespaceTables = new();

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            var state = luaL_newstate();
            luaL_openlibs(state);

            // Store a handle to the environment in the extra space portion of the Lua state. This enables us to
            // retrieve the environment associated with a Lua state, allowing the usage of static callbacks (and hence
            // unmanaged function pointers).
            //
            var handle = GCHandle.Alloc(this);
            *(IntPtr*)lua_getextraspace(state) = GCHandle.ToIntPtr(handle);

            _state = state;

            MainThread = new(_state, LUA_RIDX_MAINTHREAD);
            Globals    = new(_state, LUA_RIDX_GLOBALS);
        }

        /// <summary>
        /// Gets the environment's main thread.
        /// </summary>
        public LuaThread MainThread { get; }

        /// <summary>
        /// Gets the environment's globals table.
        /// </summary>
        public LuaTable Globals { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                var state = _state;  // local optimization

                // Cleanup must be done in a very specific order:
                // - We must retrieve the `GCHandle` in the extra space portion of the Lua state prior to closing it.
                // - We must close the state prior to freeing the handle (since the `__gc` metamethods depend on it).
                //
                var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_getextraspace(state));
                lua_close(state);
                handle.Free();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Gets the value of the global with the given name.
        /// </summary>
        /// <param name="name">The name of the global to get.</param>
        /// <returns>The value of the global.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public LuaResult GetGlobal(string name)
        {
            if (name is null)
                ThrowHelper.ThrowArgumentNullException(nameof(name));

            ThrowIfDisposed();

            var state = _state;  // local optimization

            lua_settop(state, 0);  // ensure that the value will be at index 1

            _ = lua_getglobal(state, name);

            return new(state, 1);
        }

        /// <summary>
        /// Sets the value of the global with the given name.
        /// </summary>
        /// <param name="name">The name of the global to set.</param>
        /// <param name="value">The value to set the global to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public void SetGlobal(string name, LuaArgument value)
        {
            if (name is null)
                ThrowHelper.ThrowArgumentNullException(nameof(name));

            ThrowIfDisposed();

            var state = _state;  // local optimization

            value.Push(state);
            lua_setglobal(state, name);
        }

        /// <summary>
        /// Evaluates the given string.
        /// </summary>
        /// <param name="str">The string to evaluate.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">The string results in a Lua load error.</exception>
        /// <exception cref="LuaRuntimeException">The string results in a Lua runtime error.</exception>
        public LuaResults Eval(string str)
        {
            if (str is null)
                ThrowHelper.ThrowArgumentNullException(nameof(str));

            ThrowIfDisposed();

            var state = _state;  // local optimization

            lua_settop(state, 0);  // ensure that the results will begin at index 1

            luaL_loadstring(state, str);
            return lua_pcall(state, 0, LUA_MULTRET);
        }

        /// <summary>
        /// Creates a new table with the given initial capacities.
        /// </summary>
        /// <param name="arrayCapacity">The initial capacity of the array portion of the table.</param>
        /// <param name="hashCapacity">The initial capacity of the hash portion of the table.</param>
        /// <returns>The resulting table.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayCapacity"/> or <paramref name="hashCapacity"/> are negative.</exception>
        public LuaTable CreateTable(int arrayCapacity = 0, int hashCapacity = 0)
        {
            if (arrayCapacity < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(arrayCapacity), "Array capacity is negative");
            if (hashCapacity < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(hashCapacity), "Hash capacity is negative");

            ThrowIfDisposed();

            var state = _state;  // local optimization

            lua_createtable(state, arrayCapacity, hashCapacity);
            return new(state, luaL_ref(state, LUA_REGISTRYINDEX));
        }

        /// <summary>
        /// Creates a new function from the given string.
        /// </summary>
        /// <param name="str">The string to create a function from.</param>
        /// <returns>The resulting function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">The string results in a Lua load error.</exception>
        public LuaFunction CreateFunction(string str)
        {
            if (str is null)
                ThrowHelper.ThrowArgumentNullException(nameof(str));

            ThrowIfDisposed();

            var state = _state;  // local optimization

            luaL_loadstring(state, str);
            return new(state, luaL_ref(state, LUA_REGISTRYINDEX));
        }

        /// <summary>
        /// Creates a new thread.
        /// </summary>
        /// <returns>The resulting thread.</returns>
        public LuaThread CreateThread()
        {
            ThrowIfDisposed();

            var state = _state;  // local optimization

            var threadState = lua_newthread(state);
            return new(threadState, luaL_ref(state, LUA_REGISTRYINDEX));
        }

        /// <summary>
        /// Imports the exported types from the specified assembly as globals.
        /// </summary>
        /// <param name="assembly">The assembly to import exported types from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
        public void ImportTypes(Assembly assembly)
        {
            if (assembly is null)
                ThrowHelper.ThrowArgumentNullException(nameof(assembly));

            ThrowIfDisposed();

            foreach (var grouping in assembly.ExportedTypes
                .Where(t => !t.IsNested)
                .GroupBy(t => t.FullName!.Split('`')[0]))
            {
                var fullName = grouping.Key;
                var types = LuaArgument.FromClrTypes(grouping.ToArray());

                var index = fullName.LastIndexOf('.');
                if (index == -1)
                    SetGlobal(fullName, types);
                else
                    GetTable(fullName[..index]).SetValue(fullName[(index + 1)..], types);
            }

            return;

            // Recursively gets the table corresponding to the specified namespace. This uses 
            LuaTable GetTable(string @namespace)
            {
                if (_namespaceTables.TryGetValue(@namespace, out var table))
                    return table;

                var index = @namespace.LastIndexOf('.');
                if (index == -1)
                    SetGlobal(@namespace, table = CreateTable());
                else
                    GetTable(@namespace[..index]).SetValue(@namespace[(index + 1)..], table = CreateTable());

                _namespaceTables.Add(@namespace, table);
                return table;
            }
        }

        internal void PushClrObject(lua_State* state, object obj)
        {
            var handle = GCHandle.Alloc(obj);
            *(IntPtr*)lua_newuserdatauv(state, (nuint)IntPtr.Size, 0) = GCHandle.ToIntPtr(handle);

            _metatableGenerator.Push(state, obj);
            lua_setmetatable(state, -2);
        }

        internal void PushClrTypes(lua_State* state, Type[] types)
        {
            var handle = GCHandle.Alloc(types);
            *(nint*)lua_newuserdatauv(state, (nuint)IntPtr.Size, 0) = (nint)GCHandle.ToIntPtr(handle) | 1;

            _metatableGenerator.Push(state, types);
            lua_setmetatable(state, -2);
        }

        [ExcludeFromCodeCoverage]
        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                ThrowHelper.ThrowObjectDisposedException(nameof(LuaEnvironment));
        }
    }
}
