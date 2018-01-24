using Xunit;

namespace Triton.Tests.Integration {
    public class IOTest {
        private const string TestString = @"
            using 'System'
            using 'System.IO'

            path = Path.GetTempFileName()
            File.WriteAllText(path, 'string1\nstring2\nstring3\nstring4\nstring5\nstring6')

            sr = StreamReader(path)
            pcall(function()
                count = 1
                line = sr:ReadLine()
                while line ~= nil do
                    assert(line == 'string' .. count)
                    count = count + 1
                    line = sr:ReadLine()
                end
            end)
            sr:Dispose()";

        [Fact]
        public void Test() {
            using (var lua = new Lua()) {
                lua.DoString(TestString);
            }
        }
    }
}
