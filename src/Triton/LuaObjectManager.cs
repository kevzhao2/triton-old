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
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton
{
    internal sealed class LuaObjectManager
    {
        private readonly LuaEnvironment _environment;

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetatableReference;

        private readonly Dictionary<IntPtr, (int reference, WeakReference<LuaObject> weakReference)> _objects;

        internal LuaObjectManager(IntPtr state, LuaEnvironment environment)
        {
            _environment = environment;

            // Set up a metatable with a `__gc` metamethod. This allows us to clean up dead Lua objects whenever a Lua
            // garbage collection occurs.
            //
            _gcMetamethod = GcMetamethod;

            lua_newtable(state);
            lua_newtable(state);
            lua_pushcfunction(state, _gcMetamethod);
            lua_setfield(state, -2, "__gc");
            lua_pushvalue(state, -1);
            _gcMetatableReference = luaL_ref(state, LUA_REGISTRYINDEX);
            lua_setmetatable(state, -2);
            lua_pop(state, 1);

            // Set up the cache of Lua objects. This lowers the number of CLR object allocations, since the CLR object
            // is reused, if possible. The cache needs to have weak values, as otherwise the objects will never get
            // garbage collected!
            //
            _objects = new Dictionary<IntPtr, (int, WeakReference<LuaObject>)>();
        }

        internal void Intern(IntPtr ptr, LuaObject obj)
        {
            _objects[ptr] = (obj._reference, new WeakReference<LuaObject>(obj));
        }

        internal void Push(IntPtr state, LuaObject obj)
        {
            if (obj._environment != _environment)
            {
                throw new InvalidOperationException("Lua object does not belong to the given environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, obj._reference);
        }

        internal void ToValue(IntPtr state, int index, out LuaValue value, LuaType type)
        {
            value = default;
            Unsafe.AsRef(in value._objectType) = ObjectType.LuaObject;
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

        private int GcMetamethod(IntPtr state)
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

            // Create a new table which triggers `GcMetamethod` upon being garbage collected.
            //
            lua_newtable(state);
            lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetatableReference);
            lua_setmetatable(state, -1);
            lua_pop(state, 1);
            return 0;
        }
    }
}
