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
    /// Generates metatables for CLR objects and types.
    /// </summary>
    internal sealed class MetatableGenerator
    {
        private readonly Dictionary<Type, int> _cachedObjectMetatableRefs = new();
        private readonly Dictionary<HashSet<Type>, int> _cachedTypesMetatableRefs = new(HashSet<Type>.CreateSetComparer());

        public unsafe void Push(lua_State* state, object obj)
        {
            var type = obj.GetType();
            if (_cachedObjectMetatableRefs.TryGetValue(type, out var objectMetatableRef))
            {
                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, objectMetatableRef);
                return;
            }

            lua_newtable(state);

            lua_pushcfunction(state, &GcMetamethod);
            lua_setfield(state, -2, "__gc");

            lua_pushcfunction(state, &TostringMetamethod);
            lua_setfield(state, -2, "__tostring");

            // Cache the newly-generated metatable in the Lua registry.
            //
            lua_pushvalue(state, -1);
            _cachedObjectMetatableRefs.Add(type, luaL_ref(state, LUA_REGISTRYINDEX));
            return;

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static int GcMetamethod(lua_State* state)
            {
                var handle = GCHandle.FromIntPtr(*(IntPtr*)lua_touserdata(state, 1));
                handle.Free();

                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static int TostringMetamethod(lua_State* state)
            {
                var target = GCHandle.FromIntPtr(*(IntPtr*)lua_touserdata(state, 1)).Target!;
                lua_pushstring(state, $"CLR object: {target}");

                return 1;
            }
        }

        public unsafe void Push(lua_State* state, Type[] types)
        {
            var typesSet = new HashSet<Type>(types);
            if (_cachedTypesMetatableRefs.TryGetValue(typesSet, out var typesMetatableRef))
            {
                _ = lua_rawgeti(state, LUA_REGISTRYINDEX, typesMetatableRef);
                return;
            }

            lua_newtable(state);

            lua_pushcfunction(state, &GcMetamethod);
            lua_setfield(state, -2, "__gc");

            lua_pushcfunction(state, &TostringMetamethod);
            lua_setfield(state, -2, "__tostring");

            // Cache the newly-generated metatable in the Lua registry.
            //
            lua_pushvalue(state, -1);
            _cachedTypesMetatableRefs.Add(typesSet, luaL_ref(state, LUA_REGISTRYINDEX));

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static int GcMetamethod(lua_State* state)
            {
                var handle = GCHandle.FromIntPtr(*(nint*)lua_touserdata(state, 1) & ~1);
                handle.Free();

                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            static int TostringMetamethod(lua_State* state)
            {
                var target = GCHandle.FromIntPtr(*(nint*)lua_touserdata(state, 1) & ~1).Target!;
                var types = Unsafe.As<object, Type[]>(ref target);
                lua_pushstring(state, types.Length == 1 ?
                    $"CLR type: {types[0]}" :
                    $"CLR types: ({string.Join<Type>(", ", types)})");

                return 1;
            }
        }
    }
}
