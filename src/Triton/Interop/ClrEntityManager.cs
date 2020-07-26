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
    // Manages CLR entities: types, generic types, and objects.
    //
    internal sealed class ClrEntityManager
    {
        private const string GcMetamethodField = "<>__gc";
        private const string ToStringMetamethodField = "<>__tostring";
        private const string EntityCacheField = "<>__entityCache";

        private readonly ClrMetavalueGenerator _metavalueGenerator;
        private readonly lua_CFunction _gcMetamethod;
        private readonly lua_CFunction _tostringMetamethod;
        private readonly Dictionary<IntPtr, object> _entities;
        private readonly Dictionary<object, IntPtr> _entityPtrs;

        internal ClrEntityManager(IntPtr state, LuaEnvironment environment)
        {
            _metavalueGenerator = new ClrMetavalueGenerator(environment);

            // Set up the `__gc` and `__tostring` metamethods in the registry to lower the number of allocations.
            //
            _gcMetamethod = GcMetamethod;
            lua_pushcfunction(state, _gcMetamethod);
            lua_setfield(state, LUA_REGISTRYINDEX, GcMetamethodField);

            _tostringMetamethod = ToStringMetamethod;
            lua_pushcfunction(state, _tostringMetamethod);
            lua_setfield(state, LUA_REGISTRYINDEX, ToStringMetamethodField);

            // Set up the entity cache in the registry to lower the number of allocations. The caches need to have weak
            // values, as otherwise the entities will never get garbage collected.
            //
            lua_newtable(state);
            lua_newtable(state);
            lua_pushstring(state, "v");
            lua_setfield(state, -2, "__mode");
            lua_pushvalue(state, -1);
            lua_setmetatable(state, -2);
            lua_setfield(state, LUA_REGISTRYINDEX, EntityCacheField);

            _entities = new Dictionary<IntPtr, object>();
            _entityPtrs = new Dictionary<object, IntPtr>();
        }

        internal void Push(IntPtr state, object entity)
        {
            lua_getfield(state, LUA_REGISTRYINDEX, EntityCacheField);

            if (_entityPtrs.TryGetValue(entity, out var ptr))
            {
                // Note that the value might have been garbage collected, but the `__gc` metamethod might not have run
                // yet. We need to account for this here!
                //
                if (lua_rawgetp(state, -1, ptr) != LuaType.Nil)
                {
                    lua_remove(state, -2);  // Remove the entity cache from the stack
                    return;
                }

                lua_pop(state, 1);  // Remove nil from the stack
                Remove(ptr, entity);  // Remove the entity from the cache since the pointer has been invalidated
            }

            ptr = lua_newuserdatauv(state, UIntPtr.Zero, 0);
            PushMetatable(state, entity);
            lua_setmetatable(state, -2);
            lua_pushvalue(state, -1);
            lua_rawsetp(state, -3, ptr);

            _entityPtrs.Add(entity, ptr);
            _entities.Add(ptr, entity);

            lua_remove(state, -2);  // Remove the entity cache from the stack
        }

        internal object ToClrEntity(IntPtr state, int index)
        {
            var ptr = lua_touserdata(state, index);
            return _entities[ptr];
        }

        internal void ToValue(IntPtr state, int index, out LuaValue value)
        {
            value = default;
            ref var entity = ref Unsafe.AsRef(in value._objectOrTag);

            var ptr = lua_touserdata(state, index);
            _ = _entities.TryGetValue(ptr, out entity);
        }

        private void PushMetatable(IntPtr state, object entity)
        {
            if (entity is ClrTypeProxy { Type: var type })
            {
                PushTypeMetatable(state, type);
            }
            else if (entity is ClrGenericTypesProxy { Types: var types })
            {
                PushGenericTypesMetatable(state, types);
            }
            else
            {
                PushObjectMetatable(state, entity);
            }

            void PushTypeMetatable(IntPtr state, Type type)
            {
                lua_newtable(state);
                lua_getfield(state, LUA_REGISTRYINDEX, GcMetamethodField);
                lua_setfield(state, -2, "__gc");
                lua_getfield(state, LUA_REGISTRYINDEX, ToStringMetamethodField);
                lua_setfield(state, -2, "__tostring");
                _metavalueGenerator.PushTypeIndex(state, type);
                lua_setfield(state, -2, "__index");
            }

            void PushGenericTypesMetatable(IntPtr state, Type[] types)
            {
                lua_newtable(state);
                lua_getfield(state, LUA_REGISTRYINDEX, GcMetamethodField);
                lua_setfield(state, -2, "__gc");
                lua_getfield(state, LUA_REGISTRYINDEX, ToStringMetamethodField);
                lua_setfield(state, -2, "__tostring");

                throw new NotImplementedException();
            }

            void PushObjectMetatable(IntPtr state, object entity)
            {
                throw new NotImplementedException();
            }
        }

        private int GcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            if (_entities.TryGetValue(ptr, out var entity))
            {
                Remove(ptr, entity);
            }

            return 0;
        }

        private int ToStringMetamethod(IntPtr state)
        {
            var entity = ToClrEntity(state, 1);

            try
            {
                lua_pushstring(state, entity.ToString());
                return 1;
            }
            catch (Exception ex)
            {
                return luaL_error(state, $"unhandled CLR exception:\n{ex}");
            }
        }

        private void Remove(IntPtr ptr, object entity)
        {
            _entities.Remove(ptr);
            _entityPtrs.Remove(entity);
        }
    }
}
