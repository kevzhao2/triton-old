// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace Triton.Interop
{
    public class IndexerTests
    {
        [Fact]
        public void Test()
        {
            using var environment = new LuaEnvironment();
            environment["Int32"] = LuaValue.FromClrTypes(typeof(int));
            environment["List"] = LuaValue.FromClrTypes(typeof(List<>));

            environment.Eval("list = List[Int32]()");
            environment.Eval("list:Add(1)");
            environment.Eval("assert(list[0] == 1)");
        }
    }
}
