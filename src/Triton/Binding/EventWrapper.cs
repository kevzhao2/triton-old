﻿// Copyright (c) 2018 Kevin Zhao
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
using System.Reflection;
using Triton.Interop;

namespace Triton.Binding {
    /// <summary>
    /// A wrapper class that exposes events to Lua.
    /// </summary>
    internal sealed class EventWrapper {
        private readonly Dictionary<LuaFunction, Delegate> _delegates = new Dictionary<LuaFunction, Delegate>();
        private readonly EventInfo _event;
        private readonly object _obj;
        private readonly IntPtr _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventWrapper"/> class for the given Lua state, object, and event.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="obj">The object.</param>
        /// <param name="event">The event.</param>
        public EventWrapper(IntPtr state, object obj, EventInfo @event) {
            _state = state;
            _obj = obj;
            _event = @event;
        }

        /// <summary>
        /// Adds a <see cref="LuaFunction"/> to the event.
        /// </summary>
        /// <param name="function">The <see cref="LuaFunction"/>.</param>
        public void Add(LuaFunction function) {
            if (function == null) {
                throw LuaApi.Error(_state, "attempt to add nil to event");
            }

            Delegate @delegate;

            try {
#if NETCORE
                var invokeMethod = typeof(LuaFunctionWrapper).GetTypeInfo().GetDeclaredMethod("Invoke");
                @delegate = invokeMethod.CreateDelegate(_event.EventHandlerType, new LuaFunctionWrapper(function));
#else
                @delegate = Delegate.CreateDelegate(_event.EventHandlerType, new LuaFunctionWrapper(function), "Invoke");
#endif
                _event.AddEventHandler(_obj, @delegate);
            } catch (ArgumentException) {
                throw LuaApi.Error(_state, "attempt to add to non-EventHandler event");
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(_state, $"attempt to add to event threw:\n{e.InnerException}");
            }

            _delegates[function] = @delegate;
        }

        /// <summary>
        /// Removes a <see cref="LuaFunction"/> from the event.
        /// </summary>
        /// <param name="function">The <see cref="LuaFunction"/>.</param>
        public void Remove(LuaFunction function) {
            if (function == null) {
                throw LuaApi.Error(_state, "attempt to remove nil from event");
            }
            if (!_delegates.TryGetValue(function, out var @delegate)) {
                return;
            }

            try {
                _event.RemoveEventHandler(_obj, @delegate);
            } catch (TargetInvocationException e) {
                throw LuaApi.Error(_state, $"attempt to remove from event threw:\n{e.InnerException}");
            }

            _delegates.Remove(function);
        }

        /// <summary>
        /// A wrapper class that forms a closure around a <see cref="LuaFunction"/>.
        /// </summary>
        private sealed class LuaFunctionWrapper {
            private readonly LuaFunction _function;

            /// <summary>
            /// Initializes a new instance of the <see cref="LuaFunctionWrapper"/> class around the given <see cref="LuaFunction"/>.
            /// </summary>
            /// <param name="function">The <see cref="LuaFunction"/>.</param>
            public LuaFunctionWrapper(LuaFunction function) => _function = function;

            /// <summary>
            /// Invokes the wrapped <see cref="LuaFunction"/> with the given arguments.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="args">The event arguments.</param>
            public void Invoke(object sender, EventArgs args) => _function.Call(sender, args);
        }
    }
}