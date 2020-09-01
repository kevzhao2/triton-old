// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Triton.Interop
{
    public class ArrayTests
    {
        [Fact]
        public void Get_SzArray()
        {
            var array = new[] { 1, 2, 3, 4 };

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("assert(array[1] == 2)");
        }

        [Fact]
        public void Get_NonSzArray_SingleDimensional()
        {
            var array = Array.CreateInstance(typeof(int), new[] { 4 }, new[] { 1 });
            array.SetValue(2, 1);

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("assert(array[1] == 2)");
        }

        [Fact]
        public void Get_NonSzArray_MultiDimensional()
        {
            var array = new int[4, 4];
            array[1, 2] = 3;

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("assert(array[{1, 2}] == 3)");
        }

        [Fact]
        public void Set_SzArray()
        {
            var array = new int[4];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[1] = 2");

            Assert.Equal(2, array[1]);
        }

        [Fact]
        public void Set_NonSzArray_SingleDimensional()
        {
            var array = Array.CreateInstance(typeof(int), new[] { 4 }, new[] { 1 });

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[1] = 2");

            Assert.Equal(2, array.GetValue(1));
        }

        [Fact]
        public void Set_NonSzArray_MultiDimensional()
        {
            var array = new int[4, 4];

            using var environment = new LuaEnvironment();
            environment["array"] = LuaValue.FromClrObject(array);

            environment.Eval("array[{1, 2}] = 3");

            Assert.Equal(3, array[1, 2]);
        }
    }
}
