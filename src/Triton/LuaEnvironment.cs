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
using System.Runtime.CompilerServices;
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
        private const int StringBufferSize = 1 << 16;

        private readonly lua_State* _state;
        private readonly byte* _stringBuffer;

        private readonly Dictionary<IntPtr, WeakReference<LuaObject>> _luaObjects =
            new Dictionary<IntPtr, WeakReference<LuaObject>>();

        private readonly Dictionary<IntPtr, int> _references = new Dictionary<IntPtr, int>();

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class with the default encoding, ASCII.
        /// </summary>
        public LuaEnvironment() : this(Encoding.ASCII)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaEnvironment"/> class with the specified
        /// <paramref name="encoding"/>.
        /// </summary>
        /// <param name="encoding">The encoding to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>.</exception>
        public LuaEnvironment(Encoding encoding)
        {
            _state = luaL_newstate();
            _stringBuffer = (byte*)Marshal.AllocHGlobal(StringBufferSize);

            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            Globals = new LuaTable(this, LUA_RIDX_GLOBALS, _state);

            luaL_openlibs(_state);

            // TODO: set up garbage collection
        }

        /// <summary>
        /// Finalizes the <see cref="LuaEnvironment"/> instance.
        /// </summary>
        ~LuaEnvironment()
        {
            DisposeUnmanaged();
        }

        /// <summary>
        /// Gets or sets the value of the global with name <paramref name="s"/>.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The value of the global with name <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaStackException">The Lua stack space is insufficient.</exception>
        /// <exception cref="ObjectDisposedException">The Lua environment is disposed.</exception>
        public object? this[string s]
        {
            get
            {
                if (s is null)
                {
                    throw new ArgumentNullException(nameof(s));
                }

                ThrowIfDisposed();

                // We require 1 stack slot since the value of the global is pushed onto the stack.
                ThrowIfNotEnoughLuaStack(_state, 1);
#if DEBUG
                var oldTop = lua_gettop(_state);
#endif

                // May throw an exception
                var buffer = MarshalString(s, out _, out var wasAllocated, isNullTerminated: true);

                var type = lua_getglobal(_state, buffer);

                if (wasAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                }

                try
                {
                    return ToObject(_state, -1, typeHint: type);  // May throw an exception
                }
                finally
                {
                    lua_pop(_state, 1);  // Pop the value of the global off of the stack
#if DEBUG
                    Debug.Assert(lua_gettop(_state) == oldTop);
#endif
                }
            }

            set
            {
                if (s is null)
                {
                    throw new ArgumentNullException(nameof(s));
                }

                ThrowIfDisposed();

                // We require 1 stack slot since the new value of the global is pushed onto the stack.
                ThrowIfNotEnoughLuaStack(_state, 1);
#if DEBUG
                var oldTop = lua_gettop(_state);
#endif

                // Push the value onto the stack first, as it may involve using the string buffer.
                PushObject(_state, value);  // May throw an exception

                try
                {
                    var buffer = MarshalString(s, out _, out var wasAllocated, isNullTerminated: true);

                    lua_setglobal(_state, buffer);

                    if (wasAllocated)
                    {
                        Marshal.FreeHGlobal((IntPtr)buffer);
                    }
                }
                catch (EncoderFallbackException)
                {
                    lua_pop(_state, 1);  // Pop the value of the global off of the stack
                    throw;
                }
#if DEBUG
                finally
                {
                    Debug.Assert(lua_gettop(_state) == oldTop);
                }
#endif
            }
        }

        /// <summary>
        /// Gets the Lua environment's encoding.
        /// </summary>
        /// <value>The Lua environment's encoding.</value>
        public Encoding Encoding { get; }

        /// <summary>
        /// Gets the Lua environment's globals in the form of a table.
        /// </summary>
        /// <value>The Lua environment's globals in the form of a table.</value>
        public LuaTable Globals { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeUnmanaged();
            GC.SuppressFinalize(this);
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
            ThrowIfDisposed();

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
#if DEBUG
            var oldTop = lua_gettop(_state);

            try
            {
#endif
                lua_createtable(_state, sequentialCapacity, nonSequentialCapacity);

                // Because we just created a table, it is guaranteed to be a unique table. So we can just construct a
                // new `LuaTable` instance without checking if it is cached.
                var ptr = (IntPtr)lua_topointer(_state, -1);
                var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
                var table = new LuaTable(this, reference, _state);

                _references[ptr] = reference;
                _luaObjects[ptr] = new WeakReference<LuaObject>(table);
                return table;
#if DEBUG
            }
            finally
            {
                Debug.Assert(lua_gettop(_state) == oldTop);
            }
#endif
        }

        /// <summary>
        /// Creates a Lua function from loading the string <paramref name="s"/> as a Lua chunk.
        /// </summary>
        /// <param name="s">The string to load as a Lua chunk.</param>
        /// <returns>The resulting Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaLoadException">A Lua error occurred when loading the chunk.</exception>
        /// <exception cref="ObjectDisposedException">The environment is disposed.</exception>
        public LuaFunction CreateFunction(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            ThrowIfDisposed();
#if DEBUG
            var oldTop = lua_gettop(_state);

            try
            {
#endif
                LoadString(s);

                // Because we just created a function, it is guaranteed to be a unique function. So we can just
                // construct a new `LuaFunction` instance without checking if it is cached.
                var ptr = (IntPtr)lua_topointer(_state, -1);
                var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
                var function = new LuaFunction(this, reference, _state);

                _references[ptr] = reference;
                _luaObjects[ptr] = new WeakReference<LuaObject>(function);
                return function;
#if DEBUG
            }
            finally
            {
                Debug.Assert(lua_gettop(_state) == oldTop);
            }
#endif
        }

        #region Stack manipulation helpers

        // Pushes a value onto the stack. May throw an exception.
        internal void Push<T>(lua_State* state, T value)
        {
            Debug.Assert(lua_checkstack(state, 1));

            if (value is null)
            {
                lua_pushnil(state);
            }
            else if (typeof(T) == typeof(bool))
            {
                PushBoolean(state, Unsafe.As<T, bool>(ref value));
            }
            else if (typeof(T) == typeof(sbyte))
            {
                PushInteger(state, Unsafe.As<T, sbyte>(ref value));
            }
            else if (typeof(T) == typeof(byte))
            {
                PushInteger(state, Unsafe.As<T, byte>(ref value));
            }
            else if (typeof(T) == typeof(short))
            {
                PushInteger(state, Unsafe.As<T, short>(ref value));
            }
            else if (typeof(T) == typeof(ushort))
            {
                PushInteger(state, Unsafe.As<T, ushort>(ref value));
            }
            else if (typeof(T) == typeof(int))
            {
                PushInteger(state, Unsafe.As<T, int>(ref value));
            }
            else if (typeof(T) == typeof(uint))
            {
                PushInteger(state, Unsafe.As<T, uint>(ref value));
            }
            else if (typeof(T) == typeof(long))
            {
                PushInteger(state, Unsafe.As<T, long>(ref value));
            }
            else if (typeof(T) == typeof(ulong))
            {
                PushInteger(state, (long)Unsafe.As<T, ulong>(ref value));
            }
            else if (typeof(T) == typeof(float))
            {
                PushNumber(state, Unsafe.As<T, float>(ref value));
            }
            else if (typeof(T) == typeof(double))
            {
                PushNumber(state, Unsafe.As<T, double>(ref value));
            }
            else if (typeof(T) == typeof(decimal))
            {
                PushNumber(state, (double)Unsafe.As<T, decimal>(ref value));
            }
            else if (typeof(T) == typeof(char))
            {
                PushString(state, Unsafe.As<T, char>(ref value).ToString());
            }
            else if (typeof(T) == typeof(string))
            {
                PushString(state, Unsafe.As<T, string>(ref value));
            }
            else if (
                typeof(T) == typeof(LuaTable) || typeof(T) == typeof(LuaFunction) || typeof(T) == typeof(LuaThread))
            {
                PushLuaObject(state, Unsafe.As<T, LuaObject>(ref value));
            }
            else
            {
                throw new NotImplementedException();
            } 
        }

        // Pushes an object onto the stack. May throw an exception.
        internal void PushObject(lua_State* state, object? value)
        {
            Debug.Assert(lua_checkstack(state, 1));

            if (value is null)
            {
                lua_pushnil(state);
            }
            else if (value is bool b)
            {
                PushBoolean(state, b);
            }
            else if (value is sbyte i8)
            {
                PushInteger(state, i8);
            }
            else if (value is byte u8)
            {
                PushInteger(state, u8);
            }
            else if (value is short i16)
            {
                PushInteger(state, i16);
            }
            else if (value is ushort u16)
            {
                PushInteger(state, u16);
            }
            else if (value is int i32)
            {
                PushInteger(state, i32);
            }
            else if (value is uint u32)
            {
                PushInteger(state, u32);
            }
            else if (value is long i64)
            {
                PushInteger(state, i64);
            }
            else if (value is ulong u64)
            {
                PushInteger(state, (long)u64);
            }
            else if (value is float f32)
            {
                PushNumber(state, f32);
            }
            else if (value is double f64)
            {
                PushNumber(state, f64);
            }
            else if (value is decimal d)
            {
                PushNumber(state, (double)d);
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
                PushLuaObject(state, luaObject);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // Pushes a boolean onto the stack.
        internal void PushBoolean(lua_State* state, bool b)
        {
            Debug.Assert(lua_checkstack(state, 1));

            lua_pushboolean(state, b);
        }

        // Pushes an integer onto the stack.
        internal void PushInteger(lua_State* state, long n)
        {
            Debug.Assert(lua_checkstack(state, 1));

            lua_pushinteger(state, n);
        }

        // Pushes a number onto the stack.
        internal void PushNumber(lua_State* state, double n)
        {
            Debug.Assert(lua_checkstack(state, 1));

            lua_pushnumber(state, n);
        }

        // Pushes a string onto the stack. May throw an exception.
        internal void PushString(lua_State* state, string s)
        {
            Debug.Assert(lua_checkstack(state, 1));

            var buffer = MarshalString(s, out var len, out var wasAllocated, isNullTerminated: false);

            try
            {
                _ = lua_pushlstring(state, buffer, len);
            }
            finally
            {
                if (wasAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                }
            }
        }

        // Pushes a Lua object onto the stack. Throws an `InvalidOperationException` if the Lua object does not belong
        // to this environment.
        internal void PushLuaObject(lua_State* state, LuaObject obj)
        {
            Debug.Assert(lua_checkstack(state, 1));

            if (obj._environment != this)
            {
                throw new InvalidOperationException("Lua object does not belong to this environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, obj._reference);
        }

        // Converts a Lua value on the stack to an object. The type hint saves a P/Invoke. May throw an exception.
        [SuppressMessage(
            "Performance", "HAA0601:Value type to reference type conversion causing boxing allocation",
            Justification = "Object return is required")]
        internal object? ToObject(lua_State* state, int index, LuaType? typeHint = null)
        {
            return (typeHint ?? lua_type(state, index)) switch
            {
                LuaType.Nil => null,
                LuaType.Boolean => ToBoolean(state, index),
                LuaType.LightUserdata => throw new NotImplementedException(),
                LuaType.Number => ToIntegerOrNumber(state, index),
                LuaType.String => ToString(state, index),  // May throw an exception
                LuaType.Table => ToLuaObject(state, index, typeHint: LuaType.Table),
                LuaType.Function => ToLuaObject(state, index, typeHint: LuaType.Function),
                LuaType.Userdata => throw new NotImplementedException(),
                LuaType.Thread => ToLuaObject(state, index, typeHint: LuaType.Thread),
                _ => throw new InvalidOperationException(),
            };

            // Since integers and numbers have the same type, we need to differentiate the two using `lua_isinteger`.
            object ToIntegerOrNumber(lua_State* state, int index) =>
                lua_isinteger(state, index) ? (object)ToInteger(state, index) : ToNumber(state, index);
        }

        // Converts a Lua value on the stack to a boolean.
        internal bool ToBoolean(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.Boolean);

            return lua_toboolean(state, index);
        }

        // Converts a Lua value on the stack to an integer.
        internal long ToInteger(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.Number);
            Debug.Assert(lua_isinteger(state, index));

            return lua_tointeger(state, index);
        }

        // Converts a Lua value on the stack to a number.
        internal double ToNumber(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.Number);
            Debug.Assert(lua_isnumber(state, index));

            return lua_tonumber(state, index);
        }

        // Converts a Lua value on the stack to a string. May throw an exception.
        internal string ToString(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.String);

            UIntPtr len;
            var buffer = lua_tolstring(state, index, &len);

            return Encoding.GetString(buffer, (int)len);
        }

        // Converts a Lua value on the stack to a Lua object. The type hint saves a P/Invoke.
        internal LuaObject ToLuaObject(lua_State* state, int index, LuaType? typeHint = null)
        {
            // Try to retrieve the cached Lua object, if possible. This reduces the number of allocations.
            var ptr = (IntPtr)lua_topointer(state, index);
            if (_luaObjects.TryGetValue(ptr, out var weakLuaObject) && weakLuaObject.TryGetTarget(out var luaObject))
            {
                return luaObject;
            }
#if DEBUG
            var oldTop = lua_gettop(_state);

            try
            {
#endif
                Debug.Assert(lua_checkstack(state, 1));

                // Construct the Lua object by storing a reference to it inside of the Lua registry. This prevents Lua
                // from garbage collecting the object.
                lua_pushvalue(state, index);
                var reference = luaL_ref(state, LUA_REGISTRYINDEX);

                Debug.Assert(reference != LUA_REFNIL);

                luaObject = (typeHint ?? lua_type(state, index)) switch
                {
                    LuaType.Table => new LuaTable(this, reference, _state),
                    LuaType.Function => new LuaFunction(this, reference, _state),
                    LuaType.Thread => new LuaThread(this, reference, (lua_State*)ptr),  // Special case for threads
                    _ => throw new InvalidOperationException()
                };

                // Try to reuse the weak reference, if possible. This reduces the number of allocations.
                if (weakLuaObject != null)
                {
                    weakLuaObject.SetTarget(luaObject);
                }
                else
                {
                    _luaObjects[ptr] = new WeakReference<LuaObject>(luaObject);
                }

                // Store the reference information so that dead Lua objects can be properly cleaned up when Lua performs
                // a garbage collection.
                _references[ptr] = reference;
                return luaObject;
#if DEBUG
            }
            finally
            {
                Debug.Assert(lua_gettop(_state) == oldTop);
            }
#endif
        }

        #endregion

        // Marshals a string to a string buffer using the Lua environment's encoding.
        internal StringBuffer MarshalString(string s, bool isNullTerminated)
        {
            Debug.Assert(s != null);

            var maxByteLength = Encoding.GetMaxByteCount(s.Length) + (isNullTerminated ? 1 : 0);

            // If the maximum byte length is small enough, then we can use `_stringBuffer`. Otherwise, we'll have to
            // perform an allocation.
            var isAllocated = maxByteLength > StringBufferSize;

            byte* pointer = isAllocated ? (byte*)Marshal.AllocHGlobal(maxByteLength) : _stringBuffer;

            try
            {
                UIntPtr length;
                fixed (char* sPtr = s)
                {
                    length = (UIntPtr)Encoding.GetBytes(sPtr, s.Length, pointer, maxByteLength);  // May throw
                }

                if (isNullTerminated)
                {
                    pointer[(int)length] = 0;
                }

                return new StringBuffer(pointer, length, isAllocated);
            }
            catch (EncoderFallbackException)
            {
                if (isAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)pointer);
                }

                throw;
            }
        }

        // Marshals a string to a byte pointer using the Lua environment's encoding.
        internal byte* MarshalString(string s, out UIntPtr len, out bool wasAllocated, bool isNullTerminated)
        {
            Debug.Assert(s != null);

            var maxByteLength = Encoding.GetMaxByteCount(s.Length) + (isNullTerminated ? 1 : 0);

            // If the maximum byte length is small enough, then we'll use the string buffer. Otherwise, we'll have to
            // perform an allocation.
            wasAllocated = maxByteLength > StringBufferSize;

            byte* buffer = wasAllocated ? (byte*)Marshal.AllocHGlobal(maxByteLength) : _stringBuffer;

            try
            {
                fixed (char* sPtr = s)
                {
                    len = (UIntPtr)Encoding.GetBytes(sPtr, s.Length, buffer, maxByteLength);  // May throw an exception
                }

                if (isNullTerminated)
                {
                    buffer[(int)len] = 0;
                }

                return buffer;
            }
            catch
            {
                if (wasAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                }

                throw;
            }
        }

        // Throws an `ObjectDisposedException` if the Lua environment is disposed.
        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        // Throws a `LuaStackException` if the Lua stack does not have enough space for the requested number of slots.
        internal void ThrowIfNotEnoughLuaStack(lua_State* state, int requestedSlots)
        {
            Debug.Assert(requestedSlots >= 1);

            if (!lua_checkstack(state, requestedSlots))
            {
                throw new LuaStackException(requestedSlots);
            }
        }

        // Loads a string as a function and pushes it on top of the stack.
        private void LoadString(string s)
        {
            if (LoadString(s) != LuaStatus.Ok)
            {
                try
                {
                    var errorMessage = ToString(_state, -1);  // May throw an exception
                    throw new LuaLoadException(errorMessage);
                }
                finally
                {
                    lua_pop(_state, 1);
                }
            }

            LuaStatus LoadString(string s)
            {
                // May throw an exception
                var buffer = MarshalString(s, out _, out var wasAllocated, isNullTerminated: true);

                var status = luaL_loadstring(_state, buffer);

                if (wasAllocated)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                }

                return status;
            }
        }

        // Disposes the following unmanaged resources: `_state` and `_stringBuffer`.
        private void DisposeUnmanaged()
        {
            if (!_isDisposed)
            {
                lua_close(_state);
                Marshal.FreeHGlobal((IntPtr)_stringBuffer);
                _isDisposed = true;
            }
        }
    }
}
