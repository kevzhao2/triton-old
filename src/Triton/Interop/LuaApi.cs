// Copyright (c) 2018 Kevin Zhao
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Triton.Interop {
    /// <summary>
    /// Provides Lua API definitions.
    /// </summary>
    internal static class LuaApi {
        public const int MinStackSize = 20;
        public const int MultRet = -1;
        public const int RegistryIndex = -1001000;
        public const int RidxMainThread = 1;

        public static readonly Delegates.CheckStack CheckStack;
        public static readonly Delegates.Close Close;
        public static readonly Delegates.CreateTable CreateTable;
        public static readonly Delegates.GetStack GetStack;
        public static readonly Delegates.GetTable GetTable;
        public static readonly Delegates.GetTop GetTop;
        public static readonly Delegates.IsInteger IsInteger;
        public static readonly Delegates.NewState NewState;
        public static readonly Delegates.NewThread NewThread;
        public static readonly Delegates.Next Next;
        public static readonly Delegates.OpenLibs OpenLibs;
        public static readonly Delegates.PCallK PCallK;
        public static readonly Delegates.PushBoolean PushBoolean;
        public static readonly Delegates.PushCClosure PushCClosure;
        public static readonly Delegates.PushInteger PushInteger;
        public static readonly Delegates.PushNil PushNil;
        public static readonly Delegates.PushNumber PushNumber;
        public static readonly Delegates.PushValue PushValue;
        public static readonly Delegates.RawGetI RawGetI;
        public static readonly Delegates.Ref Ref;
        public static readonly Delegates.Resume Resume;
        public static readonly Delegates.SetMetatable SetMetatable;
        public static readonly Delegates.SetTable SetTable;
        public static readonly Delegates.SetTop SetTop;
        public static readonly Delegates.Status Status;
        public static readonly Delegates.ToBoolean ToBoolean;
        public static readonly Delegates.ToPointer ToPointer;
        public static readonly Delegates.ToUserdata ToUserdata;
        public static readonly Delegates.Type Type;
        public static readonly Delegates.Unref Unref;

        private static readonly Delegates.GetField GetFieldDelegate;
        private static readonly Delegates.GetGlobal GetGlobalDelegate;
        private static readonly Delegates.LoadString LoadStringDelegate;
        private static readonly Delegates.NewMetatable NewMetatableDelegate;
        private static readonly Delegates.NewUserdata NewUserdataDelegate;
        private static readonly Delegates.PushLString PushLString;
        private static readonly Delegates.SetField SetFieldDelegate;
        private static readonly Delegates.SetGlobal SetGlobalDelegate;
        private static readonly Delegates.ToIntegerX ToIntegerX;
        private static readonly Delegates.ToLString ToLString;
        private static readonly Delegates.ToNumberX ToNumberX;

        static LuaApi() {
            var libraryFolder = "win-{0}";
            var libraryName = "lua53.dll";
            if (Platform.IsOSX) {
                libraryFolder = "osx-{0}";
                libraryName = "liblua53.dylib";
            } else if (Platform.IsLinux) {
                libraryFolder = "linux-{0}";
                libraryName = "liblua53.so";
            }
            var libraryPath = Path.Combine(string.Format(libraryFolder, Platform.Is64Bit ? "x64" : "x86"), libraryName);

            // There are two possible base paths: the assembly directory, and for unpublished .NET core projects using a NuGet package,
            // the assembly directory up two levels.
            var assemblyDirectory = Path.GetDirectoryName(GetAssemblyPath(typeof(LuaApi).Assembly));
            var path1 = new[] { assemblyDirectory, "runtimes", libraryPath }.Aggregate(Path.Combine);
            var path2 = new[] { assemblyDirectory, "..", "..", "runtimes", libraryPath }.Aggregate(Path.Combine);
            var library = new NativeLibrary(path1, path2);

            CheckStack = library.GetDelegate<Delegates.CheckStack>("lua_checkstack");
            Close = library.GetDelegate<Delegates.Close>("lua_close");
            CreateTable = library.GetDelegate<Delegates.CreateTable>("lua_createtable");
            GetStack = library.GetDelegate<Delegates.GetStack>("lua_getstack");
            GetTable = library.GetDelegate<Delegates.GetTable>("lua_gettable");
            GetTop = library.GetDelegate<Delegates.GetTop>("lua_gettop");
            IsInteger = library.GetDelegate<Delegates.IsInteger>("lua_isinteger");
            NewState = library.GetDelegate<Delegates.NewState>("luaL_newstate");
            NewThread = library.GetDelegate<Delegates.NewThread>("lua_newthread");
            Next = library.GetDelegate<Delegates.Next>("lua_next");
            OpenLibs = library.GetDelegate<Delegates.OpenLibs>("luaL_openlibs");
            PCallK = library.GetDelegate<Delegates.PCallK>("lua_pcallk");
            PushBoolean = library.GetDelegate<Delegates.PushBoolean>("lua_pushboolean");
            PushCClosure = library.GetDelegate<Delegates.PushCClosure>("lua_pushcclosure");
            PushInteger = library.GetDelegate<Delegates.PushInteger>("lua_pushinteger");
            PushNil = library.GetDelegate<Delegates.PushNil>("lua_pushnil");
            PushNumber = library.GetDelegate<Delegates.PushNumber>("lua_pushnumber");
            PushValue = library.GetDelegate<Delegates.PushValue>("lua_pushvalue");
            RawGetI = library.GetDelegate<Delegates.RawGetI>("lua_rawgeti");
            Ref = library.GetDelegate<Delegates.Ref>("luaL_ref");
            Resume = library.GetDelegate<Delegates.Resume>("lua_resume");
            SetMetatable = library.GetDelegate<Delegates.SetMetatable>("lua_setmetatable");
            SetTable = library.GetDelegate<Delegates.SetTable>("lua_settable");
            SetTop = library.GetDelegate<Delegates.SetTop>("lua_settop");
            Status = library.GetDelegate<Delegates.Status>("lua_status");
            ToBoolean = library.GetDelegate<Delegates.ToBoolean>("lua_toboolean");
            ToPointer = library.GetDelegate<Delegates.ToPointer>("lua_topointer");
            ToUserdata = library.GetDelegate<Delegates.ToUserdata>("lua_touserdata");
            Type = library.GetDelegate<Delegates.Type>("lua_type");
            Unref = library.GetDelegate<Delegates.Unref>("luaL_unref");

            GetFieldDelegate = library.GetDelegate<Delegates.GetField>("lua_getfield");
            GetGlobalDelegate = library.GetDelegate<Delegates.GetGlobal>("lua_getglobal");
            LoadStringDelegate = library.GetDelegate<Delegates.LoadString>("luaL_loadstring");
            NewMetatableDelegate = library.GetDelegate<Delegates.NewMetatable>("luaL_newmetatable");
            NewUserdataDelegate = library.GetDelegate<Delegates.NewUserdata>("lua_newuserdata");
            PushLString = library.GetDelegate<Delegates.PushLString>("lua_pushlstring");
            SetFieldDelegate = library.GetDelegate<Delegates.SetField>("lua_setfield");
            SetGlobalDelegate = library.GetDelegate<Delegates.SetGlobal>("lua_setglobal");
            ToIntegerX = library.GetDelegate<Delegates.ToIntegerX>("lua_tointegerx");
            ToNumberX = library.GetDelegate<Delegates.ToNumberX>("lua_tonumberx");
            ToLString = library.GetDelegate<Delegates.ToLString>("lua_tolstring");
        }

        public static void GetField(IntPtr state, int index, string field) => GetFieldDelegate(state, index, GetUtf8String(field));
        public static LuaType GetGlobal(IntPtr state, string name) => GetGlobalDelegate(state, GetUtf8String(name));

        public static IntPtr GetMainState(IntPtr state) {
            LuaApi.RawGetI(state, LuaApi.RegistryIndex, LuaApi.RidxMainThread);
            var result = LuaApi.ToPointer(state, -1);
            LuaApi.Pop(state, 1);
            return result;
        }

        public static void GetMetatable(IntPtr state, string name) => GetField(state, RegistryIndex, name);
        public static LuaStatus LoadString(IntPtr state, string s) => LoadStringDelegate(state, GetUtf8String(s));
        public static bool NewMetatable(IntPtr state, string name) => NewMetatableDelegate(state, GetUtf8String(name));
        public static IntPtr NewUserdata(IntPtr state, int size) => NewUserdataDelegate(state, new UIntPtr((uint)size));
        public static void Pop(IntPtr state, int n) => SetTop(state, -n - 1);

        public static void PushHandle(IntPtr state, GCHandle handle) {
            var ud = NewUserdata(state, IntPtr.Size);
            Marshal.WriteIntPtr(ud, GCHandle.ToIntPtr(handle));
        }

        public static void PushString(IntPtr state, string s) {
            // Because PushLString accepts a length parameter, we don't actually need to null-terminate the string.
            var buffer = Encoding.UTF8.GetBytes(s);
            PushLString(state, buffer, new UIntPtr((uint)buffer.Length));
        }

        public static void SetField(IntPtr state, int index, string field) => SetFieldDelegate(state, index, GetUtf8String(field));
        public static void SetGlobal(IntPtr state, string name) => SetGlobalDelegate(state, GetUtf8String(name));

        public static GCHandle ToHandle(IntPtr state, int index) {
            var ud = ToUserdata(state, index);
            return GCHandle.FromIntPtr(Marshal.ReadIntPtr(ud));
        }

        public static long ToInteger(IntPtr state, int index) => ToIntegerX(state, index, out _);
        public static double ToNumber(IntPtr state, int index) => ToNumberX(state, index, out _);

        public static string ToString(IntPtr state, int index) {
            var ptr = ToLString(state, index, out var len);
            var byteCount = (int)len.ToUInt32();
            if (byteCount == 0) {
                return "";
            }

            var buffer = new byte[byteCount];
            Marshal.Copy(ptr, buffer, 0, byteCount);
            return Encoding.UTF8.GetString(buffer);
        }

        public static int UpvalueIndex(int i) => RegistryIndex - i;

        private static string GetAssemblyPath(Assembly assembly) {
            const string prefix = "file:///";

            // See https://stackoverflow.com/a/28319367.
            var codeBase = assembly.CodeBase;
            if (codeBase != null && codeBase.StartsWith(prefix)) {
                var path = codeBase.Substring(prefix.Length).Replace('/', '\\');
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(assembly.Location);
        }

        private static byte[] GetUtf8String(string s) {
            var byteCount = Encoding.UTF8.GetByteCount(s);
            var buffer = new byte[byteCount + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Holds delegate definitions.
        /// </summary>
        public static class Delegates {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool CheckStack(IntPtr state, int n);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Close(IntPtr state);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void CreateTable(IntPtr state, int numArray = 0, int numNonArray = 0);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void GetField(IntPtr state, int index, byte[] field);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType GetGlobal(IntPtr state, byte[] name);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetStack(IntPtr state, int level, ref LuaDebug debug);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType GetTable(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetTop(IntPtr state);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool IsInteger(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaStatus LoadString(IntPtr state, byte[] s);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool NewMetatable(IntPtr state, byte[] name);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewState();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewThread(IntPtr state);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewUserdata(IntPtr state, UIntPtr size);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool Next(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void OpenLibs(IntPtr state);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaStatus PCallK(
                IntPtr state, int numArgs, int numResults = MultRet, int messageHandler = 0, IntPtr context = default(IntPtr),
                LuaKFunction continuation = null);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushBoolean(IntPtr state, bool b);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushCClosure(IntPtr state, LuaCFunction function, int numUpvalues);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushInteger(IntPtr state, long i);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushNil(IntPtr state);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushNumber(IntPtr state, double n);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushLString(IntPtr state, byte[] s, UIntPtr length);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PushValue(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType RawGetI(IntPtr state, int index, long n);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int Ref(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaStatus Resume(IntPtr thread, IntPtr from, int numArgs);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetField(IntPtr state, int index, byte[] field);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetGlobal(IntPtr state, byte[] name);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetMetatable(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetTable(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetTop(IntPtr state, int top);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaStatus Status(IntPtr state);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool ToBoolean(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long ToIntegerX(IntPtr state, int index, out bool isSuccess);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr ToLString(IntPtr state, int index, out UIntPtr length);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate double ToNumberX(IntPtr state, int index, out bool isSuccess);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr ToPointer(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr ToUserdata(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate LuaType Type(IntPtr state, int index);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Unref(IntPtr state, int index, int reference);
        }
    }
}
