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
using System.Runtime.InteropServices;
using Triton.Interop;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment.
    /// </summary>
    public class LuaEnvironment : IDisposable
    {
        private const string GcHelperMetatable = "<>__gcHelper";

        // A callback that is run when a Lua GC occurs. This is done by creating a table with a `__gc` metamethod.
        private static readonly lua_CFunction _gcCallback = GcCallback;

        // The main Lua state.
        private readonly IntPtr _state;

        // A handle to the `LuaEnvironment`. By storing the handle in the "extra space" portion of a Lua state, we can
        // retrieve the `LuaEnvironment` instance that a Lua state is attached to.
        private readonly GCHandle _selfHandle;

        // A cache of Lua objects. Maps a pointer (retrieved via `lua_topointer`) to the reference in the Lua registry
        // along with a weak reference to the Lua object. This allows for efficient lookup and cleanup.
        private readonly Dictionary<IntPtr, (int reference, WeakReference<LuaObject> weakReference)> _luaObjects =
            new Dictionary<IntPtr, (int, WeakReference<LuaObject>)>();

        private readonly ClrMetatableManager _metatableCache;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);  // Open all standard libraries by default for convenience

            _selfHandle = GCHandle.Alloc(this, GCHandleType.Weak);
            Marshal.WriteIntPtr(lua_getextraspace(_state), GCHandle.ToIntPtr(_selfHandle));

            _metatableCache = new ClrMetatableManager(_state, this);

            luaL_newmetatable(_state, GcHelperMetatable);
            lua_pushstring(_state, "__gc");
            lua_pushcfunction(_state, _gcCallback);
            lua_settable(_state, -3);
            lua_pop(_state, 1);

            SetupGcCallback(_state);
        }

        // A finalizer is _NOT_ feasible here. If the finalizer calls `lua_close` during an unmanaged -> managed
        // transition (which is possible since finalizers run on a separate thread), we'll never be able to make the
        // managed -> unmanaged transition.
        //
        // Thus, you must ensure that `LuaEnvironment` instances are properly disposed of!!

        /// <summary>
        /// Gets or sets the value of the given <paramref name="global"/>.
        /// </summary>
        /// <param name="global">The global.</param>
        /// <returns>The value of the given <paramref name="global"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="global"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaVariant this[string global]
        {
            get
            {
                if (global is null)
                {
                    throw new ArgumentNullException(nameof(global));
                }

                ThrowIfDisposed();

                lua_settop(_state, 0);  // Reset stack

                var type = lua_getglobal(_state, global);
                ToVariant(_state, -1, out var variant, type);
                return variant;
            }

            set
            {
                if (global is null)
                {
                    throw new ArgumentNullException(nameof(global));
                }

                ThrowIfDisposed();

                lua_settop(_state, 0);  // Reset stack

                value.Push(_state);
                lua_setglobal(_state, global);
            }
        }

        private static int GcCallback(IntPtr state)
        {
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(lua_getextraspace(state)));
            if (!(handle.Target is LuaEnvironment environment))
            {
                return 0;
            }

            var luaObjects = environment._luaObjects;

            // Scan through the Lua objects, cleaning up any which have been GC'd by the CLR. The objects within Lua
            // will then be cleaned up on the next Lua GC.
            var deadPtrs = new List<IntPtr>(luaObjects.Count);
            foreach (var (ptr, (reference, weakReference)) in luaObjects)
            {
                if (!weakReference.TryGetTarget(out _))
                {
                    luaL_unref(state, LUA_REGISTRYINDEX, reference);
                    deadPtrs.Add(ptr);
                }
            }

            foreach (var deadPtr in deadPtrs)
            {
                luaObjects.Remove(deadPtr);
            }

            SetupGcCallback(state);
            return 0;
        }

        private static void SetupGcCallback(IntPtr state)
        {
            lua_newtable(state);
            luaL_setmetatable(state, GcHelperMetatable);
            lua_pop(state, 1);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                foreach (var (_, (_, weakReference)) in _luaObjects)
                {
                    if (weakReference.TryGetTarget(out var obj))
                    {
                        obj.Dispose();
                    }
                }

                lua_close(_state);
                _selfHandle.Free();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Creates a new Lua table with the given initial capacities.
        /// </summary>
        /// <param name="sequentialCapacity">The initial sequential capacity for the table.</param>
        /// <param name="nonSequentialCapacity">The initial non-sequential capacity for the table.</param>
        /// <returns>The resulting Lua table.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sequentialCapacity"/> or <paramref name="nonSequentialCapacity"/> are negative.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaTable CreateTable(int sequentialCapacity = 0, int nonSequentialCapacity = 0)
        {
            if (sequentialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequentialCapacity), "Sequential capacity is negative");
            }

            if (nonSequentialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nonSequentialCapacity), "Non-sequential capacity is negative");
            }

            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_createtable(_state, sequentialCapacity, nonSequentialCapacity);

            var ptr = lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var table = new LuaTable(this, reference, _state);

            _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(table));
            return table;
        }

        /// <summary>
        /// Creates a Lua function from loading the given Lua <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The Lua chunk to load.</param>
        /// <returns>The resulting Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaFunction CreateFunction(string chunk)
        {
            LoadString(chunk);  // Performs validation

            var ptr = lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(this, reference, _state);

            _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(function));
            return function;
        }

        /// <summary>
        /// Creates a Lua thread.
        /// </summary>
        /// <returns>The resulting Lua thread.</returns>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaThread CreateThread()
        {
            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            var ptr = lua_newthread(_state);

            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var thread = new LuaThread(this, reference, ptr);

            _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(thread));
            return thread;
        }

        /// <summary>
        /// Evaluates the given Lua <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The Lua chunk to evaluate.</param>
        /// <returns>The results.</returns>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the chunk.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Eval(string chunk)
        {
            LoadString(chunk);  // Performs validation

            var status = lua_pcall(_state, 0, -1, 0);
            if (status != LuaStatus.Ok)
            {
                throw CreateExceptionFromStack<LuaEvalException>(_state);
            }

            return new LuaResults(this, _state);
        }

        /// <summary>
        /// Pushes a CLR type onto the stack of the given Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        internal void PushClrType(IntPtr state, Type type)
        {
            // TODO: can perform caching of types here
            PushClrTypeOrObject(state, type, isType: true);
            _metatableCache.PushClrTypeMetatable(state, type);
            lua_setmetatable(state, -2);
        }

        /// <summary>
        /// Pushes a CLR object onto the stack of the given Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The CLR object.</param>
        internal void PushClrObject(IntPtr state, object obj)
        {
            // TODO: can perform caching of objects here
            PushClrTypeOrObject(state, obj, isType: false);
            _metatableCache.PushClrObjectMetatable(state, obj);
            lua_setmetatable(state, -2);
        }

        /// <summary>
        /// Converts the Lua value on the stack of the given Lua <paramref name="state"/> at <paramref name="index"/>
        /// into a Lua <paramref name="variant"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the Lua value on the stack.</param>
        /// <param name="variant">The resulting Lua variant.</param>
        internal void ToVariant(IntPtr state, int index, out LuaVariant variant) =>
            ToVariant(state, index, out variant, lua_type(state, index));

        /// <summary>
        /// Converts the Lua value on the stack of the given Lua <paramref name="state"/> at <paramref name="index"/>
        /// into a Lua <paramref name="variant"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the Lua value on the stack.</param>
        /// <param name="variant">The resulting Lua variant.</param>
        /// <param name="type">The type of the Lua value.</param>
        internal void ToVariant(IntPtr state, int index, out LuaVariant variant, LuaType type)
        {
            Debug.Assert(state != IntPtr.Zero);

            variant = type switch
            {
                LuaType.None => default,
                LuaType.Nil => default,
                LuaType.Boolean => lua_toboolean(state, index),
                LuaType.Number => ToIntegerOrNumber(state, index),
                LuaType.String => lua_tostring(state, index),
                LuaType.Table => ToLuaObject(state, index, LuaType.Table),
                LuaType.Function => ToLuaObject(state, index, LuaType.Function),
                LuaType.Userdata => ToClrTypeOrObject(state, index),
                _ => ToLuaObject(state, index, LuaType.Thread),
            };

            static LuaVariant ToIntegerOrNumber(IntPtr state, int index) =>
                lua_isinteger(state, index) ? (LuaVariant)lua_tointeger(state, index) : lua_tonumber(state, index);

            LuaObject ToLuaObject(IntPtr state, int index, LuaType type)
            {
                LuaObject obj;

                var ptr = lua_topointer(state, index);
                if (_luaObjects.TryGetValue(ptr, out var tuple))
                {
                    if (tuple.weakReference.TryGetTarget(out obj))
                    {
                        return obj;
                    }
                }
                else
                {
                    // Create a reference to the Lua object in the Lua registry.
                    lua_pushvalue(state, index);
                    tuple.reference = luaL_ref(state, LUA_REGISTRYINDEX);
                }

                obj = type switch
                {
                    LuaType.Table => new LuaTable(this, tuple.reference, state),
                    LuaType.Function => new LuaFunction(this, tuple.reference, state),
                    _ => new LuaThread(this, tuple.reference, ptr)
                };

                tuple.weakReference = new WeakReference<LuaObject>(obj);
                _luaObjects[ptr] = tuple;
                return obj;
            }

            static LuaVariant ToClrTypeOrObject(IntPtr state, int index)
            {
                var ptr = lua_touserdata(state, index);
                var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));

                lua_getiuservalue(state, -1, 1);
                var isType = lua_toboolean(state, -1);
                lua_pop(state, 1);

                return isType ? LuaVariant.FromClrType((Type)handle.Target) : LuaVariant.FromClrObject(handle.Target);
            }
        }

        /// <summary>
        /// Creates a <typeparamref name="TException"/> from the top of the stack of the given Lua
        /// <paramref name="state"/>.
        /// </summary>
        /// <typeparam name="TException">The type of exception.</typeparam>
        /// <param name="state">The Lua state.</param>
        /// <returns>The exception.</returns>
        internal TException CreateExceptionFromStack<TException>(IntPtr state)
        {
            Debug.Assert(state != IntPtr.Zero);
            Debug.Assert(lua_type(state, -1) == LuaType.String);

            var message = lua_tostring(state, -1);
            return (TException)Activator.CreateInstance(typeof(TException), message);
        }

        private void PushClrTypeOrObject(IntPtr state, object typeOrObj, bool isType)
        {
            Debug.Assert(state != IntPtr.Zero);
            Debug.Assert(typeOrObj != null);

            var handle = GCHandle.Alloc(typeOrObj);
            var ptr = lua_newuserdatauv(state, (UIntPtr)IntPtr.Size, 1);
            Marshal.WriteIntPtr(ptr, GCHandle.ToIntPtr(handle));

            lua_pushboolean(state, isType);
            lua_setiuservalue(state, -2, 1);
        }

        private void LoadString(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            var status = luaL_loadstring(_state, chunk);
            if (status != LuaStatus.Ok)
            {
                throw CreateExceptionFromStack<LuaLoadException>(_state);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
