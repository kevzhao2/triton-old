// Copyright (c) 2018 Kevin Zhao
// Copyright (c) 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy
// of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations
// under the License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Triton.Interop {
    /// <summary>
    /// Provides platform-specific native library loading.
    /// </summary>
    internal sealed class NativeLibrary {
        private static readonly Func<string, IntPtr> Open;
        private static readonly Func<IntPtr, string, IntPtr> GetSymbol;

        private readonly IntPtr _handle;

        static NativeLibrary() {
            const int RTLD_NOW = 2;

            if (Platform.IsWindows) {
                Open = Windows.LoadLibrary;
                GetSymbol = Windows.GetProcAddress;
            } else if (Platform.IsOSX) {
                Open = f => OSX.dlopen(f, RTLD_NOW);
                GetSymbol = OSX.dlsym;
            } else if (Platform.IsLinux) {
                if (Platform.IsMono) {
                    Open = f => Mono.dlopen(f, RTLD_NOW);
                    GetSymbol = Mono.dlsym;
                } else if (Platform.IsNetCore) {
                    Open = f => CoreCLR.dlopen(f, RTLD_NOW);
                    GetSymbol = CoreCLR.dlsym;
                } else {
                    Open = f => Linux.dlopen(f, RTLD_NOW);
                    GetSymbol = Linux.dlsym;
                }
            } else {
                throw new PlatformNotSupportedException();
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NativeLibrary"/> class, picking one of the given paths.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <exception cref="BadImageFormatException">The given path is not a valid native library.</exception>
        /// <exception cref="FileNotFoundException">None of the paths are valid.</exception>
        public NativeLibrary(params string[] paths) {
            var path = paths.FirstOrDefault(File.Exists);
            if (path == null) {
                throw new FileNotFoundException($"Could not find native library at any of the paths: {string.Join(", ", paths)}");
            }

            _handle = Open(path);
            if (_handle == IntPtr.Zero) {
                throw new BadImageFormatException("Invalid native library.", path);
            }
        }
        
        /// <summary>
        /// Gets a delegate for the function with the given symbol.
        /// </summary>
        /// <typeparam name="T">The delegate type.</typeparam>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The delegate.</returns>
        public T GetDelegate<T>(string symbol) where T : class {
            var pointer = GetSymbol(_handle, symbol);

#if NETSTANDARD
            return Marshal.GetDelegateForFunctionPointer<T>(pointer);
#else
            return Marshal.GetDelegateForFunctionPointer(pointer, typeof(T)) as T;
#endif
        }

        private static class Windows {
            [DllImport("kernel32.dll")]
            public static extern IntPtr LoadLibrary(string path);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetProcAddress(IntPtr handle, string symbol);
        }

        // On OSX, libSystem.dylib will contain libdl.
        private static class OSX {
            [DllImport("libSystem.dylib")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("libSystem.dylib")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }

        // On Mono, load from the current process since Mono is linked against libdl.
        private static class Mono {
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }

        // On .NET core, use libcoreclr.so, which is linked against libdl.
        private static class CoreCLR {
            [DllImport("libcoreclr.so")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("libcoreclr.so")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }
        
        private static class Linux {
            [DllImport("libdl.so")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("libdl.so")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }
    }
}
