// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
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
    }
}
