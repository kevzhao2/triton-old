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
    public sealed unsafe class LuaEnvironment : DynamicObject, IDisposable
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
        /// Gets the Lua environment's encoding.
        /// </summary>
        /// <value>The Lua environment's encoding.</value>
        public Encoding Encoding { get; }

        /// <summary>
        /// Gets the Lua environment's globals as a table.
        /// </summary>
        /// <value>The Lua environment's globals as a table.</value>
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

            lua_createtable(_state, sequentialCapacity, nonSequentialCapacity);

            // Because we just created a table, it is guaranteed to be a unique table. So we can just construct a new
            // `LuaTable` instance without checking if it is cached.

            var ptr = (IntPtr)lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var table = new LuaTable(this, reference, _state);

            _references[ptr] = reference;
            _luaObjects[ptr] = new WeakReference<LuaObject>(table);
            return table;
        }

        /// <summary>
        /// Creates a Lua function from loading the string <paramref name="s"/> as a Lua chunk.
        /// </summary>
        /// <param name="s">The string to load as a Lua chunk.</param>
        /// <returns>The resulting Lua function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="LuaException">A Lua error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The environment is disposed.</exception>
        public LuaFunction CreateFunction(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            ThrowIfDisposed();

            LoadStringInternal(s);

            // Because we just created a function, it is guaranteed to be a unique function. So we can just construct a
            // new `LuaFunction` instance without checking if it is cached.
            
            var ptr = (IntPtr)lua_topointer(_state, -1);
            var reference = luaL_ref(_state, LUA_REGISTRYINDEX);
            var function = new LuaFunction(this, reference, _state);

            _references[ptr] = reference;
            _luaObjects[ptr] = new WeakReference<LuaObject>(function);
            return function;
        }

        internal void Push(lua_State* state, object value)
        {
            Debug.Assert(lua_checkstack(state, 1));

            if (value is null)
            {
                lua_pushnil(state);
            }
            else if (value is bool b)
            {
                Push(state, b);
            }
            else if (value is sbyte n)
            {
                Push(state, (long)n);
            }
            else if (value is byte n2)
            {
                Push(state, (long)n2);
            }
            else if (value is short n3)
            {
                Push(state, (long)n3);
            }
            else if (value is ushort n4)
            {
                Push(state, (long)n4);
            }
            else if (value is int n5)
            {
                Push(state, (long)n5);
            }
            else if (value is uint n6)
            {
                Push(state, (long)n6);
            }
            else if (value is long n7)
            {
                Push(state, n7);
            }
            else if (value is ulong n8)
            {
                Push(state, (long)n8);
            }
            else if (value is float n9)
            {
                Push(state, (double)n9);
            }
            else if (value is double n10)
            {
                Push(state, n10);
            }
            else if (value is decimal n11)
            {
                Push(state, n11);
            }
            else if (value is char c)
            {
                Push(state, c.ToString());
            }
            else if (value is string s)
            {
                Push(state, s);
            }
            else if (value is LuaObject luaObject)
            {
                Push(state, luaObject);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal void Push<T>(lua_State* state, T value)
        {
            Debug.Assert(lua_checkstack(state, 1));

            if (value is null)
            {
                lua_pushnil(state);
            }
            else if (typeof(T) == typeof(bool))
            {
                Push(state, Unsafe.As<T, bool>(ref value));
            }
            else if (typeof(T) == typeof(sbyte))
            {
                Push(state, (long)Unsafe.As<T, sbyte>(ref value));
            }
            else if (typeof(T) == typeof(byte))
            {
                Push(state, (long)Unsafe.As<T, byte>(ref value));
            }
            else if (typeof(T) == typeof(short))
            {
                Push(state, (long)Unsafe.As<T, short>(ref value));
            }
            else if (typeof(T) == typeof(ushort))
            {
                Push(state, (long)Unsafe.As<T, ushort>(ref value));
            }
            else if (typeof(T) == typeof(int))
            {
                Push(state, (long)Unsafe.As<T, int>(ref value));
            }
            else if (typeof(T) == typeof(uint))
            {
                Push(state, (long)Unsafe.As<T, uint>(ref value));
            }
            else if (typeof(T) == typeof(long))
            {
                Push(state, Unsafe.As<T, long>(ref value));
            }
            else if (typeof(T) == typeof(ulong))
            {
                Push(state, (long)Unsafe.As<T, ulong>(ref value));
            }
            else if (typeof(T) == typeof(float))
            {
                Push(state, (double)Unsafe.As<T, float>(ref value));
            }
            else if (typeof(T) == typeof(double))
            {
                Push(state, Unsafe.As<T, double>(ref value));
            }
            else if (typeof(T) == typeof(decimal))
            {
                Push(state, (double)Unsafe.As<T, decimal>(ref value));
            }
            else if (typeof(T) == typeof(char))
            {
                Push(state, Unsafe.As<T, char>(ref value).ToString());
            }
            else if (typeof(T) == typeof(string))
            {
                Push(state, Unsafe.As<T, string>(ref value));
            }
            else if (
                typeof(T) == typeof(LuaTable) || typeof(T) == typeof(LuaFunction) || typeof(T) == typeof(LuaThread))
            {
                Push(state, Unsafe.As<T, LuaObject>(ref value));
            }
            else
            {
                throw new NotImplementedException();
            } 
        }

        internal void Push(lua_State* state, bool b)
        {
            Debug.Assert(lua_checkstack(state, 1));

            lua_pushboolean(state, b);
        }

        internal void Push(lua_State* state, long n)
        {
            Debug.Assert(lua_checkstack(state, 1));

            lua_pushinteger(state, n);
        }

        internal void Push(lua_State* state, double n)
        {
            Debug.Assert(lua_checkstack(state, 1));

            lua_pushnumber(state, n);
        }

        internal void Push(lua_State* state, string s)
        {
            Debug.Assert(lua_checkstack(state, 1));

            // If the maximum byte length is small enough, then we'll use the string buffer. Otherwise, we'll have to
            // resort to an allocation.
            var maxByteLength = Encoding.GetMaxByteCount(s.Length);
            var useBuffer = maxByteLength <= StringBufferSize;

            byte* buffer = useBuffer ? _stringBuffer : (byte*)Marshal.AllocHGlobal(maxByteLength);

            try
            {
                var byteLength = 0;
                fixed (char* sPtr = s)
                {
                    byteLength = Encoding.GetBytes(sPtr, s.Length, buffer, maxByteLength);
                }

                _ = lua_pushlstring(state, buffer, (UIntPtr)byteLength);
            }
            finally
            {
                if (!useBuffer)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                }
            }
        }

        internal void Push(lua_State* state, LuaObject obj)
        {
            Debug.Assert(lua_checkstack(state, 1));

            if (obj._environment != this)
            {
                throw new InvalidOperationException("Lua object does not belong to this environment");
            }

            lua_rawgeti(state, LUA_REGISTRYINDEX, obj._reference);
        }

        internal object? ToObject(lua_State* state, int index, LuaType? typeHint = null)
        {
            return (typeHint ?? lua_type(state, index)) switch
            {
                LuaType.Nil => null,
                LuaType.Boolean => ToBoolean(state, index),
                LuaType.LightUserdata => throw new NotImplementedException(),
                LuaType.Number => ToIntegerOrNumber(state, index),
                LuaType.String => ToString(state, index),
                LuaType.Table => ToLuaObject(state, index, LuaType.Table),
                LuaType.Function => ToLuaObject(state, index, LuaType.Function),
                LuaType.Userdata => throw new NotImplementedException(),
                LuaType.Thread => ToLuaObject(state, index, LuaType.Thread),
                _ => throw new InvalidOperationException(),
            };

            object ToIntegerOrNumber(lua_State* state, int index) =>
                lua_isinteger(state, index) ? (object)ToInteger(state, index) : ToNumber(state, index);
        }

        internal bool ToBoolean(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.Boolean);

            return lua_toboolean(state, index);
        }

        internal long ToInteger(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.Number);
            Debug.Assert(lua_isinteger(state, index));

            return lua_tointeger(state, index);
        }

        internal double ToNumber(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.Number);
            Debug.Assert(lua_isnumber(state, index));

            return lua_tonumber(state, index);
        }

        internal string ToString(lua_State* state, int index)
        {
            Debug.Assert(lua_type(state, index) == LuaType.String);

            UIntPtr len;
            var buffer = lua_tolstring(state, index, &len);

            return Encoding.GetString(buffer, (int)len);
        }

        internal LuaObject ToLuaObject(lua_State* state, int index, LuaType? typeHint = null)
        {
            var ptr = (IntPtr)lua_topointer(state, index);
            if (_luaObjects.TryGetValue(ptr, out var weakLuaObject) && weakLuaObject.TryGetTarget(out var luaObject))
            {
                return luaObject;
            }

            // The Lua object was either never created, or it has been garbage collected.
            lua_pushvalue(state, index);
            var reference = luaL_ref(state, LUA_REGISTRYINDEX);

            luaObject = (typeHint ?? lua_type(state, index)) switch
            {
                LuaType.Table => new LuaTable(this, reference, _state),
                LuaType.Function => new LuaFunction(this, reference, _state),
                LuaType.Thread => new LuaThread(this, reference, (lua_State*)ptr),
                _ => throw new InvalidOperationException()
            };

            // If the Lua object was garbage collected, then we can reuse the weak reference.
            if (weakLuaObject != null)
            {
                weakLuaObject.SetTarget(luaObject);
            }
            else
            {
                _luaObjects[ptr] = new WeakReference<LuaObject>(luaObject);
            }

            _references[ptr] = reference;
            return luaObject;
        }

        // Throws an exception if the Lua stack cannot hold a number of extra slots.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckStack(lua_State* state, int slots)
        {
            Debug.Assert(slots >= 1);

            if (!lua_checkstack(state, slots))
            {
                throw new LuaException("Not enough Lua stack space");
            }
        }

        internal LuaException ToLuaException(lua_State* state)
        {
            var errorMessage = ToString(state, -1);
            lua_pop(state, 1);
            return new LuaException(errorMessage);
        }

        private void LoadStringInternal(string s)
        {
            if (LoadString(s) != LuaStatus.Ok)
            {
                throw ToLuaException(_state);
            }

            LuaStatus LoadString(string s)
            {
                // If the maximum byte length is small enough, then we'll use the string buffer. Otherwise, we'll have
                // to resort to an allocation.
                var maxByteLength = Encoding.GetMaxByteCount(s.Length) + 1;  // Need room for a null terminator.
                var useBuffer = maxByteLength <= StringBufferSize;

                byte* buffer = useBuffer ? _stringBuffer : (byte*)Marshal.AllocHGlobal(maxByteLength);

                try
                {
                    var byteLength = 0;
                    fixed (char* sPtr = s)
                    {
                        byteLength = Encoding.GetBytes(sPtr, s.Length, buffer, maxByteLength);
                    }
                    buffer[byteLength] = 0;  // Write the null terminator.

                    return luaL_loadstring(_state, buffer);
                }
                finally
                {
                    if (!useBuffer)
                    {
                        Marshal.FreeHGlobal((IntPtr)buffer);
                    }
                }
            }
        }

        private void DisposeUnmanaged()
        {
            if (!_isDisposed)
            {
                lua_close(_state);
                Marshal.FreeHGlobal((IntPtr)_stringBuffer);
                _isDisposed = true;
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
