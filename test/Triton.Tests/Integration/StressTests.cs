using Xunit;

namespace Triton.Tests.Integration {
    public class StressTests {
        private class TestClass {
            public int TestProperty { get; set; }

            public void TestMethod() {
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void GetProperty(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("x = test.TestProperty");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void SetProperty(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("test.TestProperty = 0");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(1000000)]
        public void CallMethod(int n) {
            using (var lua = new Lua()) {
                lua["test"] = new TestClass();
                var function = lua.CreateFunction("test:TestMethod()");

                for (var i = 0; i < n; ++i) {
                    function.Call();
                }
            }
        }

        [Theory]
        [InlineData(100000)]
        public void LotsOfReferencesCleanedUp(int n) {
            using (var lua = new Lua()) {
                for (var i = 0; i < n; ++i) {
                    var t = lua.CreateTable();
                }

                lua.DoString("");
            }
        }
    }
}
