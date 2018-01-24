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
