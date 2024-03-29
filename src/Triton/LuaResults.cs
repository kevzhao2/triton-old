﻿// Copyright (c) 2020 Kevin Zhao
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Triton.NativeMethods;

namespace Triton
{
    /// <summary>
    /// Represents multiple Lua results.
    /// </summary>
    /// <remarks>
    /// This structure is ephemeral and is invalidated immediately after calling another Lua API. It is marked as a
    /// <see langword="ref struct"/> to reduce the potential for errors.
    /// </remarks>
    [DebuggerDisplay("{ToDebugString(),nq}")]
    [DebuggerStepThrough]
    public unsafe readonly ref struct LuaResults
    {
        // Keep the Lua state.
        //
        // The `LuaResults` structure can lazily represent up to 8 Lua results using just eight bytes, making it quite
        // efficient. The limitation of not being able to represent more than eight results is very unlikely to have a
        // real-world impact.
        //
        private readonly lua_State* _state;

        internal LuaResults(lua_State* state)
        {
            _state = state;
        }

        #region IsXxx properties

        /// <inheritdoc cref="LuaResult.IsNil"/>
        public bool IsNil => ((LuaResult)this).IsNil;

        /// <inheritdoc cref="LuaResult.IsBoolean"/>
        public bool IsBoolean => ((LuaResult)this).IsBoolean;

        /// <inheritdoc cref="LuaResult.IsInteger"/>
        public bool IsInteger => ((LuaResult)this).IsInteger;

        /// <inheritdoc cref="LuaResult.IsNumber"/>
        public bool IsNumber => ((LuaResult)this).IsNumber;

        /// <inheritdoc cref="LuaResult.IsString"/>
        public bool IsString => ((LuaResult)this).IsString;

        /// <inheritdoc cref="LuaResult.IsTable"/>
        public bool IsTable => ((LuaResult)this).IsTable;

        /// <inheritdoc cref="LuaResult.IsFunction"/>
        public bool IsFunction => ((LuaResult)this).IsFunction;

        /// <inheritdoc cref="LuaResult.IsThread"/>
        public bool IsThread => ((LuaResult)this).IsThread;

        /// <inheritdoc cref="LuaResult.IsClrObject"/>
        public bool IsClrObject => ((LuaResult)this).IsClrObject;

        /// <inheritdoc cref="LuaResult.IsClrTypes"/>
        public bool IsClrTypes => ((LuaResult)this).IsClrTypes;

        #endregion

        #region Deconstruct(...) overloads

        /// <summary>
        /// Deconstructs two results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
        }

        /// <summary>
        /// Deconstructs three results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2,
            out LuaResult result3)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
            result3 = new(state, 3);
        }

        /// <summary>
        /// Deconstructs four results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="result4">The fourth result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2,
            out LuaResult result3,
            out LuaResult result4)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
            result3 = new(state, 3);
            result4 = new(state, 4);
        }

        /// <summary>
        /// Deconstructs five results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="result4">The fourth result.</param>
        /// <param name="result5">The fifth result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2,
            out LuaResult result3,
            out LuaResult result4,
            out LuaResult result5)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
            result3 = new(state, 3);
            result4 = new(state, 4);
            result5 = new(state, 5);
        }

        /// <summary>
        /// Deconstructs six results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="result4">The fourth result.</param>
        /// <param name="result5">The fifth result.</param>
        /// <param name="result6">The sixth result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2,
            out LuaResult result3,
            out LuaResult result4,
            out LuaResult result5,
            out LuaResult result6)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
            result3 = new(state, 3);
            result4 = new(state, 4);
            result5 = new(state, 5);
            result6 = new(state, 6);
        }

        /// <summary>
        /// Deconstructs seven results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="result4">The fourth result.</param>
        /// <param name="result5">The fifth result.</param>
        /// <param name="result6">The sixth result.</param>
        /// <param name="result7">The seventh result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2,
            out LuaResult result3,
            out LuaResult result4,
            out LuaResult result5,
            out LuaResult result6,
            out LuaResult result7)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
            result3 = new(state, 3);
            result4 = new(state, 4);
            result5 = new(state, 5);
            result6 = new(state, 6);
            result7 = new(state, 7);
        }

        /// <summary>
        /// Deconstructs eight results.
        /// </summary>
        /// <param name="result">The first result.</param>
        /// <param name="result2">The second result.</param>
        /// <param name="result3">The third result.</param>
        /// <param name="result4">The fourth result.</param>
        /// <param name="result5">The fifth result.</param>
        /// <param name="result6">The sixth result.</param>
        /// <param name="result7">The seventh result.</param>
        /// <param name="result8">The eighth result.</param>
        public void Deconstruct(
            out LuaResult result,
            out LuaResult result2,
            out LuaResult result3,
            out LuaResult result4,
            out LuaResult result5,
            out LuaResult result6,
            out LuaResult result7,
            out LuaResult result8)
        {
            var state = _state;  // local optimization

            result  = new(state, 1);
            result2 = new(state, 2);
            result3 = new(state, 3);
            result4 = new(state, 4);
            result5 = new(state, 5);
            result6 = new(state, 6);
            result7 = new(state, 7);
            result8 = new(state, 8);
        }

        #endregion

        #region ToXxx() methods

        /// <inheritdoc cref="LuaResult.ToBoolean"/>
        public bool ToBoolean() => ((LuaResult)this).ToBoolean();

        /// <inheritdoc cref="LuaResult.ToInteger"/>
        public long ToInteger() => ((LuaResult)this).ToInteger();

        /// <inheritdoc cref="LuaResult.ToNumber"/>
        public double ToNumber() => ((LuaResult)this).ToNumber();

        /// <inheritdoc cref="LuaResult.ToString"/>
        public new string ToString() => ((LuaResult)this).ToString();

        /// <inheritdoc cref="LuaResult.ToTable"/>
        public LuaTable ToTable() => ((LuaResult)this).ToTable();

        /// <inheritdoc cref="LuaResult.ToFunction"/>
        public LuaFunction ToFunction() => ((LuaResult)this).ToFunction();

        /// <inheritdoc cref="LuaResult.ToThread"/>
        public LuaThread ToThread() => ((LuaResult)this).ToThread();

        /// <inheritdoc cref="LuaResult.ToClrObject"/>
        public object ToClrObject() => ((LuaResult)this).ToClrObject();

        /// <inheritdoc cref="LuaResult.ToClrTypes"/>
        public Type[] ToClrTypes() => ((LuaResult)this).ToClrTypes();

        #endregion

        // Because this method is not on a hot path, it is optimized for readability instead.
        //
        [ExcludeFromCodeCoverage]
        internal string ToDebugString()
        {
            var state = _state;  // local optimization

            return state is null ?
                "<uninitialized>" :
                Math.Min(lua_gettop(state), 9) switch  // show at most eight values
                {
                    0       => "()",
                    1       => ToDebugString(1),
                    var top => $"({string.Join(", ", Enumerable.Range(1, top).Select(ToDebugString))})"
                };

            [ExcludeFromCodeCoverage]
            string ToDebugString(int index) =>
                index < 9 ? new LuaResult(state, index).ToDebugString() : "...";
        }

        /// <summary>
        /// Decays the results into a single <see cref="LuaResult"/>.
        /// </summary>
        /// <param name="results">The Lua results to convert.</param>
        public static implicit operator LuaResult(LuaResults results) => *(LuaResult*)&results;  // reinterpret

        #region explicit operators

        /// <inheritdoc cref="LuaResult.explicit operator bool"/>
        public static explicit operator bool(LuaResults results) => (bool)(LuaResult)results;

        /// <inheritdoc cref="LuaResult.explicit operator long"/>
        public static explicit operator long(LuaResults results) => (long)(LuaResult)results;

        /// <inheritdoc cref="LuaResult.explicit operator double"/>
        public static explicit operator double(LuaResults results) => (double)(LuaResult)results;

        /// <inheritdoc cref="LuaResult.explicit operator string"/>
        public static explicit operator string(LuaResults results) => (string)(LuaResult)results;

        /// <inheritdoc cref="LuaResult.explicit operator LuaTable"/>
        public static explicit operator LuaTable(LuaResults results) => (LuaTable)(LuaResult)results;

        /// <inheritdoc cref="LuaResult.explicit operator LuaFunction"/>
        public static explicit operator LuaFunction(LuaResults results) => (LuaFunction)(LuaResult)results;

        /// <inheritdoc cref="LuaResult.explicit operator LuaThread"/>
        public static explicit operator LuaThread(LuaResults results) => (LuaThread)(LuaResult)results;

        #endregion
    }
}
