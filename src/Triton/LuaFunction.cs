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
    /// Represents a Lua function.
    /// </summary>
    public class LuaFunction : LuaReference {
        internal LuaFunction(IntPtr state, int reference) : base(state, reference) {
        }

        /// <summary>
        /// Calls the function with given arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="LuaException">A Lua error occurs.</exception>
        public object[] Call(params object[] args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }

            // Ensure that we have enough stack space for the function currently on the stack and its arguments.
            if (args.Length + 1 >= LuaApi.MinStackSize && !LuaApi.CheckStack(State, args.Length + 1)) {
                throw new LuaException("Not enough stack space for function and arguments.");
            }

            try {
                LuaApi.RawGetI(State, LuaApi.RegistryIndex, Reference);
                foreach (var arg in args) {
                    LuaApi.PushObject(State, arg);
                }
                
                if (LuaApi.PCallK(State, args.Length) != LuaStatus.Ok) {
                    var errorMessage = LuaApi.ToString(State, -1);
                    throw new LuaException(errorMessage);
                }

                // Ensure that we have enough stack space for GetObjects.
                var numResults = LuaApi.GetTop(State);
                if (numResults >= LuaApi.MinStackSize && !LuaApi.CheckStack(State, 1)) {
                    throw new LuaException("Not enough scratch stack space.");
                }

                return LuaApi.ToObjects(State, 1, numResults);
            } finally {
                LuaApi.SetTop(State, 0);
            }
        }
    }
}
