// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Triton.Interop
{
    public class InstancePropertyTests
    {
        public class BoolProperty
        {
            public bool Value { get; set; }
        }

        public class NonWritableProperty
        {
            public int Value => 1234;
        }

        public class ByrefLikeProperty
        {
            public Span<byte> Value => default;
        }

        [Fact]
        public void Bool_Get()
        {
            var obj = new BoolProperty();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            obj.Value = true;

            environment.Eval("assert(obj.Value)");
        }

        [Fact]
        public void Bool_Set()
        {
            var obj = new BoolProperty();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            environment.Eval("obj.Value = true");

            Assert.True(obj.Value);
        }

        [Fact]
        public void ByrefLike_Get_RaisesError()
        {
            var obj = new ByrefLikeProperty();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("_ = obj.Value"));
            Assert.Contains("attempt to get byref-like property 'Value'", ex.Message);
        }

        [Fact]
        public void NonWritable_Set_RaisesError()
        {
            var obj = new NonWritableProperty();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("obj.Value = 1"));
            Assert.Contains("attempt to set non-writable property 'Value'", ex.Message);
        }
    }
}
