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
using System.Diagnostics.CodeAnalysis;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a Lua function.
    /// </summary>
    public sealed class LuaFunction : LuaObject
    {
        internal LuaFunction(IntPtr state, LuaEnvironment environment, int reference) :
            base(state, environment, reference)
        {
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string ToString() => $"Lua function: {_reference}";

        /// <summary>
        /// Calls the Lua function with no arguments.
        /// </summary>
        /// <returns>The results.</returns>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the function.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Call()
        {
            CallPrologue();  // Performs validation
            return _environment.Call(_state, 0);
        }

        /// <summary>
        /// Calls the Lua function with a single argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the function.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Call(in LuaValue arg)
        {
            CallPrologue();  // Performs validation.
            _environment.PushValue(_state, arg);
            return _environment.Call(_state, 1);
        }

        /// <summary>
        /// Calls the Lua function with two arguments.
        /// </summary>
        /// <param name="arg">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the function.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Call(in LuaValue arg, in LuaValue arg2)
        {
            CallPrologue();  // Performs validation
            _environment.PushValue(_state, arg);
            _environment.PushValue(_state, arg2);
            return _environment.Call(_state, 2);
        }

        /// <summary>
        /// Calls the Lua function with three arguments.
        /// </summary>
        /// <param name="arg">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The results.</returns>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the function.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Call(in LuaValue arg, in LuaValue arg2, in LuaValue arg3)
        {
            CallPrologue();  // Performs validation
            _environment.PushValue(_state, arg);
            _environment.PushValue(_state, arg2);
            _environment.PushValue(_state, arg3);
            return _environment.Call(_state, 3);
        }

        /// <summary>
        /// Calls the Lua function with the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the function.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Call(params LuaValue[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            CallPrologue();  // Performs validation
            for (var i = 0; i < args.Length; ++i)
            {
                _environment.PushValue(_state, args[i]);
            }
            return _environment.Call(_state, args.Length);
        }

        private void CallPrologue()
        {
            _environment.ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_rawgeti(_state, LUA_REGISTRYINDEX, _reference);
        }
    }
}
