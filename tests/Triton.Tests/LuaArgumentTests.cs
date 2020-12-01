// Copyright (c) 2020 Kevin Zhao
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Triton
{
    public class LuaArgumentTests
    {
        [Fact]
        public void Nil_Get()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.Nil);

            environment.Eval("assert(test == nil)");
        }

        [Fact]
        public void FromObject()
        {
            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(null));

                environment.Eval("assert(test == nil)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(true));

                environment.Eval("assert(test)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject((sbyte)123));

                environment.Eval("assert(test == 123)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject((byte)123));

                environment.Eval("assert(test == 123)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject((short)12345));

                environment.Eval("assert(test == 12345)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject((ushort)12345));

                environment.Eval("assert(test == 12345)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(123456789));

                environment.Eval("assert(test == 123456789)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(123456789U));

                environment.Eval("assert(test == 123456789)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(1234567891011121314L));

                environment.Eval("assert(test == 1234567891011121314)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(1234567891011121314UL));

                environment.Eval("assert(test == 1234567891011121314)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(1.234f));

                environment.Eval("assert(test == 1.2339999675750732)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(1.234));

                environment.Eval("assert(test == 1.234)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject('a'));

                environment.Eval("assert(test == 'a')");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject("test"));

                environment.Eval("assert(test == 'test')");
            }

            {
                using var environment = new LuaEnvironment();
                using var table = (LuaTable)environment.Eval("table = {}; return table");
                environment.SetGlobal("test", LuaArgument.FromObject(table));

                environment.Eval("assert(test == table)");
            }

            {
                using var environment = new LuaEnvironment();
                using var function = (LuaFunction)environment.Eval("func = function() return 1234 end; return func");
                environment.SetGlobal("test", LuaArgument.FromObject(function));

                environment.Eval("assert(test == func)");
            }

            {
                using var environment = new LuaEnvironment();
                using var thread =
                    (LuaThread)environment.Eval("thread = coroutine.create(function() return 1234 end); return thread");
                environment.SetGlobal("test", LuaArgument.FromObject(thread));

                environment.Eval("assert(test == thread)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromObject(new object()));

                environment.Eval("assert(tostring(test) == 'CLR object: System.Object')");
            }
        }
        
        [Fact]
        public void FromBoolean()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.FromBoolean(true));

            environment.Eval("assert(test)");
        }

        [Fact]
        public void FromInteger()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.FromInteger(1234));

            environment.Eval("assert(test == 1234)");
        }

        [Fact]
        public void FromNumber()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.FromNumber(1.234));

            environment.Eval("assert(test == 1.234)");
        }

        [Fact]
        public void FromString_NullString_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaArgument.FromString(null!));
        }

        [Fact]
        public void FromString()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.FromString("test"));

            environment.Eval("assert(test == 'test')");
        }

        [Fact]
        public void FromTable_NullTable_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaArgument.FromTable(null!));
        }

        [Fact]
        public void FromTable()
        {
            using var environment = new LuaEnvironment();
            using var table = (LuaTable)environment.Eval("table = {}; return table");
            environment.SetGlobal("test", LuaArgument.FromTable(table));

            environment.Eval("assert(test == table)");
        }

        [Fact]
        public void FromFunction_NullFunction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaArgument.FromFunction(null!));
        }

        [Fact]
        public void FromFunction()
        {
            using var environment = new LuaEnvironment();
            using var function = (LuaFunction)environment.Eval("func = function() return 1234 end; return func");
            environment.SetGlobal("test", LuaArgument.FromFunction(function));

            environment.Eval("assert(test == func)");
        }

        [Fact]
        public void FromThread_NullThread_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaArgument.FromThread(null!));
        }

        [Fact]
        public void FromThread()
        {
            using var environment = new LuaEnvironment();
            using var thread =
                (LuaThread)environment.Eval("thread = coroutine.create(function() return 1234 end); return thread");
            environment.SetGlobal("test", LuaArgument.FromThread(thread));

            environment.Eval("assert(test == thread)");
        }

        [Fact]
        public void FromClrObject_NullObj_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaArgument.FromClrObject(null!));
        }

        [Fact]
        public void FromClrObject()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.FromObject(new object()));

            environment.Eval("assert(tostring(test) == 'CLR object: System.Object')");
        }

        [Fact]
        public void FromClrTypes_NullTypes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LuaArgument.FromClrTypes(null!));
        }

        [Fact]
        public void FromClrTypes_EmptyTypes_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LuaArgument.FromClrTypes());
        }

        [Fact]
        public void FromClrTypes_TypesContainsNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LuaArgument.FromClrTypes(null!, null!));
        }

        [Fact]
        public void FromClrTypes_TypesContainsTwoTypesWithTheSameGenericArity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => LuaArgument.FromClrTypes(typeof(object), typeof(object)));
        }

        [Fact]
        public void FromClrTypes()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", LuaArgument.FromClrTypes(typeof(object)));

            environment.Eval("assert(tostring(test) == 'CLR type: System.Object')");
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_Bool()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", true);

            environment.Eval("assert(test)");
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_Long()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", 1234);

            environment.Eval("assert(test == 1234)");
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_Double()
        {
            using var environment = new LuaEnvironment();
            environment.SetGlobal("test", 1.234);

            environment.Eval("assert(test == 1.234)");
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_String()
        {
            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", "test");

                environment.Eval("assert(test == 'test')");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", (string?)null);

                environment.Eval("assert(test == nil)");
            }
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_LuaTable()
        {
            {
                using var environment = new LuaEnvironment();
                using var table = (LuaTable)environment.Eval("table = {}; return table");
                environment.SetGlobal("test", table);

                environment.Eval("assert(test == table)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", (LuaTable?)null);

                environment.Eval("assert(test == nil)");
            }
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_LuaFunction()
        {
            {
                using var environment = new LuaEnvironment();
                using var function = (LuaFunction)environment.Eval("func = function() return 1234 end; return func");
                environment.SetGlobal("test", function);

                environment.Eval("assert(test == func)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", (LuaFunction?)null);

                environment.Eval("assert(test == nil)");
            }
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Implicit_LuaThread()
        {
            {
                using var environment = new LuaEnvironment();
                using var thread =
                    (LuaThread)environment.Eval("thread = coroutine.create(function() return 1234 end); return thread");
                environment.SetGlobal("test", thread);

                environment.Eval("assert(test == thread)");
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", (LuaThread?)null);

                environment.Eval("assert(test == nil)");
            }
        }
    }
}
