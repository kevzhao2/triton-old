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
using System.Text;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment.
    /// </summary>
    public class LuaEnvironment : IDisposable
    {
        private readonly IntPtr _state;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            // Initialize the Lua state, opening the standard libraries for convenience.
            //
            _state = luaL_newstate();
            luaL_openlibs(_state);
        }

        // A finalizer for this class is infeasible. If the Lua environment is finalized during a Lua -> CLR transition,
        // the CLR -> Lua transition is impossible.
        //
        // As such, care must be taken to _never_ leak a Lua environment!!!
        //

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                lua_close(_state);

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Evaluates the given Lua chunk.
        /// </summary>
        /// <param name="chunk">The Lua chunk to evaluate.</param>
        public void Eval(string chunk)
        {

        }

        /// <summary>
        /// Converts the value on the stack into a Lua value.
        /// </summary>
        /// <param name="state">The Lua state. </param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal void ToValue(IntPtr state, int index, LuaType type, out LuaValue value)
        {
            switch (type)
            {
            default:
                LuaValue.FromNil(out value);
                break;

            case LuaType.Boolean:
                LuaValue.FromBoolean(lua_toboolean(state, index), out value);
                break;

            case LuaType.LightUserdata:
                LuaValue.FromLightUserdata(lua_touserdata(state, index), out value);
                break;

            case LuaType.Number:
                if (lua_isinteger(state, index))
                {
                    LuaValue.FromInteger(lua_tointeger(state, index), out value);
                }
                else
                {
                    LuaValue.FromNumber(lua_tonumber(state, index), out value);
                }
                break;

            case LuaType.String:
                unsafe
                {
                    UIntPtr length;
                    var ptr = lua_tolstring(state, index, (IntPtr)(&length));

                    LuaValue.FromString(Encoding.UTF8.GetString((byte*)ptr, (int)length), out value);
                    break;
                }
            }
        }

        private void LoadString(IntPtr state, string chunk)
        {

        }
    }
}
