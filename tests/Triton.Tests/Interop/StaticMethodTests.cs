// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Triton.Interop
{
    public class StaticMethodTests
    {
        public class VoidMethod
        {
            public static int Value;

            public static void DoWork() => Value = 1234;
        }

        public class OverloadedMethods
        {
            public static int Value;

            public static void DoWork() => Value = 1234;

            public static void DoWork(int value) => Value = value;
        }

        [Fact]
        public void VoidMethod_Call()
        {
            using var environment = new LuaEnvironment();
            environment["VoidMethod"] = LuaValue.FromClrType(typeof(VoidMethod));

            environment.Eval("VoidMethod.DoWork()");

            Assert.Equal(1234, VoidMethod.Value);
        }

        [Fact]
        public void OverloadedMethods_Call()
        {
            using var environment = new LuaEnvironment();
            environment["OverloadedMethods"] = LuaValue.FromClrType(typeof(OverloadedMethods));

            environment.Eval("OverloadedMethods.DoWork()");

            Assert.Equal(1234, OverloadedMethods.Value);

            environment.Eval("OverloadedMethods.DoWork(1111)");

            Assert.Equal(1111, OverloadedMethods.Value);
        }
    }
}
