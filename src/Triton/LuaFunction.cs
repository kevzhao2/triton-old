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
using System.Diagnostics;
using Triton.Native;
using static Triton.Native.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua function.
    /// </summary>
    public sealed unsafe class LuaFunction : LuaObject
    {
        internal LuaFunction(LuaEnvironment environment, int reference, lua_State* state) :
            base(environment, reference, state)
        {
        }

        // TODO: consider optimization by adding generic overloads

        /// <summary>
        /// Calls the function with the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The function results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaEvaluationException">A Lua error occurred when evaluating the function.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object?[] Call(params object?[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _environment.ThrowIfDisposed();
            _environment.ThrowIfNotEnoughLuaStack(_state, 1 + args.Length);  // (1 + numArgs) stack slots required

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
            var stackDelta = 1;

            try
            {

                foreach (var arg in args)
                {
                    _environment.PushObject(_state, arg);
                    ++stackDelta;
                }
            }
            catch
            {
                lua_pop(_state, stackDelta);
                throw;
            }

            return CallInternal(args.Length);
        }

        private object?[] CallInternal(int numArgs)
        {
            Debug.Assert(numArgs >= 0);

            var oldTop = lua_gettop(_state) - numArgs - 1;
            var status = lua_pcall(_state, numArgs, -1, 0);
            if (status != LuaStatus.Ok)
            {
                throw _environment.CreateExceptionFromLuaStack<LuaEvaluationException>(_state);
            }

            var numResults = lua_gettop(_state) - oldTop;
            return _environment.MarshalResults(_state, numResults);
        }
    }
}
