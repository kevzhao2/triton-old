// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Xunit;

namespace Triton.Interop
{
    public class ArrayTests
    {
        [Fact]
        public void Int_Get()
        {
            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(new int[] { 1, 2, 3, 4 });

            environment.Eval("assert(array[0] == 1)");
            environment.Eval("assert(array[1] == 2)");
            environment.Eval("assert(array[2] == 3)");
            environment.Eval("assert(array[3] == 4)");
        }

        [Fact]
        public void Int_Set()
        {
            var array = new int[4];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[0] = 1");
            environment.Eval("array[1] = 2");
            environment.Eval("array[2] = 3");
            environment.Eval("array[3] = 4");

            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);
            Assert.Equal(4, array[3]);
        }

        [Fact]
        public void Int_MultiDimensional_Get()
        {
            var array = new int[10, 10];
            array[1, 2] = 34;

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("assert(array[{1, 2}] == 34)");
        }

        [Fact]
        public void Int_MultiDimensional_Set()
        {
            var array = new int[10, 10];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[{1, 2}] = 34");

            Assert.Equal(34, array[1, 2]);
        }
    }
}
