using Xunit;

namespace Triton.Tests.Integration {
    public class CoroutineTest {
        private const string TestString = @"
            using 'System'
            using 'System.Collections.Generic'

            list = List(String)()
            co = coroutine.create(function()
                list:Add('checkpoint 1')
                coroutine.yield()
                list:Add('checkpoint 2')
                coroutine.yield()
                list:Add('checkpoint 3')
                coroutine.yield()
                list = List(String)()
                list:Add('checkpoint 4')
                coroutine.yield()
                list:Clear()
            end)

            coroutine.resume(co)
            assert(list.Count == 1 and list.Item:Get(0) == 'checkpoint 1')
            coroutine.resume(co)
            assert(list.Count == 2 and list.Item:Get(1) == 'checkpoint 2')
            coroutine.resume(co)
            assert(list.Count == 3 and list.Item:Get(2) == 'checkpoint 3')
            coroutine.resume(co)
            assert(list.Count == 1 and list.Item:Get(0) == 'checkpoint 4')
            coroutine.resume(co)
            assert(list.Count == 0)";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                lua.DoString(TestString);
            }
        }
    }
}
