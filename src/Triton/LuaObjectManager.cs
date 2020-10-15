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
using static Triton.Lua;
using static Triton.Lua.LuaType;

namespace Triton
{
    /// <summary>
    /// Manages Lua objects. Controls the lifetime of <see cref="LuaObject"/> instances and provides methods to
    /// manipulate and retrieve them.
    /// </summary>
    internal sealed unsafe class LuaObjectManager
    {
        private readonly LuaEnvironment _environment;

        private readonly Dictionary<IntPtr, (int @ref, WeakReference<LuaObject> weakRef)> _cache = new();

        internal LuaObjectManager(LuaEnvironment environment)
        {
            _environment = environment;
        }

        public TLuaObject Intern<TLuaObject>(IntPtr ptr, int @ref, TLuaObject obj) where TLuaObject : LuaObject
        {
            // The pointer may already exist in the cache. This can happen if the Lua object was garbage collected by
            // the CLR and then reconstructed.

            _cache[ptr] = (@ref, new(obj));
            return obj;
        }

        public LuaObject Load(lua_State* state, int index, LuaType type)
        {
            LuaObject? obj;

            // There are two cases:
            // - If the Lua object is present in the cache, then the Lua object exists in the registry. However, the
            //   Lua object may have been garbage collected by the CLR, so it is reconstructed if necessary.
            // - If the Lua object is NOT present in the cache, then the Lua object is placed in the registry and
            //   constructed.

            var ptr = lua_topointer(state, index);
            if (_cache.TryGetValue((IntPtr)ptr, out var tuple))
            {
                if (tuple.weakRef.TryGetTarget(out obj))
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
                LUA_TTABLE    => new LuaTable(state, _environment, tuple.@ref),
                LUA_TFUNCTION => new LuaFunction(state, _environment, tuple.@ref),
                _             => new LuaThread((lua_State*)ptr, _environment, tuple.@ref)  // Special case for threads
            };

            return Intern((IntPtr)ptr, tuple.@ref, obj);
        }

        public void Clean(lua_State* state)
        {
            var deadPtrs = new List<IntPtr>();
            foreach (var (ptr, (@ref, weakRef)) in _cache)
            {
                if (!weakRef.TryGetTarget(out _))
                {
                    luaL_unref(state, LUA_REGISTRYINDEX, @ref);
                    deadPtrs.Add(ptr);
                }
            }

            foreach (var deadPtr in deadPtrs)
            {
                _cache.Remove(deadPtr);
            }
        }
    }
}
