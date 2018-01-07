using Xunit;

namespace Triton.Tests.Integration {
    public class ThreadTest {
        private const string TestString = @"
            for i = 1, 10 do
                x = i
                coroutine.yield(i)
            end
            return -1";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                var function = lua.LoadString(TestString);
                var thread = lua.CreateThread(function);

                Assert.True(thread.CanResume);

                for (var i = 1; i <= 10; i++) {
                    var results = thread.Resume();

                    Assert.Single(results);
                    Assert.Equal((long)i, results[0]);
                    Assert.Equal((long)i, lua["x"]);
                    Assert.True(thread.CanResume);
                }

                var results2 = thread.Resume();

                Assert.Single(results2);
                Assert.Equal(-1L, results2[0]);
                Assert.False(thread.CanResume);
            }
        }
    }
}
