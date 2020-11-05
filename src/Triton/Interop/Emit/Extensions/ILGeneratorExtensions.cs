// Copyright (c) 2020 Kevin Zhao
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
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;

namespace Triton.Interop.Emit.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="ILGenerator"/> class.
    /// </summary>
    internal static class ILGeneratorExtensions
    {
        /// <summary>
        /// Declares a reusable local variable of the specified type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type of the local variable.</param>
        /// <returns>The declared reusable local variable.</returns>
        public static ReusableLocalBuilder DeclareReusableLocal(this ILGenerator ilg, Type type) =>
            ReusableLocalBuilder.Allocate(ilg, type);

        /// <summary>
        /// Declares the given number of labels.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="count">The number of labels.</param>
        /// <returns>The labels that can be used as tokens for branching.</returns>
        public static Label[] DefineLabels(this ILGenerator ilg, int count)
        {
            var labels = new Label[count];
            for (var i = 0; i < count; ++i)
            {
                labels[i] = ilg.DefineLabel();
            }

            return labels;
        }

        /// <summary>
        /// Emits a load indirect instruction for the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type to emit a load indirect instruction for.</param>
        public static void EmitLdind(this ILGenerator ilg, Type type)
        {
            type = type.Simplify();

            if (type.IsValueType && !type.IsPrimitive)
            {
                ilg.Emit(Ldobj, type);
                return;
            }

            ilg.Emit(true switch
            {
                _ when type == typeof(bool)    => Ldind_U1,
                _ when type == typeof(byte)    => Ldind_U1,
                _ when type == typeof(sbyte)   => Ldind_I1,
                _ when type == typeof(short)   => Ldind_I2,
                _ when type == typeof(ushort)  => Ldind_U2,
                _ when type == typeof(int)     => Ldind_I4,
                _ when type == typeof(uint)    => Ldind_U4,
                _ when type == typeof(long)    => Ldind_I8,
                _ when type == typeof(ulong)   => Ldind_I8,
                _ when type == typeof(IntPtr)  => Ldind_I,
                _ when type == typeof(UIntPtr) => Ldind_I,
                _ when type == typeof(char)    => Ldind_U2,
                _ when type == typeof(float)   => Ldind_R4,
                _ when type == typeof(double)  => Ldind_R8,
                _                              => Ldind_Ref
            });
        }

        /// <summary>
        /// Emits a store indirect instruction for the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type to emit a store indirect instruction for.</param>
        public static void EmitStind(this ILGenerator ilg, Type type)
        {
            type = type.Simplify();

            if (type.IsValueType && !type.IsPrimitive)
            {
                ilg.Emit(Stobj, type);
                return;
            }

            ilg.Emit(true switch
            {
                _ when type == typeof(bool)    => Stind_I1,
                _ when type == typeof(byte)    => Stind_I1,
                _ when type == typeof(sbyte)   => Stind_I1,
                _ when type == typeof(short)   => Stind_I2,
                _ when type == typeof(ushort)  => Stind_I2,
                _ when type == typeof(int)     => Stind_I4,
                _ when type == typeof(uint)    => Stind_I4,
                _ when type == typeof(long)    => Stind_I8,
                _ when type == typeof(ulong)   => Stind_I8,
                _ when type == typeof(IntPtr)  => Stind_I,
                _ when type == typeof(UIntPtr) => Stind_I,
                _ when type == typeof(char)    => Stind_I2,
                _ when type == typeof(float)   => Stind_R4,
                _ when type == typeof(double)  => Stind_R8,
                _                              => Stind_Ref
            });
        }
    }
}
