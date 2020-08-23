// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Xunit;

namespace Triton.Interop
{
    public class InstancePropertyTests
    {
        public class IntProperty
        {
            public int Value { get; set; }
        }

        [Fact]
        public void Int_Get()
        {
            var obj = new IntProperty();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            obj.Value = 1234;

            environment.Eval("assert(obj.Value == 1234)");
        }

        [Fact]
        public void Int_Set()
        {
            var obj = new IntProperty();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            environment.Eval("obj.Value = 1234");

            Assert.Equal(1234, obj.Value);
        }
    }
}
