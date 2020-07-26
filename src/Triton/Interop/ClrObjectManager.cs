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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    // Manages CLR objects.
    //
    internal sealed class ClrObjectManager
    {
        private const string GcMetamethodField = "<>__gc";
        private const string ToStringMetamethodField = "<>__tostring";
        private const string ObjectCacheField = "<>__objectCache";

        private readonly ClrMetavalueGenerator _metavalueGenerator;
        private readonly lua_CFunction _gcMetamethod;
        private readonly lua_CFunction _tostringMetamethod;
        private readonly Dictionary<IntPtr, object> _objects;
        private readonly Dictionary<object, IntPtr> _objectPtrs;

        internal ClrObjectManager(IntPtr state, LuaEnvironment environment)
        {
            _metavalueGenerator = new ClrMetavalueGenerator(environment);

            // Set up the `__gc` and `__tostring` metamethods in the registry to lower the amount of allocations.
            //
            _gcMetamethod = GcMetamethod;
            lua_pushcfunction(state, _gcMetamethod);
            lua_setfield(state, LUA_REGISTRYINDEX, GcMetamethodField);

            _tostringMetamethod = ToStringMetamethod;
            lua_pushcfunction(state, _tostringMetamethod);
            lua_setfield(state, LUA_REGISTRYINDEX, ToStringMetamethodField);

            // Set up the object cache in the registry to lower the amount of allocations. The caches need to have weak
            // values, as otherwise the objects will never get garbage collected.
            //
            lua_newtable(state);
            lua_newtable(state);
            lua_pushstring(state, "v");
            lua_setfield(state, -2, "__mode");
            lua_pushvalue(state, -1);
            lua_setmetatable(state, -2);
            lua_setfield(state, LUA_REGISTRYINDEX, ObjectCacheField);

            _objects = new Dictionary<IntPtr, object>();
            _objectPtrs = new Dictionary<object, IntPtr>();
        }

        internal void Push(IntPtr state, object obj)
        {
            lua_getfield(state, LUA_REGISTRYINDEX, ObjectCacheField);

            if (_objectPtrs.TryGetValue(obj, out var ptr))
            {
                // Note that the value might have been garbage collected, but the `__gc` metamethod might not have run
                // yet. We need to account for this here!
                //
                if (lua_rawgetp(state, -1, ptr) != LuaType.Nil)
                {
                    lua_remove(state, -2);  // Remove the object cache from the stack
                    return;
                }

                lua_pop(state, 1);  // Remove nil from the stack
                RemoveObject(obj, ptr);  // Remove the object since the pointer has been invalidated
            }

            ptr = lua_newuserdatauv(state, UIntPtr.Zero, 0);
            PushMetatable(state, obj);
            lua_setmetatable(state, -2);
            lua_pushvalue(state, -1);
            lua_rawsetp(state, -3, ptr);

            _objectPtrs.Add(obj, ptr);
            _objects.Add(ptr, obj);

            lua_remove(state, -2);  // Remove the object cache from the stack
        }

        internal object ToClrObject(IntPtr state, int index)
        {
            var ptr = lua_touserdata(state, index);
            return _objects[ptr];
        }

        internal void ToValue(IntPtr state, int index, out LuaValue value)
        {
            value = default;
            ref var obj = ref Unsafe.AsRef(in value._objectOrTag);

            var ptr = lua_touserdata(state, index);
            if (_objects.TryGetValue(ptr, out obj))
            {
                Unsafe.AsRef(in value._integer) = 3;
            }
        }

        private void PushMetatable(IntPtr state, object obj)
        {
            if (obj is LuaValue.ClrTypeProxy { Type: var type })
            {
                PushTypeMetatable(state, type);
            }
            else if (obj is LuaValue.ClrGenericTypesProxy { Types: var types })
            {
                PushGenericTypeMetatable(state, types);
            }
            else
            {
                PushObjectMetatable(state, obj);
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

            void PushGenericTypeMetatable(IntPtr state, Type[] types)
            {
                lua_newtable(state);
                lua_getfield(state, LUA_REGISTRYINDEX, GcMetamethodField);
                lua_setfield(state, -2, "__gc");
                lua_getfield(state, LUA_REGISTRYINDEX, ToStringMetamethodField);
                lua_setfield(state, -2, "__tostring");

                throw new NotImplementedException();
            }

            void PushObjectMetatable(IntPtr state, object obj)
            {
                throw new NotImplementedException();
            }
        }

        private int GcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            RemoveObject(ptr);
            return 0;
        }

        private int ToStringMetamethod(IntPtr state)
        {
            var obj = ToClrObject(state, 1);

            try
            {
                lua_pushstring(state, obj.ToString());
                return 1;
            }
            catch (Exception ex)
            {
                return luaL_error(state, $"unhandled CLR exception:\n{ex}");
            }
        }

        private void RemoveObject(IntPtr ptr)
        {
            if (_objects.TryGetValue(ptr, out var obj))
            {
                RemoveObject(obj, ptr);
            }
        }

        private void RemoveObject(object obj, IntPtr ptr)
        {
            _objects.Remove(ptr);
            _objectPtrs.Remove(obj);
        }
    }
}
