using Xunit;

namespace Triton.Tests.Integration {
    public class MetatableTest {
        private const string TestString = @"
            x = table['test']
            assert(x == 1)
            y = table['test2']
            assert(y == 1)

            table['test'] = 5
            x = table['test']
            assert(x == 5)";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                var indexFunction = lua.CreateFunction("return 1");
                var metatable = lua.CreateTable();
                metatable["__index"] = indexFunction;

                lua.DoString("table = {}");

                var table = (LuaTable)lua["table"];
                table.Metatable = metatable;

                lua.DoString(TestString);
            }
        }
    }
}
