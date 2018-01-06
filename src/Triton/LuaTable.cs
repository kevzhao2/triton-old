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
using Triton.Interop;

namespace Triton {
    /// <summary>
    /// Represents a Lua table.
    /// </summary>
    public sealed class LuaTable : LuaReference {
        internal LuaTable(Lua lua, IntPtr state, int reference, IntPtr pointer) : base(lua, state, reference, pointer) {
        }

        /// <summary>
        /// Gets or sets the value associated with the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value, or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c> and is being set.</exception>
        public object this[object key] {
            get {
                try {
                    PushTo(State);
                    Lua.PushObject(key);
                    var type = LuaApi.GetTable(State, -2);
                    return Lua.GetObject(-1, type);
                } finally {
                    LuaApi.SetTop(State, 0);
                }
            }
            set {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }

                try {
                    PushTo(State);
                    Lua.PushObject(key);
                    Lua.PushObject(value);
                    LuaApi.SetTable(State, -3);
                } finally {
                    LuaApi.SetTop(State, 0);
                }
            }
        }
    }
}
