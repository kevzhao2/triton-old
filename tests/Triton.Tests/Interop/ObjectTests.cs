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
using Xunit;

namespace Triton.Interop
{
    public class ObjectTests
    {
        public class MyClass
        {
            public byte ByteField;
            public sbyte SByteField;
            public short ShortField;
            public ushort UShortField;
            public int IntField;
            public uint UIntField;
            public long LongField;
            public ulong ULongField;
        }

        public struct MyStruct
        {
            public byte ByteField;
            public sbyte SByteField;
            public short ShortField;
            public ushort UShortField;
            public int IntField;
            public uint UIntField;
            public long LongField;
            public ulong ULongField;
        }

        [Fact]
        public void InstanceField_Get_Class()
        {
            var obj = new MyClass
            {
                ByteField = 12,
                SByteField = -12,
                ShortField = -1234,
                UShortField = 1234,
                IntField = -12345678,
                UIntField = 12345678,
                LongField = -12345678910,
                ULongField = 12345678910
            };

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            environment.Eval("assert(obj.ByteField == 12)");
            environment.Eval("assert(obj.SByteField == -12)");
            environment.Eval("assert(obj.ShortField == -1234)");
            environment.Eval("assert(obj.UShortField == 1234)");
            environment.Eval("assert(obj.IntField == -12345678)");
            environment.Eval("assert(obj.UIntField == 12345678)");
            environment.Eval("assert(obj.LongField == -12345678910)");
            environment.Eval("assert(obj.ULongField == 12345678910)");
        }

        [Fact]
        public void InstanceField_Get_Struct()
        {
            var obj = new MyStruct
            {
                ByteField = 12,
                SByteField = -12,
                ShortField = -1234,
                UShortField = 1234,
                IntField = -12345678,
                UIntField = 12345678,
                LongField = -12345678910,
                ULongField = 12345678910
            };

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            environment.Eval("assert(obj.ByteField == 12)");
            environment.Eval("assert(obj.SByteField == -12)");
            environment.Eval("assert(obj.ShortField == -1234)");
            environment.Eval("assert(obj.UShortField == 1234)");
            environment.Eval("assert(obj.IntField == -12345678)");
            environment.Eval("assert(obj.UIntField == 12345678)");
            environment.Eval("assert(obj.LongField == -12345678910)");
            environment.Eval("assert(obj.ULongField == 12345678910)");
        }
    }
}
