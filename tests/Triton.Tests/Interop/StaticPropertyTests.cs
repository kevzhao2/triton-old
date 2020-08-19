// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Triton.Interop
{
    public class StaticPropertyTests
    {
        public class BoolProperty
        {
            public static bool Value { get; set; }
        }

        public class NonWritableProperty
        {
            public static int Value => 1234;
        }

        public class ByrefLikeProperty
        {
            public static Span<byte> Value => default;
        }

        [Fact]
        public void Bool_Get()
        {
            using var environment = new LuaEnvironment();
            environment["BoolProperty"] = LuaValue.FromClrType(typeof(BoolProperty));

            BoolProperty.Value = true;

            environment.Eval("assert(BoolProperty.Value)");
        }

        [Fact]
        public void Bool_Set()
        {
            using var environment = new LuaEnvironment();
            environment["BoolProperty"] = LuaValue.FromClrType(typeof(BoolProperty));

            environment.Eval("BoolProperty.Value = true");

            Assert.True(BoolProperty.Value);
        }

        [Fact]
        public void RefLikeStruct_Get_RaisesError()
        {
            using var environment = new LuaEnvironment();
            environment["ByrefLikeProperty"] = LuaValue.FromClrType(typeof(ByrefLikeProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("_ = ByrefLikeProperty.Value"));
            Assert.Contains("attempt to get byref-like property 'Value'", ex.Message);
        }

        [Fact]
        public void NonWritable_Set_RaisesError()
        {
            using var environment = new LuaEnvironment();
            environment["NonWritableProperty"] = LuaValue.FromClrType(typeof(NonWritableProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("NonWritableProperty.Value = 1"));
            Assert.Contains("attempt to set non-writable property 'Value'", ex.Message);
        }
    }
}
