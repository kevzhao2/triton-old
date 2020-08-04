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
using System.Text;
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment.
    /// </summary>
    public class LuaEnvironment : IDisposable
    {
        private readonly IntPtr _state;

        private readonly LuaReferenceManager _luaReferences;

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

            _luaReferences = new LuaReferenceManager(_state, this);
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
        /// Pushes the given Lua value onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The Lua value.</param>
        internal void PushValue(IntPtr state, in LuaValue value)
        {
            switch (value.GetObjectOrTag())
            {
            case null:
                lua_pushnil(state);
                break;

            case PrimitiveTag { PrimitiveType: var primitiveType }:
                switch (primitiveType)
                {
                case PrimitiveType.Boolean:
                    lua_pushboolean(state, value.AsBoolean());
                    break;

                case PrimitiveType.LightUserdata:
                    lua_pushlightuserdata(state, value.AsLightUserdata());
                    break;

                case PrimitiveType.Integer:
                    lua_pushinteger(state, value.AsInteger());
                    break;

                default:
                    lua_pushnumber(state, value.AsNumber());
                    break;
                }

                break;

            case { } obj:
                switch (value.GetObjectType())
                {
                case ObjectType.String:
                    throw new NotImplementedException();

                case ObjectType.LuaReference:
                    PushLuaReference(state, (LuaReference)obj);
                    break;

                default:
                    PushClrEntity(state, obj);
                    break;
                }

                break;
            }
        }

        /// <summary>
        /// Pushes the given Lua reference onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="reference">The Lua reference.</param>
        internal void PushLuaReference(IntPtr state, LuaReference reference) => reference.Push(state, this);

        /// <summary>
        /// Pushes the given CLR entity onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="entity">The CLR entity.</param>
        internal void PushClrEntity(IntPtr state, object entity)
        {
            Debug.Assert(entity is { });

            throw new NotImplementedException();
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
                FromNil(out value);
                break;

            case LuaType.Boolean:
                FromBoolean(lua_toboolean(state, index), out value);
                break;

            case LuaType.LightUserdata:
                FromLightUserdata(lua_touserdata(state, index), out value);
                break;

            case LuaType.Number:
                if (lua_isinteger(state, index))
                {
                    FromInteger(lua_tointeger(state, index), out value);
                }
                else
                {
                    FromNumber(lua_tonumber(state, index), out value);
                }
                break;

            case LuaType.String:
                FromString(lua_tostring(state, index), out value);
                break;

            case LuaType.Table:
            case LuaType.Function:
            case LuaType.Thread:
                FromLuaReference(ToLuaReference(state, index, type), out value);
                break;

            case LuaType.Userdata:
                FromClrEntity(ToClrEntity(state, index), out value);
                break;
            }
        }

        /// <summary>
        /// Converts the value on the stack into a Lua reference.
        /// </summary>
        /// <param name="state">The Lua state. </param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting Lua reference.</returns>
        internal LuaReference ToLuaReference(IntPtr state, int index, LuaType type) =>
            _luaReferences.ToLuaReference(state, index, type);

        /// <summary>
        /// Converts the value on the stack into a CLR entity.
        /// </summary>
        /// <param name="state">The Lua state. </param>
        /// <param name="index">The index.</param>
        /// <returns>The resulting CLR entity.</returns>
        internal object ToClrEntity(IntPtr state,int index)
        {
            throw new NotImplementedException();
        }

        private void LoadString(IntPtr state, string chunk)
        {

        }
    }
}
