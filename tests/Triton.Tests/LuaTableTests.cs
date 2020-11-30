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
using Xunit;

namespace Triton
{
    public class LuaTableTests
    {
        [Fact]
        public void Count_Get()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);
            table.SetValue(1, 1234);
            table.SetValue(true, 1234);

            Assert.Equal(3, table.Count);
        }

        [Fact]
        public void Metatable_Get_ReturnsNull()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Null(table.Metatable);
        }

        [Fact]
        public void Metatable_Get_ReturnsNotNull()
        {
            using var environment = new LuaEnvironment();
            using var table = (LuaTable)environment.Eval(@"
                table = {}
                return setmetatable(table, {
                    __index = function(t, index)
                        return 0
                    end
                })");

            Assert.NotNull(table.Metatable);
        }

        [Fact]
        public void Metatable_Set_NullValue()
        {
            using var environment = new LuaEnvironment();
            using var table = (LuaTable)environment.Eval(@"
                table = {}
                return setmetatable(table, {
                    __index = function(t, index)
                        return 0
                    end
                })");

            table.Metatable = null;

            Assert.True(table.GetValue("test").IsNil);
        }

        [Fact]
        public void Metatable_Set_NotNullValue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            using var metatable = environment.CreateTable();
            using var indexMetamethod = environment.CreateFunction("return 0");
            metatable.SetValue("__index", indexMetamethod);

            table.Metatable = metatable;

            Assert.Equal(0, (long)table.GetValue("test"));
        }

        [Fact]
        public void GetValue_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.GetValue(null!));
        }

        [Fact]
        public void SetValue_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.SetValue(null!, 1234));
        }

        [Fact]
        public void SetValue_GetValue_String()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            table.SetValue("test", 1234);

            Assert.Equal(1234, (long)table.GetValue("test"));
        }

        [Fact]
        public void SetValue_GetValue_Integer()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            table.SetValue(1, 1234);

            Assert.Equal(1234, (long)table.GetValue(1));
        }

        [Fact]
        public void SetValue_GetValue_LuaArgument()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            table.SetValue(true, 1234);

            Assert.Equal(1234, (long)table.GetValue(true));
        }

        [Fact]
        public void Add_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.Add(null!, 1234));
        }

        [Fact]
        public void Add_String_DuplicateKey_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);

            Assert.Throws<ArgumentException>(() => table.Add("test", 1234));
        }

        [Fact]
        public void Add_String()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            table.Add("test", 1234);

            Assert.Equal(1234, (long)table.GetValue("test"));
        }

        [Fact]
        public void Add_Long_DuplicateKey_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(1, 1234);

            Assert.Throws<ArgumentException>(() => table.Add(1, 1234));
        }

        [Fact]
        public void Add_Long()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            table.Add(1, 1234);

            Assert.Equal(1234, (long)table.GetValue(1));
        }

        [Fact]
        public void Add_LuaArgument_DuplicateKey_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(true, 1234);

            Assert.Throws<ArgumentException>(() => table.Add(true, 1234));
        }

        [Fact]
        public void Add_LuaArgument()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            table.Add(true, 1234);

            Assert.Equal(1234, (long)table.GetValue(true));
        }

        [Fact]
        public void Clear()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);
            table.SetValue(1, 1234);
            table.SetValue(true, 1234);

            table.Clear();

            Assert.Equal(0, table.Count);
        }

        [Fact]
        public void ContainsKey_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.ContainsKey(null!));
        }

        [Fact]
        public void ContainsKey_String_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);

            Assert.True(table.ContainsKey("test"));
        }

        [Fact]
        public void ContainsKey_String_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.ContainsKey("test"));
        }

        [Fact]
        public void ContainsKey_Long_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(1, 1234);

            Assert.True(table.ContainsKey(1));
        }

        [Fact]
        public void ContainsKey_Long_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.ContainsKey(1));
        }

        [Fact]
        public void ContainsKey_LuaArgument_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(true, 1234);

            Assert.True(table.ContainsKey(true));
        }

        [Fact]
        public void ContainsKey_LuaArgument_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void GetEnumerator()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);
            table.SetValue(1, 1234);
            table.SetValue(true, 1234);

            foreach (var (key, value) in table)
            {
                if (key.IsString)
                {
                    Assert.Equal("test", (string)key);
                    Assert.Equal(1234, (long)value);
                }
                else if (key.IsInteger)
                {
                    Assert.Equal(1, (long)key);
                    Assert.Equal(1234, (long)value);
                }
                else if (key.IsBoolean)
                {
                    Assert.True((bool)key);
                    Assert.Equal(1234, (long)value);
                }
            }
        }

        [Fact]
        public void Remove_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.Remove(null!));
        }

        [Fact]
        public void Remove_String_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);

            Assert.True(table.Remove("test"));

            Assert.False(table.ContainsKey("test"));
        }

        [Fact]
        public void Remove_String_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.Remove("test"));
        }

        [Fact]
        public void Remove_String2_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.Remove(null!, out _));
        }

        [Fact]
        public void Remove_String2_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue("test", 1234);

            Assert.True(table.Remove("test", out var value));
            Assert.Equal(1234, (long)value);

            Assert.False(table.ContainsKey("test"));
        }

        [Fact]
        public void Remove_String2_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.Remove("test", out _));
        }

        [Fact]
        public void Remove_Long_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(1, 1234);

            Assert.True(table.Remove(1));

            Assert.False(table.ContainsKey(1));
        }

        [Fact]
        public void Remove_Long_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.Remove(1));
        }

        [Fact]
        public void Remove_Long2_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(1, 1234);

            Assert.True(table.Remove(1, out var value));
            Assert.Equal(1234, (long)value);

            Assert.False(table.ContainsKey(1));
        }

        [Fact]
        public void Remove_Long2_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.Remove(1, out _));
        }

        [Fact]
        public void Remove_LuaArgument_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(true, 1234);

            Assert.True(table.Remove(true));

            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void Remove_LuaArgument_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.Remove(true));
        }

        [Fact]
        public void Remove_LuaArgument2_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();
            table.SetValue(true, 1234);

            Assert.True(table.Remove(true, out var value));
            Assert.Equal(1234, (long)value);

            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void Remove_LuaArgument2_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            using var table = environment.CreateTable();

            Assert.False(table.Remove(true, out _));
        }
    }
}
