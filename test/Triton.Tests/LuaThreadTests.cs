using System;
using Xunit;

namespace Triton.Tests {
    public class LuaThreadTests {
        [Fact]
        public void CanResume_Created_True() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() x = 4 end)")[0] as LuaThread) {
                Assert.True(thread.CanResume);
            }
        }

        [Fact]
        public void CanResume_Yielded_True() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() coroutine.yield() end)")[0] as LuaThread) {
                thread.Resume();

                Assert.True(thread.CanResume);
            }
        }

        [Fact]
        public void CanResume_Dead_False() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread) {
                thread.Resume();

                Assert.False(thread.CanResume);
            }
        }

        [Fact]
        public void CanResume_Disposed_ThrowsObjectDisposedException() {
            using (var lua = new Lua()) {
                var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread;
                thread.Dispose();

                Assert.Throws<ObjectDisposedException>(() => thread.CanResume);
            }
        }

        [Fact]
        public void Dispose_CalledTwice() {
            using (var lua = new Lua())
            using (var function = lua.LoadString("")) {
                var thread = lua.CreateThread(function);
                thread.Dispose();
                thread.Dispose();
            }
        }

        [Fact]
        public void Resume_NoArgs() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() x = 4 end)")[0] as LuaThread) {
                thread.Resume();

                Assert.Equal(4L, lua["x"]);
            }
        }

        [Fact]
        public void Resume_OneArg() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function(y) x = y end)")[0] as LuaThread) {
                thread.Resume(12);

                Assert.Equal(12L, lua["x"]);
            }
        }

        [Fact]
        public void Resume_YieldedOneArg() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() x = coroutine.yield() end)")[0] as LuaThread) {
                thread.Resume();
                thread.Resume(12);

                Assert.Equal(12L, lua["x"]);
            }
        }

        [Fact]
        public void Resume_ManyArgs() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function(a, b, c) x = a + b + c end)")[0] as LuaThread) {
                thread.Resume(12, 67, 123);

                Assert.Equal(202L, lua["x"]);
            }
        }

        [Fact]
        public void Resume_OneResult() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() coroutine.yield('test') end)")[0] as LuaThread) {
                var results = thread.Resume();

                Assert.Single(results);
                Assert.Equal("test", results[0]);
            }
        }

        [Fact]
        public void Resume_ManyResults() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() coroutine.yield(5, 4, 3) end)")[0] as LuaThread) {
                var results = thread.Resume();

                Assert.Equal(3, results.Length);
                Assert.Equal(5L, results[0]);
                Assert.Equal(4L, results[1]);
                Assert.Equal(3L, results[2]);
            }
        }

        [Fact]
        public void Resume_NullArgs_ThrowsArgumentNullException() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread) {
                Assert.Throws<ArgumentNullException>(() => thread.Resume(null));
            }
        }

        [Fact]
        public void Resume_Disposed_ThrowsObjectDisposedException() {
            using (var lua = new Lua()) {
                var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread;
                thread.Dispose();

                Assert.Throws<ObjectDisposedException>(() => thread.Resume());
            }
        }

        [Fact]
        public void Resume_NotResumable_ThrowsInvalidOperationException() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread) {
                thread.Resume();

                Assert.Throws<InvalidOperationException>(() => thread.Resume());
            }
        }

        [Fact]
        public void Resume_TooManyArguments_ThrowsLuaException() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() end)")[0] as LuaThread) {
                Assert.Throws<LuaException>(() => thread.Resume(new object[1000000]));
            }
        }

        [Fact]
        public void Resume_RuntimeError_ThrowsLuaException() {
            using (var lua = new Lua())
            using (var thread = lua.DoString("return coroutine.create(function() error('test') end)")[0] as LuaThread) {
                Assert.Throws<LuaException>(() => thread.Resume());
            }
        }
    }
}
