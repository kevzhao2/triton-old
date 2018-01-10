using Xunit;

namespace Triton.Tests.Integration {
    public class GenericsTest {
        private const string TestString = @"
            using 'System'
            using 'System.Collections.Generic'

            list = List(Int32)()
            list:Add(1)
            list:Add(4)
            assert(list.Count == 2)

            dict = Dictionary(String, List(Int32))()
            dict.Item:Set(list, 'test')
            assert(dict.Count == 1)

            success, l = dict:TryGetValue('test')
            assert(success)
            assert(Object.ReferenceEquals(l, list))";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                lua.DoString(TestString);
            }
        }
    }
}
