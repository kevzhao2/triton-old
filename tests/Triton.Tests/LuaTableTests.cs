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
using System.Collections.Generic;
using Xunit;

namespace Triton
{
    public class LuaTableTests
    {
        [Fact]
        public void Count_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;
            table[1234] = 1234;
            table[true] = 1234;

            Assert.Equal(3, table.Count);
        }

        [Fact]
        public void Keys_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            var keys = table.Keys;

            // Testing `Count`:

            Assert.Equal(1, keys.Count);

            // Testing `IsReadOnly`:

            Assert.True(keys.IsReadOnly);

            // Testing `Contains`:

            Assert.True(keys.Contains("test"));
            Assert.False(keys.Contains(1234));

            // Testing `CopyTo`:

            Assert.Throws<ArgumentNullException>(() => keys.CopyTo(null!, 0));

            var array = new LuaValue[2];

            keys.CopyTo(array, 1);

            Assert.Equal("test", array[1]);

            // Testing `GetEnumerator`:

            using var enumerator = keys.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal("test", enumerator.Current);

            enumerator.Reset();

            Assert.True(enumerator.MoveNext());
            Assert.Equal("test", enumerator.Current);

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Values_Get()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            var values = table.Values;

            // Testing `Count`:

            Assert.Equal(1, values.Count);

            // Testing `IsReadOnly`:

            Assert.True(values.IsReadOnly);

            // Testing `Contains`:

            Assert.True(values.Contains(1234));
            Assert.False(values.Contains("test"));

            // Testing `CopyTo`:

            Assert.Throws<ArgumentNullException>(() => values.CopyTo(null!, 0));

            var array = new LuaValue[2];

            values.CopyTo(array, 1);

            Assert.Equal(1234, array[1]);

            // Testing `GetEnumerator`:

            using var enumerator = values.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1234, enumerator.Current);

            enumerator.Reset();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1234, enumerator.Current);

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Metatable_Get_ReturnsNull()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Null(table.Metatable);
        }

        [Fact]
        public void Metatable_Get_ReturnsNotNull()
        {
            using var environment = new LuaEnvironment();
            var (result, _) = environment.Eval(@"
                table = {}
                return setmetatable(table, {
                    __index = function(t, index)
                        return 0
                    end
                })");
            var table = (LuaTable)result;

            Assert.NotNull(table.Metatable);
        }

        [Fact]
        public void Metatable_Set_NullValue()
        {
            using var environment = new LuaEnvironment();
            var (result, _) = environment.Eval(@"
                table = {}
                return setmetatable(table, {
                    __index = function(t, index)
                        return 0
                    end
                })");
            var table = (LuaTable)result;

            table.Metatable = null;

            Assert.False(table.TryGetValue("test", out _));
        }

        [Fact]
        public void Metatable_Set_NotNullValue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            var metatable = environment.CreateTable();
            metatable["__index"] = environment.CreateFunction("return 0");

            table.Metatable = metatable;

            Assert.Equal(0, table["test"]);
        }

        [Fact]
        public void IsReadOnly_Explicit_Get()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            Assert.False(table.IsReadOnly);
        }

        [Fact]
        public void Item_Get_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[null!]);
        }

        [Fact]
        public void Item_Get_String_KeyDoesNotExist_ThrowsKeyNotFoundException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<KeyNotFoundException>(() => table["test"]);
        }

        [Fact]
        public void Item_Set_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table[null!] = 1234);
        }

        [Fact]
        public void Item_Set_String_Get_String()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table["test"] = 1234;

            Assert.Equal(1234, table["test"]);
        }

        [Fact]
        public void Item_Get_Long_KeyDoesNotExist_ThrowsKeyNotFoundException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<KeyNotFoundException>(() => table[1234]);
        }

        [Fact]
        public void Item_Set_Long_Get_Long()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table[1234] = 1234;

            Assert.Equal(1234, table[1234]);
        }

        [Fact]
        public void Item_Get_LuaValue_KeyDoesNotExist_ThrowsKeyNotFoundException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<KeyNotFoundException>(() => table[true]);
        }

        [Fact]
        public void Item_Set_LuaValue_Get_LuaValue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table[true] = 1234;

            Assert.Equal(1234, table[true]);
        }

        [Fact]
        public void Item_Get_LuaValue_Explicit_KeyDoesNotExist_ThrowsKeyNotFoundException()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            Assert.Throws<KeyNotFoundException>(() => table[true]);
        }

        [Fact]
        public void Item_Set_LuaValue_Explicit_GetLuaValue()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            table[true] = 1234;

            Assert.Equal(1234, table[true]);
        }

        [Fact]
        public void Add_String_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.Add(null!, 1234));
        }

        [Fact]
        public void Add_String_KeyAlreadyExists_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            Assert.Throws<ArgumentException>(() => table.Add("test", 1234));
        }

        [Fact]
        public void Add_String()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table.Add("test", 1234);

            Assert.True(table.TryGetValue("test", out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void Add_Long_KeyAlreadyExists_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[1234] = 1234;

            Assert.Throws<ArgumentException>(() => table.Add(1234, 1234));
        }

        [Fact]
        public void Add_Long()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table.Add(1234, 1234);

            Assert.True(table.TryGetValue(1234, out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void Add_LuaValue_KeyAlreadyExists_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[true] = 1234;

            Assert.Throws<ArgumentException>(() => table.Add(true, 1234));
        }

        [Fact]
        public void Add_LuaValue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            table.Add(true, 1234);

            Assert.True(table.TryGetValue(true, out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void Clear()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;
            table[1234] = 1234;
            table[true] = 1234;

            table.Clear();

            Assert.Empty(table);
        }

        [Fact]
        public void ContainsKey_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.ContainsKey(null!));
        }

        [Fact]
        public void ContainsKey_String_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            Assert.True(table.ContainsKey("test"));
        }

        [Fact]
        public void ContainsKey_String_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.ContainsKey("test"));
        }

        [Fact]
        public void ContainsKey_Long_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[1234] = 1234;

            Assert.True(table.ContainsKey(1234));
        }

        [Fact]
        public void ContainsKey_Long_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.ContainsKey(1234));
        }

        [Fact]
        public void ContainsKey_LuaValue_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[true] = 1234;

            Assert.True(table.ContainsKey(true));
        }

        [Fact]
        public void ContainsKey_LuaValue_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void ContainsValue_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            Assert.True(table.ContainsValue(1234));
        }

        [Fact]
        public void ContainsValue_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            Assert.False(table.ContainsValue("test"));
        }

        [Fact]
        public void GetEnumerator()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            using var enumerator = table.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(new("test", 1234), enumerator.Current);

            enumerator.Reset();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(new("test", 1234), enumerator.Current);

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Remove_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.Remove(null!));
        }

        [Fact]
        public void Remove_String_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            Assert.True(table.Remove("test"));
            Assert.False(table.ContainsKey("test"));
        }

        [Fact]
        public void Remove_String_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.Remove("test"));
        }

        [Fact]
        public void Remove_Long_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[1234] = 1234;

            Assert.True(table.Remove(1234));
            Assert.False(table.ContainsKey(1234));
        }

        [Fact]
        public void Remove_Long_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.Remove(1234));
        }

        [Fact]
        public void Remove_LuaValue_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[true] = 1234;

            Assert.True(table.Remove(true));
            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void Remove_LuaValue_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.Remove(true));
        }

        [Fact]
        public void TryGetValue_NullKey_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.TryGetValue(null!, out _));
        }

        [Fact]
        public void TryGetValue_String_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table["test"] = 1234;

            Assert.True(table.TryGetValue("test", out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void TryGetValue_String_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.TryGetValue("test", out _));
        }

        [Fact]
        public void TryGetValue_Long_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[1234] = 1234;

            Assert.True(table.TryGetValue(1234, out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void TryGetValue_Long_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.TryGetValue(1234, out _));
        }

        [Fact]
        public void TryGetValue_LuaValue_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();
            table[true] = 1234;

            Assert.True(table.TryGetValue(true, out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void TryGetValue_LuaValue_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            var table = environment.CreateTable();

            Assert.False(table.TryGetValue(true, out _));
        }

        [Fact]
        public void Add_LuaValue_Explicit_KeyAlreadyExists_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table[true] = 1234;

            Assert.Throws<ArgumentException>(() => table.Add(true, 1234));
        }

        [Fact]
        public void Add_LuaValue_Explicit()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            table.Add(true, 1234);

            Assert.True(table.TryGetValue(true, out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void ContainsKey_LuaValue_Explicit_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table[true] = 1234;

            Assert.True(table.ContainsKey(true));
        }

        [Fact]
        public void ContainsKey_LuaValue_Explicit_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void Remove_LuaValue_Explicit_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table[true] = 1234;

            Assert.True(table.Remove(true));
            Assert.False(table.ContainsKey(true));
        }

        [Fact]
        public void Remove_LuaValue_Explicit_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            Assert.False(table.Remove(true));
        }

        [Fact]
        public void TryGetValue_LuaValue_Explicit_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table[true] = 1234;

            Assert.True(table.TryGetValue(true, out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void TryGetValue_LuaValue_Explicit_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            Assert.False(table.TryGetValue(true, out _));
        }

        [Fact]
        public void Add_Explicit_KeyExists_ThrowsArgumentException()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table["test"] = 1234;

            Assert.Throws<ArgumentException>(() => table.Add(new("test", 1234)));
        }

        [Fact]
        public void Add_Explicit()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            table.Add(new("test", 1234));

            Assert.True(table.TryGetValue("test", out var value));
            Assert.Equal(1234, value);
        }

        [Fact]
        public void Contains_Explicit_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table["test"] = 1234;

            Assert.True(table.Contains(new("test", 1234)));
        }

        [Fact]
        public void Contains_Explicit_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table["test2"] = 1234;

            Assert.False(table.Contains(new("test", 1234)));
            Assert.False(table.Contains(new("test2", 5678)));
        }

        [Fact]
        public void CopyTo_Explicit_NullArray_ThrowsArgumentNullException()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();

            Assert.Throws<ArgumentNullException>(() => table.CopyTo(null!, 0));
        }

        [Fact]
        public void CopyTo_Explicit()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table["test2"] = 1234;

            var array = new KeyValuePair<LuaValue, LuaValue>[2];

            table.CopyTo(array, 1);

            Assert.Equal(new("test2", 1234), array[1]);
        }

        [Fact]
        public void Remove_Explicit_ReturnsTrue()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table["test"] = 1234;

            Assert.True(table.Remove(new KeyValuePair<LuaValue, LuaValue>("test", 1234)));
            Assert.False(table.ContainsKey("test"));
        }

        [Fact]
        public void Remove_Explicit_ReturnsFalse()
        {
            using var environment = new LuaEnvironment();
            IDictionary<LuaValue, LuaValue> table = environment.CreateTable();
            table["test2"] = 1234;

            Assert.False(table.Remove(new KeyValuePair<LuaValue, LuaValue>("test", 1234)));
            Assert.False(table.Remove(new KeyValuePair<LuaValue, LuaValue>("test2", 5678)));
            Assert.True(table.ContainsKey("test2"));
        }
    }
}
