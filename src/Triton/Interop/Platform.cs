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
using System.Runtime.InteropServices;

namespace Triton.Interop {
    /// <summary>
    /// Detects platform and architecture.
    /// </summary>
    internal static class Platform {
        static Platform() {
#if NETCORE
            IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsNetCore = RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");
#else
            var platform = Environment.OSVersion.Platform;

            // PlatformID.MacOSX is normally not returned, so we need to use uname.
            IsOSX = (platform == PlatformID.Unix && GetUname() == "Darwin") || platform == PlatformID.MacOSX;
            IsLinux = platform == PlatformID.Unix && !IsOSX;
            IsWindows = platform == PlatformID.Win32S || platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT;
            IsNetCore = false;
#endif
            IsMono = Type.GetType("Mono.Runtime") != null;
        }

        /// <summary>
        /// Gets a value indicating whether the platform is 64-bit.
        /// </summary>
        /// <value>A value indicating whether the platform is 64-bit.</value>
        public static bool Is64Bit => IntPtr.Size == 8;

        /// <summary>
        /// Gets a value indicating whether the platform is Windows.
        /// </summary>
        /// <value>A value indicating whether the platform is Windows.</value>
        public static bool IsWindows { get; }

        /// <summary>
        /// Gets a value indicating whether the platform is Mac OSX.
        /// </summary>
        /// <value>A value indicating whether the platform is Mac OSX.</value>
        public static bool IsOSX { get; }

        /// <summary>
        /// Gets a value indicating whether the platform is Linux.
        /// </summary>
        /// <value>A value indicating whether the platform is Linux.</value>
        public static bool IsLinux { get; }

        /// <summary>
        /// Gets a value indicating whether the platform is running with Mono.
        /// </summary>
        /// <value>A value indicating whether the platform is running with Mono.</value>
        public static bool IsMono { get; }

        /// <summary>
        /// Gets a value indicating whether the platform is running with .NET Core.
        /// </summary>
        /// <value>A value indicating whether the platform is running with .NET Core.</value>
        public static bool IsNetCore { get; }

        [DllImport("libc")]
        private static extern int uname(IntPtr buffer);

        private static string GetUname() {
            var buffer = Marshal.AllocHGlobal(8192);
            try {
                if (uname(buffer) == 0) {
                    return Marshal.PtrToStringAnsi(buffer);
                }
                return null;
            } finally {
                if (buffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
