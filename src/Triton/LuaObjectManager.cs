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
using System.Runtime.CompilerServices;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Manages Lua objects.
    /// </summary>
    internal sealed class LuaObjectManager : IDisposable
    {
        private const string GcHelperMetatable = "<>__gcHelper";

        private readonly LuaEnvironment _environment;
        private readonly Dictionary<IntPtr, (int reference, WeakReference<LuaObject> weakReference)> _objects;
        private readonly lua_CFunction _gcCallback;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaObjectManager"/> class with the specified Lua
        /// <paramref name="state"/> and <paramref name="environment"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="environment">The Lua environment.</param>
        internal LuaObjectManager(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            // Create a cache of Lua objects, which maps a pointer (retrieved via `lua_topointer`) to the reference in
            // the Lua registry along with a weak reference to the Lua object.
            //
            // This weak reference is required, as otherwise the Lua object will never be garbage collected.
            //
            _objects = new Dictionary<IntPtr, (int, WeakReference<LuaObject>)>();

            // Set up a metatable with a `__gc` metamethod. This allows us to clean up dead Lua objects whenever a Lua
            // garbage collection occurs.
            //
            _gcCallback = GcCallback;
            lua_newtable(state);
            luaL_newmetatable(state, GcHelperMetatable);

            lua_pushcfunction(state, _gcCallback);
            lua_setfield(state, -2, "__gc");

            lua_setmetatable(state, -2);
            lua_pop(state, 1);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                foreach (var (_, (_, weakReference)) in _objects)
                {
                    if (weakReference.TryGetTarget(out var obj))
                    {
                        obj.Dispose();
                    }
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Interns the given <paramref name="luaObj"/>.
        /// </summary>
        /// <param name="luaObj">The Lua object.</param>
        /// <param name="ptr">The pointer to the Lua object.</param>
        internal void InternLuaObject(LuaObject luaObj, IntPtr ptr) =>
            _objects[ptr] = (luaObj._reference, new WeakReference<LuaObject>(luaObj));

        /// <summary>
        /// Pushes the given <paramref name="obj"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The Lua object.</param>
        internal void PushLuaObject(IntPtr state, LuaObject obj)
        {
            if (obj._environment != _environment)  // Ensure that the environments match
            {
                throw new InvalidOperationException("Lua object does not belong to the given state's environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, obj._reference);
        }

        /// <summary>
        /// Converts the Lua object on the stack of the Lua <paramref name="state"/> at the given
        /// <paramref name="index"/> to a Lua <paramref name="value"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the Lua object on the stack.</param>
        /// <param name="type">The type of the Lua object.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal void ToLuaObject(IntPtr state, int index, LuaType type, out LuaValue value)
        {
            value = default;
            Unsafe.AsRef(in value._integer) = 2;
            ref var obj = ref Unsafe.As<object?, LuaObject>(ref Unsafe.AsRef(in value._objectOrTag));

            var ptr = lua_topointer(state, index);
            if (_objects.TryGetValue(ptr, out var tuple))
            {
                if (tuple.weakReference.TryGetTarget(out obj))
                {
                    return;
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
                LuaType.Table    => new LuaTable(state, _environment, tuple.reference),
                LuaType.Function => new LuaFunction(state, _environment, tuple.reference),
                _                => new LuaThread(ptr, _environment, tuple.reference)
            };

            tuple.weakReference = new WeakReference<LuaObject>(obj);
            _objects[ptr] = tuple;
        }

        private int GcCallback(IntPtr state)
        {
            var deadPtrs = new List<IntPtr>(_objects.Count);

            // Scan through the Lua objects, cleaning up any which have been cleaned up by the CLR. The objects within
            // Lua will then be cleaned up on the next Lua garbage collection.
            //
            foreach (var (ptr, (reference, weakReference)) in _objects)
            {
                if (!weakReference.TryGetTarget(out _))
                {
                    luaL_unref(state, LUA_REGISTRYINDEX, reference);
                    deadPtrs.Add(ptr);
                }
            }

            foreach (var deadPtr in deadPtrs)
            {
                _objects.Remove(deadPtr);
            }

            // Create a new table which triggers `GcCallback` upon being garbage collected.
            //
            lua_newtable(state);
            luaL_setmetatable(state, GcHelperMetatable);
            lua_pop(state, 1);
            return 0;
        }
    }
}
