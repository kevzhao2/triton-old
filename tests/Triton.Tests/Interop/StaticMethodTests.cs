// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace Triton.Interop
{
    public class StaticMethodTests
    {
        public class GenericMethod
        {
            public static T Identity<T>(T value) => value;
        }

        public class ParamsMethod
        {
            public static int Sum(params int[] values) => values.Sum();
        }

        [Fact]
        public void GenericMethod_OneArg()
        {
            using var environment = new LuaEnvironment();
            environment["Int32"] = LuaValue.FromClrType(typeof(int));
            environment["GenericMethod"] = LuaValue.FromClrType(typeof(GenericMethod));

            environment.Eval("assert(GenericMethod.Identity[Int32](1234) == 1234)");
        }

        [Fact]
        public void ParamsMethod_NoArgs()
        {
            using var environment = new LuaEnvironment();
            environment["ParamsMethod"] = LuaValue.FromClrType(typeof(ParamsMethod));

            environment.Eval("assert(ParamsMethod.Sum() == 0)");
        }

        [Fact]
        public void ParamsMethod_OneArg()
        {
            using var environment = new LuaEnvironment();
            environment["ParamsMethod"] = LuaValue.FromClrType(typeof(ParamsMethod));

            environment.Eval("assert(ParamsMethod.Sum(1) == 1)");
        }

        [Fact]
        public void ParamsMethod_MultipleArgs()
        {
            using var environment = new LuaEnvironment();
            environment["ParamsMethod"] = LuaValue.FromClrType(typeof(ParamsMethod));

            environment.Eval("assert(ParamsMethod.Sum(1, 2) == 3)");
            environment.Eval("assert(ParamsMethod.Sum(1, 2, 3) == 6)");
        }
    }
}
