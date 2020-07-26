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
        private readonly ClrObjectManager _clrTypeObjectManager;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);  // Open all standard libraries by default for convenience

            _luaObjectManager = new LuaObjectManager(_state, this);
            _clrTypeObjectManager = new ClrObjectManager(_state, this);
        }

        // A finalizer is _NOT_ feasible here. If the finalizer calls `lua_close` during an unmanaged -> managed
        // transition (which is possible since finalizers run on a separate thread), we'll never be able to make the
        // managed -> unmanaged transition successfully.
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

            _luaObjectManager.Intern(table, ptr);
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
            Load(chunk);  // Performs validation

            var ptr = lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(_state, this, reference);

            _luaObjectManager.Intern(function, ptr);
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

            _luaObjectManager.Intern(thread, ptr);
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
            Load(chunk);  // Performs validation
            return Call(_state, 0);
        }

        /// <summary>
        /// Pushes the given object <paramref name="value"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The object.</param>
        internal void PushObject(IntPtr state, object? value)
        {
            if (value is null)               lua_pushnil(state);
            else if (value is bool b)        lua_pushboolean(state, b);
            else if (value is IntPtr p)      lua_pushlightuserdata(state, p);
            else if (value is sbyte i1)      lua_pushinteger(state, i1);
            else if (value is byte u1)       lua_pushinteger(state, u1);
            else if (value is short i2)      lua_pushinteger(state, i2);
            else if (value is ushort u2)     lua_pushinteger(state, u2);
            else if (value is int i4)        lua_pushinteger(state, i4);
            else if (value is uint u4)       lua_pushinteger(state, u4);
            else if (value is long i8)       lua_pushinteger(state, i8);
            else if (value is ulong u8)      lua_pushinteger(state, (long)u8);
            else if (value is float r4)      lua_pushnumber(state, r4);
            else if (value is double r8)     lua_pushnumber(state, r8);
            else if (value is string s)      lua_pushstring(state, s);
            else if (value is LuaObject obj) PushLuaObject(state, obj);
            else                             PushClrObject(state, value);
        }

        /// <summary>
        /// Pushes the given Lua <paramref name="value"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The Lua value.</param>
        internal void PushValue(IntPtr state, in LuaValue value)
        {
            var objectOrTag = value._objectOrTag;
            if (objectOrTag is null)
            {
                lua_pushnil(state);
            }
            else if (objectOrTag is LuaValue.TypeTag)
            {
                if (objectOrTag == LuaValue._booleanTag)            lua_pushboolean(state, value._boolean);
                else if (objectOrTag == LuaValue._lightUserdataTag) lua_pushlightuserdata(state, value._lightUserdata);
                else if (objectOrTag == LuaValue._integerTag)       lua_pushinteger(state, value._integer);
                else                                                lua_pushnumber(state, value._number);
            }
            else
            {
                var integer = value._integer;
                if (integer == 1)                                   lua_pushstring(state, (string)objectOrTag);
                else if (integer == 2)                              PushLuaObject(state, (LuaObject)objectOrTag);
                else if (integer == 3)                              PushClrType(state, (Type)objectOrTag);
                else                                                PushClrObject(state, objectOrTag);
            }
        }

        /// <summary>
        /// Pushes the given Lua <paramref name="obj"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The Lua object.</param>
        internal void PushLuaObject(IntPtr state, LuaObject obj) => _luaObjectManager.Push(state, obj);

        /// <summary>
        /// Pushes the given CLR <paramref name="type"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="type">The CLR type.</param>
        internal void PushClrType(IntPtr state, Type type) => _clrTypeObjectManager.PushNonGenericType(state, type);

        /// <summary>
        /// Pushes the given CLR <paramref name="obj"/> onto the stack of the Lua <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The CLR object.</param>
        internal void PushClrObject(IntPtr state, object obj) => _clrTypeObjectManager.PushObject(state, obj);

        /// <summary>
        /// Converts the value on the stack of the Lua <paramref name="state"/> at the given <paramref name="index"/>
        /// into a Lua <paramref name="value"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the value on the stack.</param>
        /// <param name="value">The resulting Lua value.</param>
        /// <param name="type">The type of the Lua value.</param>
        internal void ToValue(IntPtr state, int index, out LuaValue value, LuaType type)
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
                Unsafe.AsRef(in value._objectOrTag) = LuaValue._booleanTag;
                break;

            case LuaType.LightUserdata:
                value = default;
                Unsafe.AsRef(in value._lightUserdata) = lua_touserdata(state, index);
                Unsafe.AsRef(in value._objectOrTag) = LuaValue._lightUserdataTag;
                break;

            case LuaType.Number:
                value = default;
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
                value = default;
                Unsafe.AsRef(in value._integer) = 1;
                Unsafe.AsRef(in value._objectOrTag) = lua_tostring(state, index);
                break;

            case LuaType.Table:
                _luaObjectManager.ToValue(state, index, out value, LuaType.Table);
                break;

            case LuaType.Function:
                _luaObjectManager.ToValue(state, index, out value, LuaType.Function);
                break;

            case LuaType.Userdata:
                _clrTypeObjectManager.ToValue(state, index, out value);
                break;

            case LuaType.Thread:
                _luaObjectManager.ToValue(state, index, out value, LuaType.Thread);
                break;
            }
        }

        /// <summary>
        /// Performs a function call with <paramref name="numArgs"/> argmuents.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="numArgs">The number of arguments.</param>
        /// <returns>The Lua results.</returns>
        internal LuaResults Call(IntPtr state, int numArgs) =>
            CallOrResume(state, lua_pcall(_state, numArgs, -1, 0));

        /// <summary>
        /// Performs a thread resume with <paramref name="numArgs"/> arguments.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="numArgs">The number of arguments.</param>
        /// <returns>The Lua results.</returns>
        internal LuaResults Resume(IntPtr state, int numArgs) =>
            CallOrResume(state, lua_resume(state, IntPtr.Zero, numArgs, out _));

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the Lua environment is disposed.
        /// </summary>
        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void Load(string chunk)
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
                var message = lua_tostring(_state, -1);
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
