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
    public class LuaResultsTests
    {
        [Fact]
        public void IsXxx_Properties()
        {
            {
                LuaResults result = default;

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return");

                Assert.True(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return nil");

                Assert.True(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return true");

                Assert.False(result.IsNil);
                Assert.True(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 1234");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.True(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 1.234");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.True(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 'test'");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.True(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return {}");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.True(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return function() return 1234 end");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.True(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return coroutine.create(function() return 1234 end)");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.True(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromClrObject(new()));
                var result = environment.Eval("return test");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.True(result.IsClrObject);
                Assert.False(result.IsClrTypes);
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromClrTypes(typeof(object)));
                var result = environment.Eval("return test");

                Assert.False(result.IsNil);
                Assert.False(result.IsBoolean);
                Assert.False(result.IsInteger);
                Assert.False(result.IsNumber);
                Assert.False(result.IsString);
                Assert.False(result.IsTable);
                Assert.False(result.IsFunction);
                Assert.False(result.IsThread);
                Assert.False(result.IsClrObject);
                Assert.True(result.IsClrTypes);
            }
        }

        [Fact]
        public void Deconstruct_Overloads()
        {
            {
                using var environment = new LuaEnvironment();
                var (result, result2) = environment.Eval("return 1, 2");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
            }

            {
                using var environment = new LuaEnvironment();
                var (result, result2, result3) = environment.Eval("return 1, 2, 3");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
                Assert.Equal(3, (long)result3);
            }

            {
                using var environment = new LuaEnvironment();
                var (result, result2, result3, result4) = environment.Eval("return 1, 2, 3, 4");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
                Assert.Equal(3, (long)result3);
                Assert.Equal(4, (long)result4);
            }

            {
                using var environment = new LuaEnvironment();
                var (result, result2, result3, result4, result5) = environment.Eval("return 1, 2, 3, 4, 5");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
                Assert.Equal(3, (long)result3);
                Assert.Equal(4, (long)result4);
                Assert.Equal(5, (long)result5);
            }

            {
                using var environment = new LuaEnvironment();
                var (result, result2, result3, result4, result5, result6) = environment.Eval("return 1, 2, 3, 4, 5, 6");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
                Assert.Equal(3, (long)result3);
                Assert.Equal(4, (long)result4);
                Assert.Equal(5, (long)result5);
                Assert.Equal(6, (long)result6);
            }

            {
                using var environment = new LuaEnvironment();
                var (result, result2, result3, result4, result5, result6, result7) =
                    environment.Eval("return 1, 2, 3, 4, 5, 6, 7");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
                Assert.Equal(3, (long)result3);
                Assert.Equal(4, (long)result4);
                Assert.Equal(5, (long)result5);
                Assert.Equal(6, (long)result6);
                Assert.Equal(7, (long)result7);
            }

            {
                using var environment = new LuaEnvironment();
                var (result, result2, result3, result4, result5, result6, result7, result8) =
                    environment.Eval("return 1, 2, 3, 4, 5, 6, 7, 8");

                Assert.Equal(1, (long)result);
                Assert.Equal(2, (long)result2);
                Assert.Equal(3, (long)result3);
                Assert.Equal(4, (long)result4);
                Assert.Equal(5, (long)result5);
                Assert.Equal(6, (long)result6);
                Assert.Equal(7, (long)result7);
                Assert.Equal(8, (long)result8);
            }
        }

        [Fact]
        public void ToXxx_Methods()
        {
            {
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToBoolean();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return");

                Assert.False(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return");

                Assert.False(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return true");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 1234");

                Assert.True(result.ToBoolean());

                Assert.Equal(1234, result.ToInteger());

                Assert.Equal(1234, result.ToNumber());

                Assert.Equal("1234", result.ToString());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 1.234");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return result.ToInteger();
                });

                Assert.Equal(1.234, result.ToNumber());

                Assert.Equal("1.234", result.ToString());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 'test'");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToNumber();
                });

                Assert.Equal("test", result.ToString());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                using var table = environment.CreateTable();
                environment.SetGlobal("test", table);
                var result = environment.Eval("return test");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToString();
                });

                using var table2 = result.ToTable();
                table2.SetValue("test", 1234);
                Assert.Equal(1234, (long)table.GetValue("test"));

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                using var function = environment.CreateFunction("return 1234");
                environment.SetGlobal("test", function);
                var result = environment.Eval("return test");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToTable();
                });

                using var function2 = result.ToFunction();
                Assert.Equal(1234, (long)function2.Call());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                using var function = environment.CreateFunction("return 1234");
                using var thread = environment.CreateThread();
                thread.SetFunction(function);
                environment.SetGlobal("test", thread);
                var result = environment.Eval("return test");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToFunction();
                });

                using var thread2 = result.ToThread();
                Assert.Equal(1234, (long)thread2.Resume());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrObject();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrTypes();
                });
            }

            {
                var obj = new object();
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromClrObject(obj));
                var result = environment.Eval("return test");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToThread();
                });

                Assert.Same(obj, result.ToClrObject());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrTypes();
                });
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromClrTypes(typeof(object)));
                var result = environment.Eval("return test");

                Assert.True(result.ToBoolean());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToInteger();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToNumber();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToString();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToTable();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToFunction();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToThread();
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return result.ToClrObject();
                });

                Assert.Equal(new[] { typeof(object) }, result.ToClrTypes());
            }
        }

        [Fact]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Operator naming")]
        public void op_Explicit_Operators()
        {
            {
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (bool)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    LuaResults result = default;
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return");

                Assert.False((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return");

                Assert.False((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return nil");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return true");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return true");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 1234");

                Assert.True((bool)result);

                Assert.Equal(1234, (long)result);

                Assert.Equal(1234, (double)result);

                Assert.Equal("1234", (string)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1234");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 1.234");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return (long)result;
                });

                Assert.Equal(1.234, (double)result);

                Assert.Equal("1.234", (string)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 1.234");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                var result = environment.Eval("return 'test'");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return (double)result;
                });

                Assert.Equal("test", (string)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return 'test'");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                using var table = environment.CreateTable();
                environment.SetGlobal("test", table);
                var result = environment.Eval("return test");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (string)result;
                });

                using var table2 = (LuaTable)result;
                table2.SetValue("test", 1234);
                Assert.Equal(1234, (long)table.GetValue("test"));

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                using var function = environment.CreateFunction("return 1234");
                environment.SetGlobal("test", function);
                var result = environment.Eval("return test");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaTable)result;
                });

                using var function2 = (LuaFunction)result;
                Assert.Equal(1234, (long)function2.Call());

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                using var function = environment.CreateFunction("return 1234");
                using var thread = environment.CreateThread();
                thread.SetFunction(function);
                environment.SetGlobal("test", thread);
                var result = environment.Eval("return test");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaFunction)result;
                });

                using var thread2 = (LuaThread)result;
                Assert.Equal(1234, (long)thread2.Resume());
            }

            {
                var obj = new object();
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromClrObject(obj));
                var result = environment.Eval("return test");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaThread)result;
                });
            }

            {
                using var environment = new LuaEnvironment();
                environment.SetGlobal("test", LuaArgument.FromClrTypes(typeof(object)));
                var result = environment.Eval("return test");

                Assert.True((bool)result);

                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (long)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (double)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (string)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaTable)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaFunction)result;
                });
                Assert.Throws<InvalidCastException>(() =>
                {
                    var result = environment.Eval("return test");
                    return (LuaThread)result;
                });
            }
        }
    }
}
