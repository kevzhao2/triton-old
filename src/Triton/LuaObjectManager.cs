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
using static Triton.NativeMethods;

namespace Triton.Interop.Lua
{
    /// <summary>
    /// Manages Lua objects. Controls the lifetime of <see cref="LuaObject"/>s and provides methods to manipulate and
    /// retrieve them.
    /// </summary>
    internal sealed class LuaObjectManager
    {
        private readonly LuaEnvironment _environment;

        private readonly Dictionary<IntPtr, (int @ref, WeakReference<LuaObject> weakReference)> _objects =
            new Dictionary<IntPtr, (int @ref, WeakReference<LuaObject> weakReference)>();

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetatableRef;

        internal LuaObjectManager(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            _gcMetamethod = GcMetamethod;  // Prevent garbage collection of the delegate
            lua_newtable(state);
            lua_pushcfunction(state, _gcMetamethod);
            lua_setfield(state, -2, Strings.__gc);
            _gcMetatableRef = luaL_ref(state, LUA_REGISTRYINDEX);

            PushGarbage(state);
        }

        /// <summary>
        /// Interns the given Lua object in the cache.
        /// </summary>
        /// <param name="ptr">The object pointer.</param>
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
            lua_setmetatable(state, -1);
            lua_pop(state, 1);
        }
    }
}
