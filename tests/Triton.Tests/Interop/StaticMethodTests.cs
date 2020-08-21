// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace Triton.Interop
{
    public class StaticMethodTests
    {
        public class ParamsMethod
        {
            public static int Value;

            public static void DoWork(params int[] values) => Value = values.Sum();
        }

        [Fact]
        public void ParamsMethod_NoArgs()
        {
            using var environment = new LuaEnvironment();
            environment["ParamsMethod"] = LuaValue.FromClrType(typeof(ParamsMethod));

            environment.Eval("ParamsMethod.DoWork()");

            Assert.Equal(0, ParamsMethod.Value);
        }

        [Fact]
        public void ParamsMethod_OneArg()
        {
            using var environment = new LuaEnvironment();
            environment["ParamsMethod"] = LuaValue.FromClrType(typeof(ParamsMethod));

            environment.Eval("ParamsMethod.DoWork(1234)");

            Assert.Equal(1234, ParamsMethod.Value);
        }

        [Fact]
        public void ParamsMethod_TwoArgs()
        {
            using var environment = new LuaEnvironment();
            environment["ParamsMethod"] = LuaValue.FromClrType(typeof(ParamsMethod));

            environment.Eval("ParamsMethod.DoWork(1234, 5678)");

            Assert.Equal(6912, ParamsMethod.Value);
        }
    }
}
