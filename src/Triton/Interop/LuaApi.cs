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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Triton.Interop {
    /// <summary>
    /// Holds Lua API definitions.
    /// </summary>
    internal static class LuaApi {
        internal const int MinStackSize = 20;
        internal const int RegistryIndex = -1001000;

        #region Delegate Definitions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool CheckStackD(IntPtr state, int n);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CloseD(IntPtr state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void CreateTableD(IntPtr state, int numArray = 0, int numNonArray = 0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ErrorD(IntPtr state, byte[] errorMessage);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void GetFieldD(IntPtr state, int index, byte[] field);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LuaType GetGlobalD(IntPtr state, byte[] name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LuaType GetTableD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int GetTopD(IntPtr state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool IsIntegerD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LuaStatus LoadStringD(IntPtr state, byte[] s);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool NewMetatableD(IntPtr state, byte[] name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr NewStateD();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr NewUserdataD(IntPtr state, UIntPtr size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void OpenLibsD(IntPtr state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LuaStatus PCallKD(
            IntPtr state, int numArgs, int numResults = -1, int messageHandler = 0, IntPtr context = default(IntPtr),
            IntPtr k = default(IntPtr));

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushBooleanD(IntPtr state, bool b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushCClosureD(IntPtr state, LuaCFunction function, int numUpvalues);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushIntegerD(IntPtr state, long i);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushNilD(IntPtr state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushNumberD(IntPtr state, double n);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushLightUserdataD(IntPtr state, IntPtr p);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushLStringD(IntPtr state, byte[] s, UIntPtr length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void PushValueD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LuaType RawGetID(IntPtr state, int index, long n);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int RefD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetFieldD(IntPtr state, int index, byte[] field);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetGlobalD(IntPtr state, byte[] name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetMetatableD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetTableD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetTopD(IntPtr state, int top);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool ToBooleanD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate long ToIntegerXD(IntPtr state, int index, out bool isSuccess);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ToLStringD(IntPtr state, int index, out UIntPtr length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate double ToNumberXD(IntPtr state, int index, out bool isSuccess);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ToPointerD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr ToUserdataD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate LuaType TypeD(IntPtr state, int index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void UnrefD(IntPtr state, int index, int reference);
        #endregion

        internal static readonly NativeLibrary Library;

        #region Delegates
        internal static readonly CheckStackD CheckStack;
        internal static readonly CloseD Close;
        internal static readonly CreateTableD CreateTable;
        internal static readonly GetTableD GetTable;
        internal static readonly GetTopD GetTop;
        internal static readonly IsIntegerD IsInteger;
        internal static readonly NewStateD NewState;
        internal static readonly OpenLibsD OpenLibs;
        internal static readonly PCallKD PCallK;
        internal static readonly PushBooleanD PushBoolean;
        internal static readonly PushCClosureD PushCClosure;
        internal static readonly PushIntegerD PushInteger;
        internal static readonly PushNilD PushNil;
        internal static readonly PushNumberD PushNumber;
        internal static readonly PushLightUserdataD PushLightUserdata;
        internal static readonly PushValueD PushValue;
        internal static readonly RawGetID RawGetI;
        internal static readonly RefD Ref;
        internal static readonly SetMetatableD SetMetatable;
        internal static readonly SetTableD SetTable;
        internal static readonly SetTopD SetTop;
        internal static readonly ToBooleanD ToBoolean;
        internal static readonly ToPointerD ToPointer;
        internal static readonly ToUserdataD ToUserdata;
        internal static readonly TypeD Type;
        internal static readonly UnrefD Unref;

        private static readonly ErrorD ErrorDelegate;
        private static readonly GetFieldD GetFieldDelegate;
        private static readonly GetGlobalD GetGlobalDelegate;
        private static readonly LoadStringD LoadStringDelegate;
        private static readonly NewMetatableD NewMetatableDelegate;
        private static readonly NewUserdataD NewUserdataDelegate;
        private static readonly PushLStringD PushLString;
        private static readonly SetFieldD SetFieldDelegate;
        private static readonly SetGlobalD SetGlobalDelegate;
        private static readonly ToIntegerXD ToIntegerX;
        private static readonly ToLStringD ToLString;
        private static readonly ToNumberXD ToNumberX;
        #endregion

        static LuaApi() {
#if NETCORE
            var assemblyDirectory = Path.GetDirectoryName(GetAssemblyPath(typeof(LuaApi).GetTypeInfo().Assembly));
#else
            var assemblyDirectory = Path.GetDirectoryName(GetAssemblyPath(typeof(LuaApi).Assembly));
#endif

            string libraryName;
            if (Platform.IsWindows) {
                libraryName = "lua53.dll";
            } else if (Platform.IsOSX) {
                libraryName = "liblua53.dylib";
            } else if (Platform.IsLinux) {
                libraryName = "liblua53.so";
            } else {
                throw new PlatformNotSupportedException();
            }

            var searchPath = CombinePath("lua", Platform.Is64Bit ? "x64" : "x86", libraryName);
            var path1 = CombinePath(assemblyDirectory, searchPath);
            var path2 = CombinePath(assemblyDirectory, "..", "..", searchPath);
            Library = new NativeLibrary(path1, path2);

            CheckStack = Library.GetDelegate<CheckStackD>("lua_checkstack");
            Close = Library.GetDelegate<CloseD>("lua_close");
            CreateTable = Library.GetDelegate<CreateTableD>("lua_createtable");
            GetTable = Library.GetDelegate<GetTableD>("lua_gettable");
            GetTop = Library.GetDelegate<GetTopD>("lua_gettop");
            IsInteger = Library.GetDelegate<IsIntegerD>("lua_isinteger");
            NewState = Library.GetDelegate<NewStateD>("luaL_newstate");
            OpenLibs = Library.GetDelegate<OpenLibsD>("luaL_openlibs");
            PCallK = Library.GetDelegate<PCallKD>("lua_pcallk");
            PushBoolean = Library.GetDelegate<PushBooleanD>("lua_pushboolean");
            PushCClosure = Library.GetDelegate<PushCClosureD>("lua_pushcclosure");
            PushInteger = Library.GetDelegate<PushIntegerD>("lua_pushinteger");
            PushNil = Library.GetDelegate<PushNilD>("lua_pushnil");
            PushNumber = Library.GetDelegate<PushNumberD>("lua_pushnumber");
            PushLightUserdata = Library.GetDelegate<PushLightUserdataD>("lua_pushlightuserdata");
            PushValue = Library.GetDelegate<PushValueD>("lua_pushvalue");
            RawGetI = Library.GetDelegate<RawGetID>("lua_rawgeti");
            Ref = Library.GetDelegate<RefD>("luaL_ref");
            SetMetatable = Library.GetDelegate<SetMetatableD>("lua_setmetatable");
            SetTable = Library.GetDelegate<SetTableD>("lua_settable");
            SetTop = Library.GetDelegate<SetTopD>("lua_settop");
            ToBoolean = Library.GetDelegate<ToBooleanD>("lua_toboolean");
            ToPointer = Library.GetDelegate<ToPointerD>("lua_topointer");
            ToUserdata = Library.GetDelegate<ToUserdataD>("lua_touserdata");
            Type = Library.GetDelegate<TypeD>("lua_type");
            Unref = Library.GetDelegate<UnrefD>("luaL_unref");

            ErrorDelegate = Library.GetDelegate<ErrorD>("luaL_error");
            GetFieldDelegate = Library.GetDelegate<GetFieldD>("lua_getfield");
            GetGlobalDelegate = Library.GetDelegate<GetGlobalD>("lua_getglobal");
            LoadStringDelegate = Library.GetDelegate<LoadStringD>("luaL_loadstring");
            NewMetatableDelegate = Library.GetDelegate<NewMetatableD>("luaL_newmetatable");
            NewUserdataDelegate = Library.GetDelegate<NewUserdataD>("lua_newuserdata");
            PushLString = Library.GetDelegate<PushLStringD>("lua_pushlstring");
            SetFieldDelegate = Library.GetDelegate<SetFieldD>("lua_setfield");
            SetGlobalDelegate = Library.GetDelegate<SetGlobalD>("lua_setglobal");
            ToIntegerX = Library.GetDelegate<ToIntegerXD>("lua_tointegerx");
            ToNumberX = Library.GetDelegate<ToNumberXD>("lua_tonumberx");
            ToLString = Library.GetDelegate<ToLStringD>("lua_tolstring");
        }

        // This is a bit of a hack. This method never returns, and we want a way to tell the compiler this. So if it returns an Exception
        // and we do something like throw NativeMethods.Error(...), then the compiler will correctly deduce that the code following will
        // not be run.
        internal static Exception Error(IntPtr state, string errorMessage) {
            ErrorDelegate(state, GetUtf8String(errorMessage));
            return new InvalidOperationException("This should never have been reached!");
        }

        internal static void GetField(IntPtr state, int index, string field) => GetFieldDelegate(state, index, GetUtf8String(field));
        internal static LuaType GetGlobal(IntPtr state, string name) => GetGlobalDelegate(state, GetUtf8String(name));
        internal static void GetMetatable(IntPtr state, string name) => GetField(state, RegistryIndex, name);
        internal static LuaStatus LoadString(IntPtr state, string s) => LoadStringDelegate(state, GetUtf8String(s));
        internal static bool NewMetatable(IntPtr state, string name) => NewMetatableDelegate(state, GetUtf8String(name));
        internal static IntPtr NewUserdata(IntPtr state, int size) => NewUserdataDelegate(state, new UIntPtr((uint)size));
        internal static void Pop(IntPtr state, int n) => SetTop(state, -n - 1);
        internal static void SetField(IntPtr state, int index, string field) => SetFieldDelegate(state, index, GetUtf8String(field));
        internal static void SetGlobal(IntPtr state, string name) => SetGlobalDelegate(state, GetUtf8String(name));
        internal static long ToInteger(IntPtr state, int index) => ToIntegerX(state, index, out _);
        internal static double ToNumber(IntPtr state, int index) => ToNumberX(state, index, out _);
        internal static int UpvalueIndex(int i) => RegistryIndex - i;

        internal static void PushString(IntPtr state, string s) {
            // Because PushLString accepts a length parameter, we don't actually need to null-terminate the string.
            var buffer = Encoding.UTF8.GetBytes(s);
            PushLString(state, buffer, new UIntPtr((uint)buffer.Length));
        }

        internal static string ToString(IntPtr state, int index) {
            var ptr = ToLString(state, index, out var len);
            var byteCount = (int)len.ToUInt32();
            if (byteCount == 0) {
                return "";
            }

            var buffer = new byte[byteCount];
            Marshal.Copy(ptr, buffer, 0, byteCount);
            return Encoding.UTF8.GetString(buffer);
        }

        internal static void PushHandle(IntPtr state, GCHandle handle) {
            var ud = NewUserdata(state, IntPtr.Size);
            Marshal.WriteIntPtr(ud, GCHandle.ToIntPtr(handle));
        }

        internal static GCHandle ToHandle(IntPtr state, int index) {
            var ud = ToUserdata(state, index);
            return GCHandle.FromIntPtr(Marshal.ReadIntPtr(ud));
        }

        // See https://stackoverflow.com/a/28319367 for more details.
        private static string GetAssemblyPath(Assembly assembly) {
            const string prefix = "file:///";

            var codeBase = assembly.CodeBase;
            if (codeBase != null && codeBase.StartsWith(prefix)) {
                var path = codeBase.Substring(prefix.Length).Replace('/', '\\');
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(assembly.Location);
        }

        private static string CombinePath(params string[] paths) {
            var result = paths[0];
            for (var i = 1; i < paths.Length; i++) {
                result = Path.Combine(result, paths[i]);
            }
            return result;
        }

        private static byte[] GetUtf8String(string s) {
            var byteCount = Encoding.UTF8.GetByteCount(s);
            var buffer = new byte[byteCount + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
            return buffer;
        }
    }
}
