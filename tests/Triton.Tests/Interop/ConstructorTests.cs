// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Xunit;

namespace Triton.Interop
{
    public class ConstructorTests
    {
        public class GenericClass<T>
        {
            public GenericClass(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }

        [Fact]
        public void GenericClass_Ctor()
        {
            using var environment = new LuaEnvironment();
            environment["String"] = LuaValue.FromClrType(typeof(string));
            environment["GenericClass"] = LuaValue.FromGenericClrTypes(typeof(GenericClass<>));

            environment.Eval("gc = GenericClass[String]('test')");

            Assert.Equal("test", ((GenericClass<string>)environment["gc"].AsClrObject()).Value);
        }
    }
}
