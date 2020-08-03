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

namespace Triton.Interop
{
    internal sealed class ClrEntityManager
    {
        private readonly ClrMetavalueGenerator _metavalueGenerator;

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetamethodReference;
        private readonly LuaCFunction _tostringMetamethod;
        private readonly int _tostringMetamethodReference;

        private readonly Dictionary<object, IntPtr> _entityPtrs;
        private readonly Dictionary<IntPtr, object> _entityCache;
        private readonly int _entityCacheReference;

        private readonly Dictionary<Type, int> _objectMetatableReferences;

        internal ClrEntityManager(IntPtr state, LuaEnvironment environment)
        {
            _metavalueGenerator = new ClrMetavalueGenerator(state, environment);

            // Set up the `__gc` and `__tostring` metamethods in the registry. This lowers the number of delegate
            // allocations since only one Lua object is created for each metamethod.
            //
            _gcMetamethod = GcMetamethod;
            lua_pushcfunction(state, _gcMetamethod);
            _gcMetamethodReference = luaL_ref(state, LUA_REGISTRYINDEX);

            _tostringMetamethod = ToStringMetamethod;
            lua_pushcfunction(state, _tostringMetamethod);
            _tostringMetamethodReference = luaL_ref(state, LUA_REGISTRYINDEX);

            // Set up the entity cache in the registry. This lowers the number of Lua object allocations, since the Lua
            // object is reused, if possible. The cache needs to have weak values, as otherwise the entities will never
            // get garbage collected!
            //
            lua_newtable(state);
            lua_newtable(state);
            lua_pushstring(state, "v");
            lua_setfield(state, -2, "__mode");
            lua_pushvalue(state, -1);
            lua_setmetatable(state, -2);

            _entityPtrs = new Dictionary<object, IntPtr>();
            _entityCache = new Dictionary<IntPtr, object>();
            _entityCacheReference = luaL_ref(state, LUA_REGISTRYINDEX);

            // Set up the object metatable cache. This lowers the number of metatable constructions, since the metatable
            // is reused, if possible.
            //
            _objectMetatableReferences = new Dictionary<Type, int>();
        }

        internal void Push(IntPtr state, object entity)
        {
            lua_rawgeti(state, LUA_REGISTRYINDEX, _entityCacheReference);
            PushEntityFromCache(state, entity);
            lua_remove(state, -2);  // Remove the entity cache from the stack

            void PushEntityFromCache(IntPtr state, object entity)
            {
                if (_entityPtrs.TryGetValue(entity, out var ptr))
                {
                    // Note that the value may have been garbage collected (meaning it is not in the cache) but the
                    // `__gc` metamethod may not have run (meaning `_entityPtrs` still contains the entity).
                    //
                    if (lua_rawgetp(state, -1, ptr) != LuaType.Nil)
                    {
                        return;
                    }

                    lua_pop(state, 1);  // Remove `nil` from the stack
                    Remove(ptr, entity);
                }

                ptr = lua_newuserdatauv(state, UIntPtr.Zero, 0);
                PushMetatable(state, entity);
                lua_setmetatable(state, -2);

                lua_pushvalue(state, -1);
                lua_rawsetp(state, -3, ptr);
                _entityCache.Add(ptr, entity);
                _entityPtrs.Add(entity, ptr);
            }

            void PushMetatable(IntPtr state, object entity)
            {
                switch (entity)
                {
                case ClrTypeProxy { Type: var type }:
                    _metavalueGenerator.PushTypeMetatable(state, type);
                    break;

                case ClrGenericTypesProxy { Types: var types }:
                    _metavalueGenerator.PushGenericTypesMetatable(state, types);
                    break;

                default:
                    var objType = entity.GetType();
                    if (_objectMetatableReferences.TryGetValue(objType, out var reference))
                    {
                        lua_rawgeti(state, LUA_REGISTRYINDEX, reference);
                        return;  // Return so that we don't set the `__gc` and `__tostring` metamethods again
                    }

                    _metavalueGenerator.PushObjectMetatable(state, objType);
                    lua_pushvalue(state, -1);
                    reference = luaL_ref(state, -1);
                    _objectMetatableReferences.Add(objType, reference);
                    break;
                }

                lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetamethodReference);
                lua_setfield(state, -2, "__gc");
                lua_rawgeti(state, LUA_REGISTRYINDEX, _tostringMetamethodReference);
                lua_setfield(state, -2, "__tostring");
            }
        }

        internal object ToClrEntity(IntPtr state, int index)
        {
            var ptr = lua_touserdata(state, index);
            return _entityCache[ptr];
        }

        internal void ToValue(IntPtr state, int index, out LuaValue value)
        {
            value = default;
            Unsafe.AsRef(in value._objectType) = ObjectType.ClrEntity;
            ref var entity = ref Unsafe.AsRef(in value._objectOrTag);

            var ptr = lua_touserdata(state, index);
            _ = _entityCache.TryGetValue(ptr, out entity);
        }

        private int GcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            if (_entityCache.TryGetValue(ptr, out var entity))
            {
                Remove(ptr, entity);
            }

            return 0;
        }

        private int ToStringMetamethod(IntPtr state)
        {
            lua_pushstring(state, ToClrEntity(state, 1).ToString());
            return 1;
        }

        private void Remove(IntPtr ptr, object entity)
        {
            _entityCache.Remove(ptr);
            _entityPtrs.Remove(entity);
        }
    }
}
