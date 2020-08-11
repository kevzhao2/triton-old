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

namespace Triton.Interop
{
    /// <summary>
    /// Manages CLR entities. Controls the lifetime of CLR entities and provides methods to manipulate and retrieve
    /// them.
    /// </summary>
    internal sealed class ClrEntityManager
    {
        private readonly ClrMetavalueGenerator _metavalueGenerator;

        private readonly Dictionary<object, IntPtr> _ptrs = new Dictionary<object, IntPtr>();
        private readonly Dictionary<IntPtr, object> _entities = new Dictionary<IntPtr, object>();
        private readonly Dictionary<Type, int> _objectMetatableRefs = new Dictionary<Type, int>();

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetamethodRef;

        private readonly LuaCFunction _tostringMetamethod;
        private readonly int _tostringMetamethodRef;

        private readonly int _entityCacheRef;

        internal ClrEntityManager(IntPtr state, LuaEnvironment environment)
        {
            _metavalueGenerator = new ClrMetavalueGenerator(state, environment);

            _gcMetamethod = GcMetamethod;  // Prevent garbage collection of the delegate
            lua_pushcfunction(state, _gcMetamethod);
            _gcMetamethodRef = luaL_ref(state, LUA_REGISTRYINDEX);

            _tostringMetamethod = ToStringMetamethod;  // Prevent garbage collection of the delegate
            lua_pushcfunction(state, _tostringMetamethod);
            _tostringMetamethodRef = luaL_ref(state, LUA_REGISTRYINDEX);

            lua_newtable(state);
            lua_newtable(state);
            lua_pushstring(state, Strings.v);
            lua_setfield(state, -2, Strings.__mode);
            lua_setmetatable(state, -2);
            _entityCacheRef = luaL_ref(state, LUA_REGISTRYINDEX);
        }

        /// <summary>
        /// Pushes the given CLR entity onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="entity">The CLR entity.</param>
        public void Push(IntPtr state, object entity)
        {
            lua_rawgeti(state, LUA_REGISTRYINDEX, _entityCacheRef);
            PushViaCache(state, entity);
            lua_remove(state, -2);  // Remove the entity cache from the stack
            return;

            void PushViaCache(IntPtr state, object entity)
            {
                if (_ptrs.TryGetValue(entity, out var ptr))
                {
                    if (lua_rawgetp(state, -1, ptr) != LuaType.Nil)
                    {
                        return;
                    }

                    lua_pop(state, 1);  // Remove nil from the stack

                    _ptrs.Remove(entity);
                    _entities.Remove(ptr);
                }

                ptr = lua_newuserdatauv(state, UIntPtr.Zero, 0);
                PushMetatable(state, entity);
                lua_setmetatable(state, -2);
                lua_pushvalue(state, -1);
                lua_rawsetp(state, -3, ptr);

                _ptrs.Add(entity, ptr);
                _entities.Add(ptr, entity);
            }

            void PushMetatable(IntPtr state, object entity)
            {
                switch (entity)
                {
                    case ProxyClrType { Type: var type }:
                        _metavalueGenerator.PushTypeMetatable(state, type);
                        break;

                    case ProxyGenericClrTypes { Types: var types }:
                        _metavalueGenerator.PushGenericTypesMetatable(state, types);
                        break;

                    default:
                        var objType = entity.GetType();
                        if (_objectMetatableRefs.TryGetValue(objType, out var @ref))
                        {
                            lua_rawgeti(state, LUA_REGISTRYINDEX, @ref);
                            return;  // Return so that we don't set the `__gc` and `__tostring` metamethods again
                        }

                        _metavalueGenerator.PushObjectMetatable(state, objType);
                        lua_pushvalue(state, -1);
                        @ref = luaL_ref(state, -1);

                        _objectMetatableRefs.Add(objType, @ref);
                        break;
                }

                lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetamethodRef);
                lua_setfield(state, -2, Strings.__gc);

                lua_rawgeti(state, LUA_REGISTRYINDEX, _tostringMetamethodRef);
                lua_setfield(state, -2, Strings.__tostring);
            }
        }

        /// <summary>
        /// Loads a CLR entity from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <returns>The resulting CLR entity.</returns>
        public object Load(IntPtr state, int index)
        {
            var ptr = lua_touserdata(state, index);
            return _entities[ptr];
        }

        private int GcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            if (_entities.Remove(ptr, out var entity))
            {
                _ptrs.Remove(entity);
            }

            return 0;
        }

        private int ToStringMetamethod(IntPtr state)
        {
            var entity = Load(state, 1);
            lua_pushstring(state, entity.ToString());
            return 1;
        }
    }
}
