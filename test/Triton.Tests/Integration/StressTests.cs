using Xunit;

namespace Triton.Tests.Integration {
    public class StressTests {
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
