// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Triton.Interop;
using static Triton.LuaValue;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment. This is the entrypoint for embedding Lua into a CLR application.
    /// </summary>
    public sealed class LuaEnvironment : IDisposable
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
            luaL_openlibs(_state);

            _luaObjects = new LuaObjectManager(_state, this);
            _clrEntities = new ClrEntityManager(_state, this);
        }

        // A finalizer is infeasible -- if the Lua state were closed during a Lua -> CLR transition (which is possible
        // since the finalizer runs on a separate thread), the CLR -> Lua transition would crash.

        /// <summary>
        /// Gets or sets the value of the given global.
        /// </summary>
        /// <param name="global">The global.</param>
        /// <returns>The value of the global.</returns>
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
                LoadValue(_state, -1, type, out var value);
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
        /// Evaluates the given Lua chunk.
        /// </summary>
        /// <param name="chunk">The Lua chunk to evaluate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred during loading.</exception>
        /// <exception cref="LuaRuntimeException">A Lua error occurred during runtime.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public LuaResults Eval(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            ThrowIfDisposed();

            LoadString(_state, chunk);
            return Call(_state, 0);
        }

        /// <summary>
        /// Pushes the given object onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The object.</param>
        internal unsafe void PushObject(IntPtr state, object? obj)
        {
            switch (obj)
            {
                case null:
                    lua_pushnil(state);
                    break;

                case bool b:
                    lua_pushboolean(state, b);
                    break;

                case IntPtr p:
                    lua_pushlightuserdata(state, p);
                    break;

                case UIntPtr up:
                    lua_pushlightuserdata(state, (IntPtr)up.ToPointer());
                    break;

                case byte u1:
                    lua_pushinteger(state, u1);
                    break;

                case sbyte i1:
                    lua_pushinteger(state, i1);
                    break;

                case short i2:
                    lua_pushinteger(state, i2);
                    break;

                case ushort u2:
                    lua_pushinteger(state, u2);
                    break;

                case int i4:
                    lua_pushinteger(state, i4);
                    break;

                case uint u4:
                    lua_pushinteger(state, u4);
                    break;

                case long i8:
                    lua_pushinteger(state, i8);
                    break;

                case ulong u8:
                    lua_pushinteger(state, (long)u8);
                    break;

                case float r4:
                    lua_pushnumber(state, r4);
                    break;

                case double r8:
                    lua_pushnumber(state, r8);
                    break;

                case string s:
                    lua_pushstring(state, s);
                    break;

                case char c:
                    lua_pushstring(state, c.ToString());
                    break;

                case LuaObject luaObj:
                    PushLuaObject(state, luaObj);
                    break;

                default:
                    PushClrEntity(state, obj);
                    break;
            }
        }

        /// <summary>
        /// Pushes the given Lua value onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The Lua value.</param>
        internal void PushValue(IntPtr state, in LuaValue value)
        {
            switch (value._objectOrTag)
            {
                case null:
                    lua_pushnil(state);
                    break;

                case PrimitiveTag { PrimitiveType: var primitiveType }:
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

                    break;

                case { } obj:
                    switch (value._objectType)
                    {
                        case ObjectType.String:
                            lua_pushstring(state, (string)obj);
                            break;

                        case ObjectType.LuaObject:
                            PushLuaObject(state, (LuaObject)obj);
                            break;

                        default:
                            PushClrEntity(state, obj);
                            break;
                    }

                    break;
            }
        }

        /// <summary>
        /// Pushes the given Lua object onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The Lua object.</param>
        internal void PushLuaObject(IntPtr state, LuaObject obj) => _luaObjects.Push(state, obj);

        /// <summary>
        /// Pushes the given CLR entity onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="entity">The CLR entity.</param>
        internal void PushClrEntity(IntPtr state, object entity) => _clrEntities.Push(state, entity);

        /// <summary>
        /// Loads a Lua value from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal void LoadValue(IntPtr state, int index, out LuaValue value) =>
            LoadValue(state, index, lua_type(state, index), out value);

        /// <summary>
        /// Loads a Lua value from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <param name="value">The resulting Lua value.</param>
        internal void LoadValue(IntPtr state, int index, LuaType type, out LuaValue value)
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
                    FromLuaObject(LoadLuaObject(state, index, type), out value);
                    break;

                case LuaType.Userdata:
                    FromClrEntity(LoadClrEntity(state, index), out value);
                    break;
            }
        }

        /// <summary>
        /// Loads a Lua object from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <param name="type">The type of the value.</param>
        /// <returns>The resulting Lua object.</returns>
        internal LuaObject LoadLuaObject(IntPtr state, int index, LuaType type) => _luaObjects.Load(state, index, type);

        /// <summary>
        /// Loads a CLR entity from a value on the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index.</param>
        /// <returns>The resulting CLR entity.</returns>
        internal object LoadClrEntity(IntPtr state, int index) => _clrEntities.Load(state, index);

        /// <summary>
        /// Performs a function call with the given number of arguments.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="args">The number of arguments.</param>
        /// <returns>The results.</returns>
        internal LuaResults Call(IntPtr state, int args) => CallEpilogue(state, lua_pcall(_state, args, -1, 0));

        /// <summary>
        /// Performs a thread resume with the given number of arguments.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="args">The number of arguments.</param>
        /// <returns>The results.</returns>
        internal LuaResults Resume(IntPtr state, int args) => CallEpilogue(state, lua_resume(state, default, args));

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

        private void LoadString(IntPtr state, string chunk)
        {
            if (luaL_loadstring(state, chunk) is var status && status != LuaStatus.Ok)
            {
                var message = lua_tostring(state, -1);
                throw new LuaLoadException(message);
            }
        }

        private LuaResults CallEpilogue(IntPtr state, LuaStatus status)
        {
            if (status != LuaStatus.Ok && status != LuaStatus.Yield)
            {
                var message = lua_tostring(state, -1);
                throw new LuaRuntimeException(message);
            }

            return new LuaResults(state, this);
        }
    }
}
