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

namespace Triton {
    /// <summary>
    /// Represents a Lua function that may be called.
    /// </summary>
    public sealed class LuaFunction : LuaReference {
        internal LuaFunction(Lua lua, int referenceId) : base(lua, referenceId) {
        }

        /// <summary>
        /// Calls the <see cref="LuaFunction"/> with the given arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentException">
        /// One of the supplied arguments is a <see cref="LuaReference"/> which is tied to a different <see cref="Lua"/> environment.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        public object[] Call(params object[] args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }

            PushOnto(Lua.MainState);
            return Lua.Call(args);
        }

#if NETSTANDARD || NET40
        /// <inheritdoc/>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result) {
            result = Call(args);
            return true;
        }
#endif
    }
}
