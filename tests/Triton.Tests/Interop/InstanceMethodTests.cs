// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Xunit;

namespace Triton.Interop
{
    public class InstanceMethodTests
    {
        public class VoidMethod
        {
            public int Value;

            public void DoWork() => Value = 1234;
        }

        [Fact]
        public void VoidMethod_Call()
        {
            var obj = new VoidMethod();

            using var environment = new LuaEnvironment();
            environment["obj"] = LuaValue.FromClrObject(obj);

            environment.Eval("obj:DoWork()");

            Assert.Equal(1234, obj.Value);
        }
    }
}
