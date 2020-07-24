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
using System.Runtime.InteropServices;
using static Triton.NativeMethods;

namespace Triton.Interop
{
    /// <summary>
    /// Manages CLR types and objects.
    /// </summary>
    internal sealed class ClrTypeObjectManager
    {
        private readonly Dictionary<IntPtr, Type> _types;
        private readonly Dictionary<IntPtr, object> _objects;

        private readonly ClrMetavalueGenerator _metavalueGenerator;
        private readonly int _typeMetatablesReference;
        private readonly int _objectMetatablesReference;
        private readonly Dictionary<Type, int> _typeMetatableKeys = new Dictionary<Type, int>();
        private readonly Dictionary<Type, int> _objectMetatableKeys = new Dictionary<Type, int>();

        private readonly lua_CFunction _typeGcMetamethod;
        private readonly lua_CFunction _objectGcMetamethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClrTypeObjectManager"/> class with the specified Lua
        /// <paramref name="state"/> and <paramref name="environment"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="environment">The Lua environment.</param>
        internal ClrTypeObjectManager(IntPtr state, LuaEnvironment environment)
        {
            _types = new Dictionary<IntPtr, Type>();
            _objects = new Dictionary<IntPtr, object>();
            
            _metavalueGenerator = new ClrMetavalueGenerator(environment);

            // To store the metatables for the CLR types and objects, we will use two Lua tables with integer keys and
            // metatable values.
            //
            lua_newtable(state);
            _typeMetatablesReference = luaL_ref(state, LUA_REGISTRYINDEX);
            lua_newtable(state);
            _objectMetatablesReference = luaL_ref(state, LUA_REGISTRYINDEX);

            _typeGcMetamethod = TypeGcMetamethod;
            _objectGcMetamethod = ObjectGcMetamethod;
        }

        /// <summary>
        /// Pushes the given CLR <paramref name="type"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        internal void PushClrType(IntPtr state, Type type)
        {
            var ptr = PushClrTypeOrObject(state, type);
            PushClrTypeOrObjectMetatable(state, type, isType: true);
            lua_setmetatable(state, -2);
            _types[ptr] = type;
        }

        /// <summary>
        /// Pushes the given CLR <paramref name="obj"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The CLR object.</param>
        internal void PushClrObject(IntPtr state, object obj)
        {
            var ptr = PushClrTypeOrObject(state, obj);
            PushClrTypeOrObjectMetatable(state, obj.GetType(), isType: false);
            lua_setmetatable(state, -2);
            _objects[ptr] = obj;
        }

        /// <summary>
        /// Converts the CLR type or object on the stack of the Lua <paramref name="state"/> at the given
        /// <paramref name="index"/> to a Lua <paramref name="value"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the CLR type or object on the stack.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal void ToClrTypeOrObject(IntPtr state, int index, out LuaValue value)
        {
            value = default;
            ref var typeOrObj = ref Unsafe.AsRef(in value._objectOrTag);

            var ptr = lua_touserdata(state, index);
            if (_types.TryGetValue(ptr, out Unsafe.As<object?, Type>(ref typeOrObj)))
            {
                Unsafe.AsRef(in value._integer) = 3;
            }
            else if (_objects.TryGetValue(ptr, out Unsafe.As<object?, object>(ref typeOrObj)))
            {
                Unsafe.AsRef(in value._integer) = 4;
            }
        }

        private int TypeGcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            _types.Remove(ptr);

            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            handle.Free();
            return 0;
        }

        private int ObjectGcMetamethod(IntPtr state)
        {
            var ptr = lua_touserdata(state, 1);
            _objects.Remove(ptr);

            var handle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(ptr));
            handle.Free();
            return 0;
        }

        private IntPtr PushClrTypeOrObject(IntPtr state, object typeOrObj)
        {
            var handle = GCHandle.Alloc(typeOrObj);
            var ptr = lua_newuserdatauv(state, (UIntPtr)IntPtr.Size, 0);
            Marshal.WriteIntPtr(ptr, GCHandle.ToIntPtr(handle));
            return ptr;
        }

        private void PushClrTypeOrObjectMetatable(IntPtr state, Type typeOrObjType, bool isType)
        {
            var tableReference = isType ? _typeMetatablesReference : _objectMetatablesReference;
            var tableKeys = isType ? _typeMetatableKeys : _objectMetatableKeys;

            // If the metatable is cached, then retrieve it. Otherwise, generate it and then cache it.
            if (tableKeys.TryGetValue(typeOrObjType, out var key))
            {
                lua_rawgeti(state, LUA_REGISTRYINDEX, tableReference);
                lua_rawgeti(state, -1, key);
                lua_remove(state, -2);  // Remove the metatable table from the stack
            }
            else
            {
                if (isType)
                {
                    GenerateClrTypeMetatable(state, typeOrObjType);
                }
                else
                {
                    GenerateClrObjectMetatable(state, typeOrObjType);
                }

                key = tableKeys.Count + 1;  // Sequentially generate the keys to reduce memory usage
                lua_rawgeti(state, LUA_REGISTRYINDEX, tableReference);
                lua_pushvalue(state, -2);
                lua_rawseti(state, -2, key);
                lua_pop(state, 1);  // Pop the metatable table from the stack

                tableKeys[typeOrObjType] = key;
            }
        }

        private void GenerateClrTypeMetatable(IntPtr state, Type type)
        {
            lua_newtable(state);

            lua_pushcfunction(state, _typeGcMetamethod);
            lua_setfield(state, -2, "__gc");

            _metavalueGenerator.PushToString(state);
            lua_setfield(state, -2, "__tostring");

            _metavalueGenerator.PushTypeIndex(state, type);
            lua_setfield(state, -2, "__index");
        }

        private void GenerateClrObjectMetatable(IntPtr state, Type objType)
        {
            lua_newtable(state);

            lua_pushcfunction(state, _objectGcMetamethod);
            lua_setfield(state, -2, "__gc");

            _metavalueGenerator.PushToString(state);
            lua_setfield(state, -2, "__tostring");

            _metavalueGenerator.PushObjectIndex(state, objType);
            lua_setfield(state, -2, "__index");
        }
    }
}
