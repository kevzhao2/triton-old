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
using System.Runtime.CompilerServices;
using Triton.Interop;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment.
    /// </summary>
    public class LuaEnvironment : IDisposable
    {
        private readonly IntPtr _state;
        private readonly LuaObjectManager _luaObjectManager;
        private readonly ClrTypeObjectManager _clrTypeObjectManager;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);  // Open all standard libraries by default for convenience

            _luaObjectManager = new LuaObjectManager(_state, this);
            _clrTypeObjectManager = new ClrTypeObjectManager(_state, this);
        }

        // A finalizer is _NOT_ feasible here. If the finalizer calls `lua_close` during an unmanaged -> managed
        // transition (which is possible since finalizers run on a separate thread), we'll never be able to make the
        // managed -> unmanaged transition.
        //
        // Thus, you must ensure that `LuaEnvironment` instances are properly disposed of!!
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
                ToValue(_state, -1, out var value, type);
                return value;
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
                _luaObjectManager.Dispose();

                lua_close(_state);

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Creates a new Lua table with the given initial capacities.
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

            _luaObjectManager.InternLuaObject(table, ptr);
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
            LoadString(chunk);  // Performs validation

            var ptr = lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(_state, this, reference);

            _luaObjectManager.InternLuaObject(function, ptr);
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
            var thread = new LuaThread(_state, this, reference);

            _luaObjectManager.InternLuaObject(thread, ptr);
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
            LoadString(chunk);  // Performs validation

            var status = lua_pcall(_state, 0, -1, 0);
            if (status != LuaStatus.Ok)
            {
                throw CreateExceptionFromStack<LuaEvalException>(_state);
            }

            return new LuaResults(_state, this);
        }

        /// <summary>
        /// Pushes the given Lua <paramref name="value"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The Lua value.</param>
        internal void PushValue(IntPtr state, in LuaValue value)
        {
            Debug.Assert(lua_checkstack(state, 1));

            var objectOrTag = value._objectOrTag;
            if (objectOrTag is null)
            {
                lua_pushnil(state);
            }
            else if (objectOrTag is LuaValue.TypeTag)
            {
                if (objectOrTag == LuaValue._booleanTag)
                {
                    lua_pushboolean(state, value._boolean);
                }
                else if (objectOrTag == LuaValue._lightUserdataTag)
                {
                    lua_pushlightuserdata(state, value._lightUserdata);
                }
                else if (objectOrTag == LuaValue._integerTag)
                {
                    lua_pushinteger(state, value._integer);
                }
                else
                {
                    lua_pushnumber(state, value._number);
                }
            }
            else
            {
                var integer = value._integer;
                if (integer == 1)
                {
                    lua_pushstring(state, (string)objectOrTag);
                }
                else if (integer == 2)
                {
                    PushLuaObject(state, (LuaObject)objectOrTag);
                }
                else if (integer == 3)
                {
                    PushClrType(state, (Type)objectOrTag);
                }
                else
                {
                    PushClrObject(state, objectOrTag);
                }
            }
        }

        /// <summary>
        /// Pushes the given <paramref name="obj"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The Lua object.</param>
        internal void PushLuaObject(IntPtr state, LuaObject obj) => _luaObjectManager.PushLuaObject(state, obj);

        /// <summary>
        /// Pushes the given CLR <paramref name="type"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        internal void PushClrType(IntPtr state, Type type) => _clrTypeObjectManager.PushClrType(state, type);

        /// <summary>
        /// Pushes the given CLR <paramref name="obj"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The CLR object.</param>
        internal void PushClrObject(IntPtr state, object obj) => _clrTypeObjectManager.PushClrObject(state, obj);

        /// <summary>
        /// Converts the value on the stack of the Lua <paramref name="state"/> at the given <paramref name="index"/>
        /// to a Lua <paramref name="value"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the value on the stack.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal void ToValue(IntPtr state, int index, out LuaValue value) =>
            ToValue(state, index, out value, lua_type(state, index));

        /// <summary>
        /// Converts the value on the stack of the Lua <paramref name="state"/> at the given <paramref name="index"/>
        /// to a Lua <paramref name="value"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the value on the stack.</param>
        /// <param name="value">The resulting Lua value.</param>
        /// <param name="type">The type of the Lua value.</param>
        internal void ToValue(IntPtr state, int index, out LuaValue value, LuaType type)
        {
            // TODO: in .NET 5, use `Unsafe.SkipInit` for a small perf gain
            value = default;

            // The following code ignores the `readonly` aspect of `LuaValue` and is rather smelly looking. However, it
            // results in significantly better code generation at JIT time.
            //
            switch (type)
            {
            case LuaType.Boolean:
                Unsafe.AsRef(in value._boolean) = lua_toboolean(state, index);
                Unsafe.AsRef(in value._objectOrTag) = LuaValue._booleanTag;
                break;

            case LuaType.LightUserdata:
                Unsafe.AsRef(in value._lightUserdata) = lua_touserdata(state, index);
                Unsafe.AsRef(in value._objectOrTag) = LuaValue._lightUserdataTag;
                break;

            case LuaType.Number:
                if (lua_isinteger(state, index))
                {
                    Unsafe.AsRef(in value._integer) = lua_tointeger(state, index);
                    Unsafe.AsRef(in value._objectOrTag) = LuaValue._integerTag;
                }
                else
                {
                    Unsafe.AsRef(in value._number) = lua_tonumber(state, index);
                    Unsafe.AsRef(in value._objectOrTag) = LuaValue._numberTag;
                }
                break;

            case LuaType.String:
                Unsafe.AsRef(in value._integer) = 1;
                Unsafe.AsRef(in value._objectOrTag) = lua_tostring(state, index);
                break;

            case LuaType.Table:
                _luaObjectManager.ToLuaObject(state, index, LuaType.Table, out value);
                break;

            case LuaType.Function:
                _luaObjectManager.ToLuaObject(state, index, LuaType.Function, out value);
                break;

            case LuaType.Userdata:
                _clrTypeObjectManager.ToClrTypeOrObject(state, index, out value);
                break;

            case LuaType.Thread:
                _luaObjectManager.ToLuaObject(state, index, LuaType.Thread, out value);
                break;
            }
        }

        /// <summary>
        /// Creates a <typeparamref name="TException"/> from the top of the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <typeparam name="TException">The type of exception.</typeparam>
        /// <param name="state">The Lua state.</param>
        /// <returns>The exception.</returns>
        internal TException CreateExceptionFromStack<TException>(IntPtr state)
        {
            var message = lua_tostring(state, -1);
            return (TException)Activator.CreateInstance(typeof(TException), message);
        }

        private void LoadString(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            ThrowIfDisposed();

            lua_settop(_state, 0);  // Reset stack

            var status = luaL_loadstring(_state, chunk);
            if (status != LuaStatus.Ok)
            {
                throw CreateExceptionFromStack<LuaLoadException>(_state);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
