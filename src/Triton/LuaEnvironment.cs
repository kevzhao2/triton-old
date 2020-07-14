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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Text;
using Triton.Native;
using static Triton.Native.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents a managed Lua environment. This class is not thread-safe.
    /// </summary>
    public sealed unsafe partial class LuaEnvironment : DynamicObject, IDisposable
    {
        private readonly lua_State* _state;

        private readonly Dictionary<IntPtr, (int reference, WeakReference<LuaObject> weakReference)> _luaObjects =
            new Dictionary<IntPtr, (int, WeakReference<LuaObject>)>();

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class.
        /// </summary>
        public LuaEnvironment()
        {
            _state = luaL_newstate();
            luaL_openlibs(_state);

            _stringBuffer = (byte*)Marshal.AllocHGlobal(StringBufferSize);

            // TODO: set up garbage collection
        }

        /// <summary>
        /// Finalizes the <see cref="LuaEnvironment"/> instance.
        /// </summary>
        [ExcludeFromCodeCoverage]
        ~LuaEnvironment()
        {
            DisposeUnmanaged();
        }

        /// <summary>
        /// Gets or sets the value of the given <paramref name="global"/>.
        /// </summary>
        /// <param name="global">The global.</param>
        /// <returns>The value of the given <paramref name="global"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="global"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object? this[string global]
        {
            get
            {
                if (global is null)
                {
                    throw new ArgumentNullException(nameof(global));
                }

                ThrowIfDisposed();
                ThrowIfNotEnoughLuaStack(_state, 1);  // 1 stack slot required

                using var buffer = CreateStringBuffer(global);
                var type = lua_getglobal(_state, buffer);

                try
                {
                    return ToObject(_state, -1, typeHint: type);
                }
                finally
                {
                    lua_pop(_state, 1);  // Pop the value off the stack
                }
            }

            set
            {
                if (global is null)
                {
                    throw new ArgumentNullException(nameof(global));
                }

                ThrowIfDisposed();
                ThrowIfNotEnoughLuaStack(_state, 1);  // 1 stack slot requierd

                PushObject(_state, value);

                using var buffer = CreateStringBuffer(global);
                lua_setglobal(_state, buffer);
            }
        }

        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name]!;
            return true;
        }

        /// <inheritdoc/>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                DisposeUnmanaged();
                GC.SuppressFinalize(this);

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
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
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
            ThrowIfNotEnoughLuaStack(_state, 1);  // 1 stack slot required

            lua_createtable(_state, sequentialCapacity, nonSequentialCapacity);

            // Because we just created a table, it is guaranteed to be a unique table. So we can just construct a new
            // `LuaTable` instance without checking if it is cached.

            var ptr = (IntPtr)lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var table = new LuaTable(this, reference, _state);

            _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(table));
            return table;
        }

        /// <summary>
        /// Creates a Lua function from loading the given Lua <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The Lua chunk to load.</param>
        /// <returns>The resulting Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The environment is disposed.</exception>
        public LuaFunction CreateFunction(string chunk)
        {
            LoadStringInternal(chunk);

            // Because we just created a function, it is guaranteed to be a unique function. So we can just construct
            // a new `LuaFunction` instance without checking if it is cached.

            var ptr = (IntPtr)lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(this, reference, _state);

            _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(function));
            return function;
        }

        /// <summary>
        /// Creates a Lua thread.
        /// </summary>
        /// <returns>The resulting Lua thread.</returns>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The environment is disposed.</exception>
        public LuaThread CreateThread()
        {
            ThrowIfDisposed();
            ThrowIfNotEnoughLuaStack(_state, 1);  // 1 stack slot required

            var threadState = lua_newthread(_state);

            // Because we just created a thread, it is guaranteed to be a unique thread. So we can just construct
            // a new `LuaThread` instance without checking if it is cached.
            
            var ptr = (IntPtr)lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var thread = new LuaThread(this, reference, threadState);

            _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(thread));
            return thread;
        }

        /// <summary>
        /// Evaluates the given Lua <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">The Lua chunk to evaluate.</param>
        /// <returns>The results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chunk"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaEvaluationException">A Lua error occurred when evaluating the chunk.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The environment is disposed.</exception>
        public object?[] Eval(string chunk)
        {
            LoadStringInternal(chunk);

            var oldTop = lua_gettop(_state) - 1;
            var status = lua_pcall(_state, 0, -1, 0);
            if (status != LuaStatus.Ok)
            {
                throw CreateExceptionFromLuaStack<LuaEvaluationException>(_state);
            }

            var numResults = lua_gettop(_state) - oldTop;
            return MarshalResults(_state, numResults);
        }

        /// <summary>
        /// Pushes the given <paramref name="value"/> onto the stack.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="value">The value to push onto the stack.</param>
        internal void PushObject(lua_State* state, object? value)
        {
            Debug.Assert(state != null);
            Debug.Assert(lua_checkstack(state, 1));

            if (value is null)
            {
                lua_pushnil(state);
            }
            else if (value is bool b)
            {
                lua_pushboolean(state, b);
            }
            else if (value is sbyte i8)
            {
                lua_pushinteger(state, i8);
            }
            else if (value is byte u8)
            {
                lua_pushinteger(state, u8);
            }
            else if (value is short i16)
            {
                lua_pushinteger(state, i16);
            }
            else if (value is ushort u16)
            {
                lua_pushinteger(state, u16);
            }
            else if (value is int i32)
            {
                lua_pushinteger(state, i32);
            }
            else if (value is uint u32)
            {
                lua_pushinteger(state, u32);
            }
            else if (value is long i64)
            {
                lua_pushinteger(state, i64);
            }
            else if (value is ulong u64)
            {
                lua_pushinteger(state, (long)u64);
            }
            else if (value is float f32)
            {
                lua_pushnumber(state, f32);
            }
            else if (value is double f64)
            {
                lua_pushnumber(state, f64);
            }
            else if (value is decimal d)
            {
                lua_pushnumber(state, (double)d);
            }
            else if (value is char c)
            {
                PushString(state, c.ToString());
            }
            else if (value is string s)
            {
                PushString(state, s);
            }
            else if (value is LuaObject luaObject)
            {
                if (luaObject._environment != this)
                {
                    throw new InvalidOperationException("Lua object does not belong to this environment");
                }

                lua_rawgeti(state, LUA_REGISTRYINDEX, luaObject._reference);
            }
            else
            {
                throw new NotImplementedException();
            }

            void PushString(lua_State* state, string s)
            {
                using var buffer = CreateStringBuffer(s);
                _ = lua_pushlstring(state, buffer, buffer.Length);
            }
        }

        /// <summary>
        /// Converts the Lua value on the stack at <paramref name="index"/> to an object.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="index">The index of the Lua value.</param>
        /// <param name="typeHint">A hint for the Lua value's type.</param>
        /// <returns>The Lua value on the stack at <paramref name="index"/> as an object.</returns>
        internal object? ToObject(lua_State* state, int index, LuaType? typeHint = null)
        {
            Debug.Assert(state != null);

            return (typeHint ?? lua_type(state, index)) switch
            {
                LuaType.Nil => null,
                LuaType.Boolean => lua_toboolean(state, index),
                LuaType.LightUserdata => throw new NotImplementedException(),
                LuaType.Number => ToIntegerOrNumber(state, index),
                LuaType.String => ToString(state, index),
                LuaType.Table => ToLuaObject(state, index, LuaType.Table),
                LuaType.Function => ToLuaObject(state, index, LuaType.Function),
                LuaType.Userdata => throw new NotImplementedException(),
                LuaType.Thread => ToLuaObject(state, index, LuaType.Thread),
                _ => throw new InvalidOperationException(),
            };

            // Since integers and numbers have the same type, we need to differentiate the two using `lua_isinteger`.
            static object ToIntegerOrNumber(lua_State* state, int index) =>
                lua_isinteger(state, index) ? (object)lua_tointeger(state, index) : lua_tonumber(state, index);

            static string ToString(lua_State* state, int index)
            {
                UIntPtr len;
                var buffer = lua_tolstring(state, index, &len);
                return Encoding.UTF8.GetString(buffer, (int)len);
            }

            LuaObject ToLuaObject(lua_State* state, int index, LuaType typeHint)
            {
                var ptr = (IntPtr)lua_topointer(state, index);
                var (reference, luaObject) = GetLuaObject(ptr);
                if (luaObject != null)
                {
                    return luaObject;
                }

                // If there is no reference for the object, we need to create it.
                if (reference == LUA_REFNIL)
                {
                    Debug.Assert(lua_checkstack(state, 1));

                    lua_pushvalue(state, index);
                    reference = luaL_ref(state, LUA_REGISTRYINDEX);
                }

                luaObject = typeHint switch
                {
                    LuaType.Table => new LuaTable(this, reference, state),
                    LuaType.Function => new LuaFunction(this, reference, state),
                    LuaType.Thread => new LuaThread(this, reference, (lua_State*)ptr),  // Special case for threads
                    _ => throw new InvalidOperationException()
                };

                _luaObjects[ptr] = (reference, new WeakReference<LuaObject>(luaObject));
                return luaObject;

                (int reference, LuaObject? luaObject) GetLuaObject(IntPtr ptr)
                {
                    if (_luaObjects.TryGetValue(ptr, out var tuple))
                    {
                        var (reference, weakReference) = tuple;
                        return (reference, weakReference.TryGetTarget(out var luaObject) ? luaObject : null);
                    }

                    return (LUA_REFNIL, null);
                }
            }
        }

        /// <summary>
        /// Marshals <paramref name="numResults"/> results on the stack to an object array.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="numResults">The old stack top.</param>
        /// <returns>The results.</returns>
        internal object?[] MarshalResults(lua_State* state, int numResults)
        {
            if (numResults == 0)
            {
                return Array.Empty<object?>();
            }

            try
            {
                ThrowIfNotEnoughLuaStack(state, 1);  // 1 stack slot required (due to LuaObject)

                var results = new object?[numResults];
                for (var i = 0; i < numResults; ++i)
                {
                    results[i] = ToObject(state, i - numResults);
                }

                return results;
            }
            finally
            {
                lua_pop(state, numResults);
            }
        }

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

        /// <summary>
        /// Throws a <see cref="LuaStackException"/> if the Lua stack does not have enough space for
        /// <paramref name="requestedSlots"/>.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="requestedSlots">The requested number of slots.</param>
        internal void ThrowIfNotEnoughLuaStack(lua_State* state, int requestedSlots)
        {
            Debug.Assert(state != null);
            Debug.Assert(requestedSlots >= 0);

            if (requestedSlots > 0 && !lua_checkstack(state, requestedSlots))
            {
                throw new LuaStackException(requestedSlots);
            }
        }

        /// <summary>
        /// Creates a <typeparamref name="TException"/> instance from the top of the Lua stack.
        /// </summary>
        /// <typeparam name="TException">The type of exception.</typeparam>
        /// <param name="state">The Lua state.</param>
        internal TException CreateExceptionFromLuaStack<TException>(lua_State* state) where TException : LuaException
        {
            Debug.Assert(state != null);
            Debug.Assert(lua_type(state, -1) == LuaType.String);

            try
            {
                var message = (string)ToObject(state, -1, typeHint: LuaType.String)!;
                return (TException)Activator.CreateInstance(typeof(TException), message);
            }
            finally
            {
                lua_pop(state, 1);
            }
        }

        // Loads a Lua chunk onto the stack as a function.
        private void LoadStringInternal(string chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            ThrowIfDisposed();
            ThrowIfNotEnoughLuaStack(_state, 1);  // 1 stack slot required

            using var buffer = CreateStringBuffer(chunk);
            var status = luaL_loadstring(_state, buffer);

            if (status != LuaStatus.Ok)
            {
                throw CreateExceptionFromLuaStack<LuaLoadException>(_state);
            }
        }

        // Disposes unmanaged resources.
        private void DisposeUnmanaged()
        {
            lua_close(_state);
            Marshal.FreeHGlobal((IntPtr)_stringBuffer);
        }
    }
}
