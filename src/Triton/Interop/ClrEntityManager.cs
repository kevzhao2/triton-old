// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Manages CLR entities. Controls the lifetimes of CLR entities and provides methods to manipulate and retrieve
    /// them.
    /// </summary>
    internal sealed class ClrEntityManager
    {
        /*private readonly ClrMetavalueGenerator _metavalueGenerator;

        // We would like to use the same userdata instances for the same CLR entities. This is accomplished by storing
        // them inside of a table within the Lua registry and using the following entity caches.

        private readonly Dictionary<object, IntPtr> _ptrs = new Dictionary<object, IntPtr>();
        private readonly Dictionary<IntPtr, object> _entities = new Dictionary<IntPtr, object>();
        private readonly int _entityCacheRef;

        // Cache the metatables for CLR objects, since generating a metatable is a _very_ expensive operation.

        private readonly Dictionary<Type, int> _objectMetatableRefs = new Dictionary<Type, int>();

        // Cache the `__gc` and `__tostring` metamethods to reduce the number of delegate allocations.

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetamethodRef;
        private readonly LuaCFunction _tostringMetamethod;
        private readonly int _tostringMetamethodRef;

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
            lua_createtable(state, 0, 1);
            lua_pushstring(state, "v");
            lua_setfield(state, -2, "__mode");
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
                        @ref = luaL_ref(state, LUA_REGISTRYINDEX);

                        _objectMetatableRefs.Add(objType, @ref);
                        break;
                }

                lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetamethodRef);
                lua_setfield(state, -2, "__gc");

                lua_rawgeti(state, LUA_REGISTRYINDEX, _tostringMetamethodRef);
                lua_setfield(state, -2, "__tostring");
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
        }*/

        private readonly ClrMetavalueGenerator _metavalueGenerator;

        // Cache the metatables for CLR objects, since generating a metatable is a _very_ expensive operation.

        private readonly Dictionary<Type, int> _objectMetatableRefs = new Dictionary<Type, int>();

        // Cache the `__gc` and `__tostring` metamethods to reduce the number of delegate allocations.

        private readonly LuaCFunction _gcMetamethod;
        private readonly int _gcMetamethodRef;
        private readonly LuaCFunction _tostringMetamethod;
        private readonly int _tostringMetamethodRef;

        internal ClrEntityManager(IntPtr state, LuaEnvironment environment)
        {
            _metavalueGenerator = new ClrMetavalueGenerator(state, environment);

            _gcMetamethod = GcMetamethod;  // Prevent garbage collection of the delegate
            lua_pushcfunction(state, _gcMetamethod);
            _gcMetamethodRef = luaL_ref(state, LUA_REGISTRYINDEX);

            _tostringMetamethod = ToStringMetamethod;  // Prevent garbage collection of the delegate
            lua_pushcfunction(state, _tostringMetamethod);
            _tostringMetamethodRef = luaL_ref(state, LUA_REGISTRYINDEX);
        }

        /// <summary>
        /// Pushes the given CLR entity onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="entity">The CLR entity.</param>
        public void Push(IntPtr state, object entity)
        {
            var handle = GCHandle.Alloc(entity);
            var ptr = lua_newuserdatauv(state, (UIntPtr)IntPtr.Size, 0);
            Marshal.WriteIntPtr(ptr, GCHandle.ToIntPtr(handle));
            PushMetatable(state, entity);
            lua_setmetatable(state, -2);
            return;

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
                        @ref = luaL_ref(state, LUA_REGISTRYINDEX);

                        _objectMetatableRefs.Add(objType, @ref);
                        break;
                }

                lua_rawgeti(state, LUA_REGISTRYINDEX, _gcMetamethodRef);
                lua_setfield(state, -2, "__gc");

                lua_rawgeti(state, LUA_REGISTRYINDEX, _tostringMetamethodRef);
                lua_setfield(state, -2, "__tostring");
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
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            return handle.Target!;
        }

        private int GcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            handle.Free();

            return 0;
        }

        private int ToStringMetamethod(IntPtr state)
        {
            var entity = Load(state, 1);
            lua_pushstring(state, entity.ToString() ?? string.Empty);
            return 1;
        }
    }
}
