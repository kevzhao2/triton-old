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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Triton.Interop;
using static Triton.Lua;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment. This is the entrypoint for embedding Lua into a CLR application.
    /// 
    /// <para/>
    /// 
    /// This class is <i>not</i> thread-safe.
    /// </summary>
    public sealed unsafe class LuaEnvironment : IDisposable
    {
        private readonly lua_State* _state;
        private readonly LuaObjectManager _luaObjects;
        private readonly ClrEntityManager _clrEntities;

        private readonly int _gcMetatableRef;

        private readonly Lazy<ModuleBuilder> _lazyModuleBuilder = new(() =>
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new("Triton.Interop.Emit.Generated"), AssemblyBuilderAccess.RunAndCollect);
            return assemblyBuilder.DefineDynamicModule("Triton.Interop.Emit.Generated");
        });

        private LuaTable? _globals;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);

            _luaObjects = new(this);
            _clrEntities = new();

            // Store a weak handle to the Lua environment. This allows us to retrieve the environment.

            var handle = GCHandle.Alloc(this, GCHandleType.Weak);
            *(IntPtr*)lua_getextraspace(_state) = GCHandle.ToIntPtr(handle);

            // Set up a metatable with a `__gc` metamethod pointing to `GcMetamethod`. This allows us to invoke the
            // `GarbageCollection` event whenever Lua performs a garbage collection.

            lua_createtable(_state, 0, 1);
            lua_pushcfunction(_state, &GcMetamethod);
            lua_setfield(_state, -2, "__gc");
            _gcMetatableRef = luaL_ref(_state, LUA_REGISTRYINDEX);

            PushGarbage();
        }
        
        /// <summary>
        /// An event that occurs when a Lua garbage collection occurs.
        /// </summary>
        public event EventHandler? GarbageCollection;

        /// <summary>
        /// Gets the environment's globals as a table.
        /// </summary>
        /// <value>The environment's globals as a table.</value>
        public LuaTable Globals => _globals ??= new(_state, this, LUA_RIDX_GLOBALS);

        internal ModuleBuilder ModuleBuilder => _lazyModuleBuilder.Value;

        /// <summary>
        /// Gets or sets the value of the global with the given name.
        /// </summary>
        /// <param name="name">The name of the global whose value to get or set.</param>
        /// <value>The value of the global.</value>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public LuaValue this[string name]
        {
            get
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                CheckSelf();  // Performs validation

                var type = lua_getglobal(_state, name);
                LuaValue.FromLua(_state, -1, type, out var value);
                return value;
            }

            set
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                CheckSelf();  // Performs validation

                value.Push(_state);
                lua_setglobal(_state, name);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static int GcMetamethod(lua_State* state)
        {
            var environment = lua_getenvironment(state);
            environment._luaObjects.Clean(state);
            environment.GarbageCollection?.Invoke(environment, EventArgs.Empty);

            environment.PushGarbage();
            return 0;
        }

        /// <summary>
        /// Dispose the Lua environment, freeing its unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            // To dispose of the environment, we must perform the following steps in a very specific order:
            // 1. Retrieve the handle allocated in the constructor. This must be done before closing the state as the
            //    handle would otherwise be lost.
            // 2. Close the state. This must be done before freeing the handle as `GcMetamethod` relies on the handle
            //    being valid.
            // 3. Free the handle.

            var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_getextraspace(_state));
            lua_close(_state);
            handle.Free();

            _isDisposed = true;
        }

        /// <summary>
        /// Creates a new Lua table with the given initial capacities.
        /// </summary>
        /// <param name="arrayCapacity">The initial array capacity of the table.</param>
        /// <param name="recordCapacity">The initial record capacity of the table.</param>
        /// <returns>The resulting Lua table.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="arrayCapacity"/> or <paramref name="recordCapacity"/> are negative.
        /// </exception>
        public LuaTable CreateTable(int arrayCapacity = 0, int recordCapacity = 0)
        {
            if (arrayCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayCapacity), "Array capacity is negative");
            }

            if (recordCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(recordCapacity), "Record capacity is negative");
            }

            CheckSelf();  // Performs validation

            lua_createtable(_state, arrayCapacity, recordCapacity);

            var ptr = lua_topointer(_state, -1);
            var @ref = luaL_ref(_state, LUA_REGISTRYINDEX);
            var table = new LuaTable(_state, this, @ref);

            return _luaObjects.Intern((IntPtr)ptr, @ref, table);
        }

        /// <summary>
        /// Creates a new Lua function from the given Lua chunk.
        /// </summary>
        /// <param name="chunk">The Lua chunk to create a function from.</param>
        /// <returns>The resulting Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred during loading.</exception>
        public LuaFunction CreateFunction(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            CheckSelf();  // Performs validation

            luaL_loadstring(_state, chunk);

            var ptr = lua_topointer(_state, -1);
            var @ref = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(_state, this, @ref);

            return _luaObjects.Intern((IntPtr)ptr, @ref, function);
        }

        /// <summary>
        /// Creates a new Lua thread.
        /// </summary>
        /// <returns>The resulting Lua thread.</returns>
        public LuaThread CreateThread()
        {
            CheckSelf();  // Performs validation

            var ptr = lua_newthread(_state);
            var @ref = luaL_ref(_state, LUA_REGISTRYINDEX);
            var thread = new LuaThread(ptr, this, @ref);

            return _luaObjects.Intern((IntPtr)ptr, @ref, thread);
        }

        /// <summary>
        /// Evaluates the given Lua chunk.
        /// </summary>
        /// <param name="chunk">The Lua chunk to evaluate.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">The evaluation results in a Lua load error.</exception>
        /// <exception cref="LuaRuntimeException">The evaluation results in a Lua runtime error.</exception>
        public LuaResults Eval(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            CheckSelf();  // Performs validation

            luaL_loadstring(_state, chunk);
            return lua_pcall(_state, 0, -1, 0);
        }

        /// <summary>
        /// Imports the public types from the given assembly as globals.
        /// </summary>
        /// <param name="assembly">The assembly to import public types from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
        public void ImportTypes(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            ThrowIfDisposed();

            var tables = new Dictionary<string, LuaTable>
            {
                [string.Empty] = Globals
            };

            foreach (var grouping in assembly.ExportedTypes
                .Where(t => !t.IsNested)
                .GroupBy(t => t.FullName!.Split('`')[0]))
            {
                var index = grouping.Key.LastIndexOf('.');
                var @namespace = grouping.Key[..index];
                var name = grouping.Key[(index + 1)..];

                GetTable(@namespace)[name] = LuaValue.FromClrTypes(grouping.ToList());
            }

            return;

            LuaTable GetTable(string @namespace)
            {
                if (tables.TryGetValue(@namespace, out var table))
                {
                    return table;
                }

                var index = @namespace.LastIndexOf('.');
                var parentNamespace = @namespace[..Math.Max(0, index)];
                var childNamespace = @namespace[(index + 1)..];

                var parent = GetTable(parentNamespace);
                if (!parent.TryGetValue(childNamespace, out var value) ||
                    !value.IsLuaObject || ((LuaObject)value) is not LuaTable child)
                {
                    child = CreateTable();
                    parent[childNamespace] = child;
                }

                tables.Add(@namespace, child);
                return child;
            }
        }

        internal void PushClrEntity(lua_State* state, object entity, bool isTypes) =>
            _clrEntities.Push(state, entity, isTypes);

        internal LuaObject LoadLuaObject(lua_State* state, int index, LuaType type) => 
           _luaObjects.Load(state, index, type);

        internal object LoadClrEntity(lua_State* state, int index) =>
            lua_tohandle(state, index).Target!;

        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void CheckSelf()
        {
            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack
        }

        private void PushGarbage()
        {
            lua_newtable(_state);
            lua_rawgeti(_state, LUA_REGISTRYINDEX, _gcMetatableRef);
            lua_setmetatable(_state, -2);
            lua_pop(_state, 1);
        }
    }
}
