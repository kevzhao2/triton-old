// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Xunit;

namespace Triton.Interop
{
    public class NestedTypeTests
    {
        public class Class
        {
            public static class NestedClass
            {
                public static int Value;
            }
        }

        [Fact]
        public void Test()
        {
            using var environment = new LuaEnvironment();
            environment["Class"] = LuaValue.FromClrTypes(typeof(Class));

            environment.Eval("Class.NestedClass.Value = 1234");
            environment.Eval("assert(Class.NestedClass.Value == 1234)");
        }
    }
}
