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

namespace Triton
{
    /// <summary>
    /// Specifies the type of a Lua result.
    /// </summary>
    public enum LuaResultType
    {
        /// <summary>
        /// Indicates a <see langword="nil"/> result.
        /// </summary>
        Nil,

        /// <summary>
        /// Indicates a boolean result.
        /// </summary>
        Boolean,

        /// <summary>
        /// Indicates an integer result.
        /// </summary>
        Integer,

        /// <summary>
        /// Indicates a number result.
        /// </summary>
        Number,

        /// <summary>
        /// Indicates a string result.
        /// </summary>
        String,

        /// <summary>
        /// Indicates a table result.
        /// </summary>
        Table,

        /// <summary>
        /// Indicates a function result.
        /// </summary>
        Function,

        /// <summary>
        /// Indicates a CLR object result.
        /// </summary>
        ClrObject,

        /// <summary>
        /// Indicates a thread result.
        /// </summary>
        Thread,

        /// <summary>
        /// Indicates a CLR types result.
        /// </summary>
        ClrTypes
    }
}
