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
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Manages Lua references.
    /// </summary>
    internal sealed class LuaReferenceManager
    {
        // In order to properly manipulate a Lua reference from the CLR, we need to somehow store it within Lua. We also
        // need the references to be kept alive as long as the CLR has access to the reference. The natural solution is
        // to store the Lua reference in the Lua registry -- it can then be retrieved from the registry when needed, and
        // the reference will also be kept alive.
        //
        // If possible, Lua references should be cached. Additionally, they need to be cleaned up to prevent memory
        // leaks. This can be accomplished by running code whenever the Lua garbage collector runs using a metatable.
        //

        private readonly LuaEnvironment _environment;

        private readonly Dictionary<IntPtr, (int @ref, WeakReference<LuaReference> weakReference)> _cache;

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetatableRef;

        internal LuaReferenceManager(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            // Set up the cache of Lua references.
            //
            _cache = new Dictionary<IntPtr, (int @ref, WeakReference<LuaReference> weakReference)>();

            // Set up a metatable which will trigger `GcMetamethod` upon garbage collection, and create an empty table
            // for this purpose.
            //
            _gcMetamethod = GcMetamethod;

            lua_newtable(state);
            lua_newtable(state);
            lua_pushcfunction(state, _gcMetamethod);
            lua_setfield(state, -2, Strings.__gc);
            lua_pushvalue(state, -1);
            _gcMetatableRef = luaL_ref(state, LUA_REGISTRYINDEX);
            lua_setmetatable(state, -2);
            lua_pop(state, 1);
        }

        /// <summary>
        /// Converts the value on the stack into a Lua reference.
        /// </summary>
        /// <param name="state">The Lua state. </param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting Lua reference.</returns>
        public LuaReference ToLuaReference(IntPtr state, int index, LuaType type)
        {
            LuaReference? reference;

            var ptr = lua_topointer(state, index);
            if (_cache.TryGetValue(ptr, out var tuple))
            {
                if (tuple.weakReference.TryGetTarget(out reference))
                {
                    return reference;
                }
            }
            else
            {
                lua_pushvalue(state, index);
                tuple.@ref = luaL_ref(state, LUA_REGISTRYINDEX);
            }

            reference = type switch
            {
                LuaType.Table    => new LuaTable(state, _environment, tuple.@ref),
                LuaType.Function => new LuaFunction(state, _environment, tuple.@ref),
                _                => new LuaThread(ptr, _environment, tuple.@ref)
            };

            tuple.weakReference = new WeakReference<LuaReference>(reference);
            _cache[ptr] = tuple;
            return reference;
        }

        private int GcMetamethod(IntPtr state)
        {
            var deadPtrs = new List<IntPtr>();

            foreach (var (ptr, (@ref, weakReference)) in _cache)
            {
                if (!weakReference.TryGetTarget(out _))
                {
                    luaL_unref(state, LUA_REGISTRYINDEX, @ref);
                    deadPtrs.Add(ptr);
                }
            }

            foreach (var ptr in deadPtrs)
            {
                _cache.Remove(ptr);
            }

            // Create another empty table to trigger `GcMetamethod` upon garbage collection.
            //
            lua_newtable(state);
            lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetatableRef);
            lua_setmetatable(state, -1);
            lua_pop(state, 1);
            return 0;
        }
    }
}
