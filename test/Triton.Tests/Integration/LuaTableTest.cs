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

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Triton.Tests.Integration {
    public class LuaTableTest {
        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                var table = lua.CreateTable();
                table["apple"] = 15;
                table["bird"] = 678;
                table["couch"] = -156;
                table["deli"] = -667;
                
                void RemoveNegative(IDictionary<object, object> dict) {
                    var negativeKeys = dict.Where(kvp => (long)kvp.Value < 0).Select(kvp => kvp.Key).ToList();
                    foreach (var negativeKey in negativeKeys) {
                        Assert.True(dict.Remove(negativeKey));
                    }
                }
                long SumValues(IDictionary<object, object> dict) => dict.Values.Sum(v => (long)v);

                Assert.Equal(-130, SumValues(table));

                RemoveNegative(table);

                Assert.Equal(2, table.Count);
                foreach (var kvp in table) {
                    var value = (long)kvp.Value;
                    Assert.True(value > 0);
                }
                Assert.False(table.ContainsKey("couch"));
                Assert.False(table.ContainsKey("deli"));
            }
        }
    }
}
