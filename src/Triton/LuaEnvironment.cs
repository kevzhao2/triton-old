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
using System.Runtime.CompilerServices;
using Triton.Interop;
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
        private readonly LuaObjectManager _luaObjects;
        private readonly ClrEntityManager _clrEntities;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);  // Open all standard libraries by default for convenience

            _luaObjects = new LuaObjectManager(_state, this);
            _clrEntities = new ClrEntityManager(_state, this);
        }

        // A finalizer is _NOT_ feasible here. If the finalizer calls `lua_close` during an unmanaged -> managed
        // transition (which is possible since finalizers run on a separate thread), we'll never be able to make the
        // managed -> unmanaged transition successfully.
        //
        // Thus, you must ensure that `LuaEnvironment` instances are properly disposed of!
        //

        /// <summary>
        /// Gets or sets the value of the given <paramref name="global"/>.
        /// </summary>
        /// <param name="global">The global.</param>
        /// <returns>The value of the given <paramref name="global"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="global"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaValue this[string global]
        {
            get
            {
                if (global is null)
                {
                    throw new ArgumentNullException(nameof(global));
                }

                ThrowIfDisposed();

                lua_settop(_state, 0);  // Reset stack

                var type = lua_getglobal(_state, global);
                return ToValue(_state, -1, type);
            }

            set
            {
                if (global is null)
                {
                    throw new ArgumentNullException(nameof(global));
                }

                ThrowIfDisposed();

                lua_settop(_state, 0);  // Reset stack

                PushValue(_state, value);
                lua_setglobal(_state, global);
            }
        }

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
        /// Creates a Lua table with the given initial capacities.
        /// </summary>
        /// <param name="sequentialCapacity">The initial sequential capacity for the table.</param>
        /// <param name="nonSequentialCapacity">The initial non-sequential capacity for the table.</param>
        /// <returns>The resulting Lua table.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sequentialCapacity"/> or <paramref name="nonSequentialCapacity"/> are negative.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaTable CreateTable(int sequentialCapacity = 0, int nonSequentialCapacity = 0)
        {
            if (sequentialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sequentialCapacity), "Sequential capacity is negative");
            }

            if (nonSequentialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nonSequentialCapacity), "Non-sequential capacity is negative");
            }

            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            lua_createtable(_state, sequentialCapacity, nonSequentialCapacity);

            var ptr = lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var table = new LuaTable(_state, this, reference);

            _luaObjects.Intern(ptr, table);
            return table;
        }

        /// <summary>
        /// Creates a Lua function from loading the given Lua <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The Lua chunk to load.</param>
        /// <returns>The resulting Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaFunction CreateFunction(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            ThrowIfDisposed();

            LoadString(_state, chunk);

            var ptr = lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(_state, this, reference);

            _luaObjects.Intern(ptr, function);
            return function;
        }

        /// <summary>
        /// Creates a Lua thread.
        /// </summary>
        /// <returns>The resulting Lua thread.</returns>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaThread CreateThread()
        {
            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            var ptr = lua_newthread(_state);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var thread = new LuaThread(ptr, this, reference);

            _luaObjects.Intern(ptr, thread);
            return thread;
        }

        /// <summary>
        /// Evaluates the given Lua <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The Lua chunk to evaluate.</param>
        /// <returns>The results.</returns>
        /// <exception cref="LuaEvalException">A Lua error occurred when evaluating the chunk.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Eval(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            ThrowIfDisposed();

            LoadString(_state, chunk);  // Performs validation
            return Call(_state, 0);
        }

        internal void PushObject(IntPtr state, object? obj)
        {
            if (obj is null)                  lua_pushnil(state);
            else if (obj is bool b)           lua_pushboolean(state, b);
            else if (obj is IntPtr p)         lua_pushlightuserdata(state, p);
            else if (obj is sbyte i1)         lua_pushinteger(state, i1);
            else if (obj is byte u1)          lua_pushinteger(state, u1);
            else if (obj is short i2)         lua_pushinteger(state, i2);
            else if (obj is ushort u2)        lua_pushinteger(state, u2);
            else if (obj is int i4)           lua_pushinteger(state, i4);
            else if (obj is uint u4)          lua_pushinteger(state, u4);
            else if (obj is long i8)          lua_pushinteger(state, i8);
            else if (obj is ulong u8)         lua_pushinteger(state, (long)u8);
            else if (obj is float r4)         lua_pushnumber(state, r4);
            else if (obj is double r8)        lua_pushnumber(state, r8);
            else if (obj is string s)         lua_pushstring(state, s);
            else if (obj is LuaObject luaObj) PushLuaObject(state, luaObj);
            else                              PushClrEntity(state, obj);
        }

        internal void PushLuaObject(IntPtr state, LuaObject obj) => _luaObjects.Push(state, obj);

        internal void PushClrEntity(IntPtr state, object entity) => _clrEntities.Push(state, entity);

        internal void PushValue(IntPtr state, in LuaValue value)
        {
            var objectOrTag = value._objectOrTag;
            if (objectOrTag is null)
            {
                lua_pushnil(state);
            }
            else if (objectOrTag is PrimitiveTag { PrimitiveType: var primitiveType })
            {
                switch (primitiveType)
                {
                case PrimitiveType.Boolean:
                    lua_pushboolean(state, value._boolean);
                    break;

                case PrimitiveType.LightUserdata:
                    lua_pushlightuserdata(state, value._lightUserdata);
                    break;

                case PrimitiveType.Integer:
                    lua_pushinteger(state, value._integer);
                    break;

                default:
                    lua_pushnumber(state, value._number);
                    break;
                }
            }
            else
            {
                switch (value._objectType)
                {
                case ObjectType.String:
                    lua_pushstring(state, (string)objectOrTag);
                    break;

                case ObjectType.LuaObject:
                    PushLuaObject(state, (LuaObject)objectOrTag);
                    break;

                default:
                    PushClrEntity(state, objectOrTag);
                    break;
                }
            }
        }

        internal object ToClrEntity(IntPtr state, int index) => _clrEntities.ToClrEntity(state, index);

        internal LuaValue ToValue(IntPtr state, int index, LuaType type)
        {
            ToValue(state, index, type, out var value);
            return value;
        }

        internal void ToValue(IntPtr state, int index, LuaType type, out LuaValue value)
        {
            // The following code is unsafe and ignores the `readonly` aspect of `LuaValue`. However, it results in
            // significantly improved code generation: ~10% speed improvement in getting globals.
            //
            switch (type)
            {
            default:
                value = default;
                break;

            case LuaType.Boolean:
                value = default;
                Unsafe.AsRef(in value._boolean) = lua_toboolean(state, index);
                Unsafe.AsRef(in value._objectOrTag) = _booleanTag;
                break;

            case LuaType.LightUserdata:
                value = default;
                Unsafe.AsRef(in value._lightUserdata) = lua_touserdata(state, index);
                Unsafe.AsRef(in value._objectOrTag) = _lightUserdataTag;
                break;

            case LuaType.Number:
                value = default;
                if (lua_isinteger(state, index))
                {
                    Unsafe.AsRef(in value._integer) = lua_tointeger(state, index);
                    Unsafe.AsRef(in value._objectOrTag) = _integerTag;
                }
                else
                {
                    Unsafe.AsRef(in value._number) = lua_tonumber(state, index);
                    Unsafe.AsRef(in value._objectOrTag) = _numberTag;
                }
                break;

            case LuaType.String:
                value = default;
                Unsafe.AsRef(in value._objectType) = ObjectType.String;
                Unsafe.AsRef(in value._objectOrTag) = lua_tostring(state, index);
                break;

            case LuaType.Table:
                _luaObjects.ToValue(state, index, out value, LuaType.Table);
                break;

            case LuaType.Function:
                _luaObjects.ToValue(state, index, out value, LuaType.Function);
                break;

            case LuaType.Userdata:
                _clrEntities.ToValue(state, index, out value);
                break;

            case LuaType.Thread:
                _luaObjects.ToValue(state, index, out value, LuaType.Thread);
                break;
            }
        }

        internal LuaResults Call(IntPtr state, int args) => CallOrResume(state, lua_pcall(_state, args, -1, 0));

        internal LuaResults Resume(IntPtr state, int args) => CallOrResume(state, lua_resume(state, default, args));

        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void LoadString(IntPtr state, string chunk)
        {
            lua_settop(state, 0);  // Reset stack

            var status = luaL_loadstring(state, chunk);
            if (status != LuaStatus.Ok)
            {
                var message = lua_tostring(state, -1);
                throw new LuaLoadException(message);
            }
        }

        private LuaResults CallOrResume(IntPtr state, LuaStatus status)
        {
            if (status != LuaStatus.Ok && status != LuaStatus.Yield)
            {
                var message = lua_tostring(state, -1);
                throw new LuaEvalException(message);
            }

            return new LuaResults(state, this);
        }
    }
}
