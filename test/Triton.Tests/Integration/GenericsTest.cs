using Xunit;

namespace Triton.Tests.Integration {
    public class GenericsTest {
        private const string TestString = @"
            function assert(val)
                if not val then
                    error('assertion failed')
                end
            end

            import 'System.Int32'
            import 'System.String'
            import 'System.Object'
            import 'System.Collections.Generic.List`1'
            import 'System.Collections.Generic.Dictionary`2'

            list = List(Int32)()
            list:Add(1)
            list:Add(4)
            assert(list.Count == 2)

            dict = Dictionary(String, List(Int32))()
            dict.Item:Set(list, 'test')
            assert(dict.Count == 1)

            success, l = dict:TryGetValue('test')
            assert(success)
            assert(Object.ReferenceEquals(l, list))
        ";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                lua.DoString(TestString);
            }
        }
    }
}
