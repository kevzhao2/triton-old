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

using System.Collections.Generic;
using System.Linq;
using Triton.Interop.Emit;
using static Triton.Lua;

namespace Triton.Interop
{
    /// <summary>
    /// Generates metatables for CLR entities.
    /// </summary>
    internal sealed unsafe class MetatableGenerator
    {
        private static readonly List<IMetavalueGenerator> _metavalueGenerators = new()
        {
            new GcMetamethodGenerator(),
            new TostringMetamethodGenerator(),
            new IndexMetavalueGenerator(),
            new NewIndexMetamethodGenerator()
        };

        private readonly Dictionary<object, int> _cachedMetatableRefs = new();

        public void Push(lua_State* state, object entity, bool isTypes)
        {
            var key = isTypes ? entity : entity.GetType();
            if (_cachedMetatableRefs.TryGetValue(key, out var metatableRef))
            {
                lua_rawgeti(state, LUA_REGISTRYINDEX, metatableRef);
                return;
            }

            Generate();
            Cache();
            return;

            void Generate()
            {
                var applicableGenerators = _metavalueGenerators.Where(g => g.IsApplicable(entity, isTypes)).ToList();

                lua_createtable(state, 0, applicableGenerators.Count);
                foreach (var generator in applicableGenerators)
                {
                    generator.Push(state, entity, isTypes);
                    lua_setfield(state, -2, generator.Name);
                }
            }

            void Cache()
            {
                lua_pushvalue(state, -1);
                var metatableRef = luaL_ref(state, LUA_REGISTRYINDEX);
                _cachedMetatableRefs.Add(key, metatableRef);
            }
        }
    }
}
