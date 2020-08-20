// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Manages Lua objects. Controls the lifetime of <see cref="LuaObject"/>s and provides methods to manipulate and
    /// retrieve them.
    /// </summary>
    internal sealed class LuaObjectManager
    {
        private readonly LuaEnvironment _environment;

        // We would like to use the same `LuaObject` instances for the same Lua objects. This is accomplished by storing
        // them inside of the Lua registry and using the following object cache.

        private readonly Dictionary<IntPtr, (int @ref, WeakReference<LuaObject> weakReference)> _objects =
            new Dictionary<IntPtr, (int @ref, WeakReference<LuaObject> weakReference)>();

        // In order to clean up Lua objects which have been garbage collected by the CLR, we will execute cleanups
        // whenever a garbage collection is performed by Lua. This will enable the Lua objects to be removed from the
        // registry, making them eligible for garbage collection by Lua.

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetatableRef;

        internal LuaObjectManager(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            _gcMetamethod = GcMetamethod;  // Prevent garbage collection of the delegate
            lua_createtable(state, 0, 1);
            lua_pushcfunction(state, _gcMetamethod);
            lua_setfield(state, -2, "__gc");
            _gcMetatableRef = luaL_ref(state, LUA_REGISTRYINDEX);

            PushGarbage(state);
        }

        /// <summary>
        /// Interns the given Lua object in the cache.
        /// </summary>
        /// <param name="ptr">The Lua object pointer.</param>
        /// <param name="obj">The Lua object.</param>
        public void Intern(IntPtr ptr, LuaObject obj)
        {
            _objects.Add(ptr, (obj._ref, new WeakReference<LuaObject>(obj)));
        }

        /// <summary>
        /// Pushes the given Lua object onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The Lua object.</param>
        public void Push(IntPtr state, LuaObject obj)
        {
            if (obj._environment != _environment)
            {
                throw new InvalidOperationException("Lua object does not belong to this environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, obj._ref);
        }

        /// <summary>
        /// Loads a Lua object from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting Lua object.</returns>
        public LuaObject Load(IntPtr state, int index, LuaType type)
        {
            LuaObject? obj;

            var ptr = lua_topointer(state, index);
            if (_objects.TryGetValue(ptr, out var tuple))
            {
                if (tuple.weakReference.TryGetTarget(out obj))
                {
                    return obj;
                }
            }
            else
            {
                lua_pushvalue(state, index);
                tuple.@ref = luaL_ref(state, LUA_REGISTRYINDEX);
            }

            obj = type switch
            {
                LuaType.Table    => new LuaTable(state, _environment, tuple.@ref),
                LuaType.Function => new LuaFunction(state, _environment, tuple.@ref),
                _                => new LuaThread(ptr, _environment, tuple.@ref)
            };

            Intern(ptr, obj);
            return obj;
        }

        private int GcMetamethod(IntPtr state)
        {
            CleanRefs(state);
            PushGarbage(state);
            return 0;

            void CleanRefs(IntPtr state)
            {
                var deadPtrs = new List<IntPtr>();
                foreach (var (ptr, (@ref, weakReference)) in _objects)
                {
                    if (!weakReference.TryGetTarget(out _))
                    {
                        luaL_unref(state, LUA_REGISTRYINDEX, @ref);
                        deadPtrs.Add(ptr);
                    }
                }

                foreach (var deadPtr in deadPtrs)
                {
                    _objects.Remove(deadPtr);
                }
            }
        }

        private void PushGarbage(IntPtr state)
        {
            lua_newtable(state);
            lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetatableRef);
            lua_setmetatable(state, -2);
            lua_pop(state, 1);
        }
    }
}
