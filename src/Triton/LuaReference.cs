// Copyright (c) 2018 Kevin Zhao
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
#if NETSTANDARD || NET40
using System.Dynamic;
#endif
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua reference which is stored in the registry of some <see cref="Triton.Lua"/> environment.
    /// </summary>
#if NETSTANDARD || NET40
    public abstract class LuaReference : DynamicObject {
#else
	public abstract class LuaReference {
#endif
        private readonly int _referenceId;

        internal LuaReference(Lua lua, int referenceId) {
            Lua = lua;
            _referenceId = referenceId;
        }
        
        internal Lua Lua { get; }

        internal void PushOnto(IntPtr state) {
            if (state != Lua.MainState && LuaApi.GetMainState(state) != Lua.MainState) {
                throw new ArgumentException("Reference cannot be pushed onto the given Lua environment.", nameof(state));
            }

            LuaApi.RawGetI(state, LuaApi.RegistryIndex, _referenceId);
        }
    }
}
