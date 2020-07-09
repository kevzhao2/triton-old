using System;
using Xunit;

namespace Triton.Interop
{
    public class LuaCApiTests
    {
        [Fact]
        public void luaL_newstate()
        {
            var state = LuaCApi.luaL_newstate();

            Assert.NotEqual(IntPtr.Zero, state);
        }
    }
}
