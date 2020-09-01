// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Triton.Interop
{
    public class DelegateTests
    {
        [Fact]
        public void Call()
        {
            using var environment = new LuaEnvironment();
            environment["square"] = LuaValue.FromClrObject(new Func<int, int>(x => x * x));

            environment.Eval("assert(square(4) == 16)");
        }
    }
}
