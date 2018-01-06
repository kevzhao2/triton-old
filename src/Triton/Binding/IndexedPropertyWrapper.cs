// Copyright (c) 2018 Kevin Zhao
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
using System.Reflection;
using Triton.Interop;

namespace Triton.Binding {
    /// <summary>
    /// A wrapper class that exposes indexed properties to Lua.
    /// </summary>
    internal sealed class IndexedPropertyWrapper {
        private readonly object _obj;
        private readonly PropertyInfo _property;
        private readonly IntPtr _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedPropertyWrapper"/> class for the given Lua state, object, and property.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The object.</param>
        /// <param name="property">The property.</param>
        public IndexedPropertyWrapper(IntPtr state, object obj, PropertyInfo property) {
            _state = state;
            _obj = obj;
            _property = property;
        }

        /// <summary>
        /// Gets the value of the property at the given indices.
        /// </summary>
        /// <param name="indices">The indices.</param>
        /// <returns>The value.</returns>
        public object Get(params object[] indices) {
            if (_property.GetGetMethod() == null) {
                throw LuaApi.Error(_state, "attempt to get indexed property without getter");
            }
            if (ObjectBinder.TryCoerce(indices, _property.GetIndexParameters(), out indices) == int.MinValue) {
                throw LuaApi.Error(_state, "attempt to get indexed property with invalid indices");
            }

            try {
                return _property.GetValue(_obj, indices);
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(_state, $"attempt to get indexed property threw:\n{e.InnerException}");
            }
        }

        /// <summary>
        /// Sets the value of the property at the given indices.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="indices">The indices.</param>
        public void Set(object value, params object[] indices) {
            if (_property.GetSetMethod() == null) {
                throw LuaApi.Error(_state, "attempt to set indexed property without setter");
            }
            if (!value.TryCoerce(_property.PropertyType, out value)) {
                throw LuaApi.Error(_state, "attempt to set indexed property with invalid value");
            }
            if (ObjectBinder.TryCoerce(indices, _property.GetIndexParameters(), out indices) == int.MinValue) {
                throw LuaApi.Error(_state, "attempt to set indexed property with invalid indices");
            }

            try {
                _property.SetValue(_obj, value, indices);
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(_state, $"attempt to set indexed property threw:\n{e.InnerException}");
            }
        }
    }
}
