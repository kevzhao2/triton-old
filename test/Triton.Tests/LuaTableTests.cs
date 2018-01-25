// Copyright (c) 2018 Kevin Zhao
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

using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Triton.Tests {
    public class LuaTableTests {
        private enum TestEnum {
            A, B, C, D
        }
        
        [Fact]
        public void GetSetDynamic() {
            using (var lua = new Lua()) {
                dynamic table = lua.CreateTable();

                table.x = 567;

                Assert.Equal(567L, table.x);
            }
        }

        [Fact]
        public void BinaryOpDynamic() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                metatable["__add"] = lua.DoString("return function(o1, o2) return o1.n + o2.n end")[0];
                dynamic table1 = lua.CreateTable();
                dynamic table2 = lua.CreateTable();
                table1.Metatable = metatable;
                table2.Metatable = metatable;
                table1.n = 1;
                table2.n = 100;

                Assert.Equal(101L, table1 + table2);
            }
        }

        [Fact]
        public void BinaryOpDynamic_NoMetatable_ThrowsBindingException() {
            using (var lua = new Lua()) {
                dynamic table1 = lua.CreateTable();
                dynamic table2 = lua.CreateTable();
                table1.n = 1;
                table2.n = 100;

                Assert.Throws<RuntimeBinderException>(() => table1 + table2);
            }
        }

        [Fact]
        public void BinaryOpDynamic_NoMetamethod_ThrowsBindingException() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                dynamic table1 = lua.CreateTable();
                dynamic table2 = lua.CreateTable();
                table1.Metatable = metatable;
                table2.Metatable = metatable;
                table1.n = 1;
                table2.n = 100;

                Assert.Throws<RuntimeBinderException>(() => table1 + table2);
            }
        }

        [Fact]
        public void CallDynamic() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                metatable["__call"] = lua.DoString("return function(o) return 6 end")[0];
                dynamic table = lua.CreateTable();
                table.Metatable = metatable;

                var results = table();

                Assert.Single(results);
                Assert.Equal(6L, results[0]);
            }
        }

        [Fact]
        public void CallDynamic_NoMetatable_ThrowsBindingException() {
            using (var lua = new Lua()) {
                dynamic table = lua.CreateTable();

                Assert.Throws<RuntimeBinderException>(() => table());
            }
        }

        [Fact]
        public void CallDynamic_NoCallMetamethod_ThrowsBindingException() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                dynamic table = lua.CreateTable();
                table.Metatable = metatable;

                Assert.Throws<RuntimeBinderException>(() => table());
            }
        }

        [Fact]
        public void UnaryOpDynamic() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                metatable["__unm"] = lua.DoString("return function(o1) return -o1.n end")[0];
                dynamic table = lua.CreateTable();
                table.Metatable = metatable;
                table.n = 1;

                Assert.Equal(-1L, -table);
            }
        }

        [Fact]
        public void UnaryOpDynamic_NoMetatable_ThrowsBindingException() {
            using (var lua = new Lua()) {
                dynamic table = lua.CreateTable();
                table.n = 1;

                Assert.Throws<RuntimeBinderException>(() => -table);
            }
        }

        [Fact]
        public void UnaryOpDynamic_NoMetamethod_ThrowsBindingException() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                dynamic table = lua.CreateTable();
                table.Metatable = metatable;
                table.n = 1;

                Assert.Throws<RuntimeBinderException>(() => -table);
            }
        }

        [Fact]
        public void ComparisonDynamic() {
            using (var lua = new Lua()) {
                var metatable = lua.CreateTable();
                metatable["__eq"] = lua.DoString("return function(o1, o2) return o1.n == o2.n end")[0];
                metatable["__lt"] = lua.DoString("return function(o1, o2) return o1.n < o2.n end")[0];
                metatable["__le"] = lua.DoString("return function(o1, o2) return o1.n <= o2.n end")[0];
                dynamic table1 = lua.CreateTable();
                dynamic table2 = lua.CreateTable();
                table1.Metatable = metatable;
                table2.Metatable = metatable;
                table1.n = 1;
                table2.n = 100;

                Assert.Equal(true, table1 < table2);
                Assert.Equal(true, table1 <= table2);
                Assert.Equal(true, table1 != table2);
                Assert.Equal(true, table2 > table1);
                Assert.Equal(true, table2 >= table1);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Count(int n) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                for (var i = 0; i < n; ++i) {
                    table[i] = 0;
                }

                Assert.Equal(n, table.Count);
            }
        }

        [Fact]
        public void GetMetatable() {
            using (var lua = new Lua()) {
                lua.DoString(@"
                    local mt = {}
                    table = {}
                    setmetatable(table, mt)");
                var table = (LuaTable)lua["table"];

                var metatable = table.Metatable;

                Assert.Empty(metatable);
            }
        }

        [Fact]
        public void GetMetatable_None() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Null(table.Metatable);
            }
        }

        [Fact]
        public void SetMetatable() {
            using (var lua = new Lua()) {
                lua.DoString("table = {}");
                var table = (LuaTable)lua["table"];
                var metatable = lua.CreateTable();
                metatable["test"] = 0;

                table.Metatable = metatable;

                Assert.Equal(0L, lua.DoString("return getmetatable(table)['test']")[0]);
            }
        }

        [Fact]
        public void SetMetatable_NullValue() {
            using (var lua = new Lua()) {
                lua.DoString(@"
                    local mt = {}
                    table = {}
                    setmetatable(table, mt)");
                var table = (LuaTable)lua["table"];

                table.Metatable = null;
                
                Assert.Null(lua.DoString("return getmetatable(table)")[0]);
            }
        }

        [Fact]
        public void Keys_Nothing() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Empty(table.Keys);
            }
        }

        [Fact]
        public void Keys_Something() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table[0] = "test";
                table[1] = "test2";

                var expected = new List<object> { 0L, 1L }.OrderBy(k => k);
                Assert.Equal(expected, table.Keys.OrderBy(k => k));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Keys_Count(int n) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                for (var i = 0; i < n; ++i) {
                    table[i] = 0;
                }

                Assert.Equal(n, table.Keys.Count);
            }
        }

        [Fact]
        public void Keys_IsReadOnly() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.True(table.Keys.IsReadOnly);
            }
        }

        [Fact]
        public void Keys_Add_ThrowsNotSupportedException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<NotSupportedException>(() => table.Keys.Add("test"));
            }
        }

        [Fact]
        public void Keys_Clear_ThrowsNotSupportedException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<NotSupportedException>(() => table.Keys.Clear());
            }
        }

        [Fact]
        public void Keys_Contains_KeyExists() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["test"] = 0;

                Assert.True(table.Keys.Contains("test"));
            }
        }

        [Fact]
        public void Keys_Contains_KeyDoesntExist() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.False(table.Keys.Contains("test"));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Keys_CopyTo(int n) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                for (var i = 0; i < n; ++i) {
                    table[i] = 0;
                }

                var array = new object[n];
                table.Keys.CopyTo(array, 0);

                var expected = new List<object>();
                for (var i = 0; i < n; ++i) {
                    expected.Add((long)i);
                }
                Assert.Equal(expected.OrderBy(k => k), array.OrderBy(k => k));
            }
        }

        [Fact]
        public void Keys_Remove_ThrowsNotSupportedException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<NotSupportedException>(() => table.Keys.Remove("test"));
            }
        }

        [Fact]
        public void Values_Nothing() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Empty(table.Values);
            }
        }

        [Fact]
        public void Values_Something() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table[0] = "test";
                table[1] = "test2";

                var expected = new List<object> { "test", "test2" }.OrderBy(k => k);
                Assert.Equal(expected, table.Values.OrderBy(k => k));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Values_Count(int n) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                for (var i = 0; i < n; ++i) {
                    table[i] = 0;
                }

                Assert.Equal(n, table.Values.Count);
            }
        }

        [Fact]
        public void Values_IsReadOnly() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.True(table.Values.IsReadOnly);
            }
        }

        [Fact]
        public void Values_Add_ThrowsNotSupportedException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<NotSupportedException>(() => table.Values.Add(0));
            }
        }

        [Fact]
        public void Values_Clear_ThrowsNotSupportedException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<NotSupportedException>(() => table.Values.Clear());
            }
        }

        [Fact]
        public void Values_Contains_ValueExists() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["test"] = 0;

                Assert.True(table.Values.Contains(0L));
            }
        }

        [Fact]
        public void Values_Contains_ValueDoesntExist() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.False(table.Values.Contains(0));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Values_CopyTo(int n) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                for (var i = 0; i < n; ++i) {
                    table[i] = 0;
                }

                var array = new object[n];
                table.Values.CopyTo(array, 0);

                var expected = new List<object>();
                for (var i = 0; i < n; ++i) {
                    expected.Add(0L);
                }
                Assert.Equal(expected.OrderBy(k => k), array.OrderBy(k => k));
            }
        }

        [Fact]
        public void Values_Remove_ThrowsNotSupportedException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<NotSupportedException>(() => table.Values.Remove(0));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetSet_Boolean(bool b) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = b;

                Assert.Equal(b, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Enum() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = TestEnum.C;

                Assert.Equal((long)TestEnum.C, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Function() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                var function = lua.CreateFunction("return 0");

                table["test"] = function;

                Assert.Same(function, table["test"]);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        public void GetSet_Integer(long i) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = i;

                Assert.Equal(i, table["test"]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(ulong.MaxValue)]
        public void GetSet_UInt64(ulong u) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = u;

                Assert.Equal((long)u, table["test"]);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void GetSet_IntegerKey(int i) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table[i] = "test";

                Assert.Equal("test", table[i]);
            }
        }

        [Theory]
        [InlineData(3.14159)]
        [InlineData(Double.NaN)]
        [InlineData(Double.PositiveInfinity)]
        public void GetSet_Number(double d) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = d;

                Assert.Equal(d, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Object() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                var obj = new object();

                table["test"] = obj;

                Assert.Same(obj, table["test"]);
            }
        }

        [Theory]
        [InlineData("str\n")]
        [InlineData("s\x88ff\n")]
        public void GetSet_String(string s) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = s;

                Assert.Equal(s, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Struct() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                var dateTime = DateTime.Now;

                table["test"] = dateTime;

                Assert.Equal(dateTime, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Table() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table["test"] = table;

                Assert.Same(table, table["test"]);
            }
        }

        [Fact]
        public void GetSet_Thread() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread;

                table["test"] = thread;
                Assert.Same(thread, table["test"]);
            }
        }

        [Fact]
        public void GetNull() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Null(table[null]);
            }
        }

        [Fact]
        public void Set_NullKey_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<ArgumentNullException>(() => table[null] = 0);
            }
        }

        [Fact]
        public void Set_ValueWrongLuaEnvironment_ThrowsArgumentException() {
            using (var lua = new Lua())
            using (var lua2 = new Lua()) {
                var table = lua.CreateTable();
                var table2 = lua2.CreateTable();

                Assert.Throws<ArgumentException>(() => table["x"] = table2);
            }
        }

        [Fact]
        public void ICollectionIsReadOnly() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();

                Assert.False(table.IsReadOnly);
            }
        }

        [Fact]
        public void Add_KeyDoesntExist() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                table.Add("test", 0);

                Assert.Equal(0L, table["test"]);
            }
        }

        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<ArgumentNullException>(() => table.Add(null, 0));
            }
        }

        [Fact]
        public void Add_NullValue_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Throws<ArgumentNullException>(() => table.Add("test", null));
            }
        }

        [Fact]
        public void Add_KeyExists_ThrowsArgumentException() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["test"] = 0;

                Assert.Throws<ArgumentException>(() => table.Add("test", 0));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Clear(int n) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                for (var i = 0; i < n; ++i) {
                    table[i] = 0;
                }

                table.Clear();

                for (var i = 0; i < n; ++i) {
                    Assert.Null(table[i]);
                }
                Assert.Empty(table);
            }
        }

        [Fact]
        public void ContainsKey_KeyExists() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["test"] = 0;

                Assert.True(table.ContainsKey("test"));
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData(null)]
        public void ContainsKey_KeyDoesntExist(object key) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.False(table.ContainsKey(key));
            }
        }

        [Fact]
        public void GetEnumerator_Nothing() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.Empty(table);
            }
        }

        [Fact]
        public void GetEnumerator_Something() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table[0] = "test";
                table[1] = "test2";

                var expected = new List<KeyValuePair<object, object>> {
                    new KeyValuePair<object, object>(0L, "test"),
                    new KeyValuePair<object, object>(1L, "test2")
                }.OrderBy(kvp => kvp.Key);
                Assert.Equal(expected, table.OrderBy(kvp => kvp.Key));
            }
        }

        [Fact]
        public void Remove_KeyExists() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["test"] = 0;

                Assert.True(table.Remove("test"));
                Assert.Null(table["test"]);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData(null)]
        public void Remove_KeyDoesntExist(object key) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.False(table.Remove(key));
            }
        }

        [Fact]
        public void TryGetValue_KeyExists() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["test"] = 0;

                Assert.True(table.TryGetValue("test", out var test));
                Assert.Equal(0L, test);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData(null)]
        public void TryGetValue_KeyDoesntExist(object key) {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();

                Assert.False(table.TryGetValue(key, out _));
            }
        }

        [Fact]
        public void ICollectionAdd_KeyDoesntExist() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;

                table.Add(new KeyValuePair<object, object>("test", 0));

                Assert.Equal(0L, table2["test"]);
            }
        }

        [Fact]
        public void ICollectionAdd_NullKey_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();

                Assert.Throws<ArgumentNullException>(() => table.Add(new KeyValuePair<object, object>(null, 0)));
            }
        }

        [Fact]
        public void ICollectionAdd_NullValue_ThrowsArgumentNullException() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();

                Assert.Throws<ArgumentNullException>(() => table.Add(new KeyValuePair<object, object>("test", null)));
            }
        }

        [Fact]
        public void ICollectionAdd_KeyExists_ThrowsArgumentException() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;
                table2["test"] = 0;

                Assert.Throws<ArgumentException>(() => table.Add(new KeyValuePair<object, object>("test", 0)));
            }
        }

        [Fact]
        public void ICollectionContains_KvpExists() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;
                table2["test"] = 0;

                Assert.True(table.Contains(new KeyValuePair<object, object>("test", 0L)));
            }
        }

        [Fact]
        public void ICollectionContains_KeyExistsButKvpDoesntExist() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;
                table2["test"] = 0;

                Assert.False(table.Contains(new KeyValuePair<object, object>("test", 1L)));
            }
        }

        [Theory]
        [InlineData("test", 0)]
        [InlineData("test", null)]
        [InlineData(null, 0)]
        public void ICollectionContains_KvpDoesntExist(object key, object value) {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();

                Assert.False(table.Contains(new KeyValuePair<object, object>(key, value)));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ICollectionCopyTo(int n) {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;
                for (var i = 0; i < n; ++i) {
                    table2[i] = 0;
                }
                var array = new KeyValuePair<object, object>[n];

                table.CopyTo(array, 0);

                var expected = new List<KeyValuePair<object, object>>();
                for (var i = 0; i < n; ++i) {
                    expected.Add(new KeyValuePair<object, object>((long)i, 0L));
                }
                Assert.Equal(expected.OrderBy(kvp => kvp.Key), array.OrderBy(kvp => kvp.Key));
            }
        }

        [Fact]
        public void ICollectionRemove_KvpExists() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;
                table2["test"] = 0;

                Assert.True(table.Remove(new KeyValuePair<object, object>("test", 0L)));
                Assert.Null(table2["test"]);
            }
        }

        [Fact]
        public void ICollectionRemove_KeyExistsButKvpDoesntExist() {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();
                var table2 = (LuaTable)table;
                table2["test"] = 0;

                Assert.False(table.Remove(new KeyValuePair<object, object>("test", 1L)));
            }
        }

        [Theory]
        [InlineData("test", 0)]
        [InlineData("test", null)]
        [InlineData(null, 0)]
        public void ICollectionRemove_KvpDoesntExist(object key, object value) {
            using (var lua = new Lua()) {
                ICollection<KeyValuePair<object, object>> table = lua.CreateTable();

                Assert.False(table.Remove(new KeyValuePair<object, object>(key, value)));
            }
        }
    }
}
