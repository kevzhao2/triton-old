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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.BindingFlags;
using static System.Reflection.Emit.OpCodes;
using static Triton.NativeMethods;

namespace Triton.Interop.Extensions
{
    internal static class ILGeneratorExtensions
    {
        public static Label[] DefineLabels(this ILGenerator ilg, int count)
        {
            var labels = new Label[count];
            for (var i = 0; i < count; ++i)
            {
                labels[i] = ilg.DefineLabel();
            }

            return labels;
        }

        public static void MarkLabels(this ILGenerator ilg, IEnumerable<Label> labels)
        {
            foreach (var label in labels)
            {
                ilg.MarkLabel(label);
            }
        }

        public static void EmitLdelem(this ILGenerator ilg, Type type)
        {
            type = type.Simplify();

            if (!type.IsPrimitive && type.IsValueType)
            {
                ilg.Emit(Ldelem, type);
                return;
            }

            ilg.Emit(true switch
            {
                _ when type == typeof(bool) => Ldelem_U1,
                _ when type == typeof(byte) => Ldelem_U1,
                _ when type == typeof(sbyte) => Ldelem_I1,
                _ when type == typeof(short) => Ldelem_I2,
                _ when type == typeof(ushort) => Ldelem_U2,
                _ when type == typeof(int) => Ldelem_I4,
                _ when type == typeof(uint) => Ldelem_U4,
                _ when type == typeof(long) => Ldelem_I8,
                _ when type == typeof(ulong) => Ldelem_I8,
                _ when type == typeof(IntPtr) => Ldelem_I,
                _ when type == typeof(UIntPtr) => Ldelem_I,
                _ when type == typeof(char) => Ldelem_U2,
                _ when type == typeof(float) => Ldelem_R4,
                _ when type == typeof(double) => Ldelem_R8,
                _ => Ldelem_Ref
            });
        }

        public static void EmitLdind(this ILGenerator ilg, Type type)
        {
            type = type.Simplify();

            if (!type.IsPrimitive && type.IsValueType)
            {
                ilg.Emit(Ldobj, type);
                return;
            }

            ilg.Emit(true switch
            {
                _ when type == typeof(bool) => Ldind_U1,
                _ when type == typeof(byte) => Ldind_U1,
                _ when type == typeof(sbyte) => Ldind_I1,
                _ when type == typeof(short) => Ldind_I2,
                _ when type == typeof(ushort) => Ldind_U2,
                _ when type == typeof(int) => Ldind_I4,
                _ when type == typeof(uint) => Ldind_U4,
                _ when type == typeof(long) => Ldind_I8,
                _ when type == typeof(ulong) => Ldind_I8,
                _ when type == typeof(IntPtr) => Ldind_I,
                _ when type == typeof(UIntPtr) => Ldind_I,
                _ when type == typeof(char) => Ldind_U2,
                _ when type == typeof(float) => Ldind_R4,
                _ when type == typeof(double) => Ldind_R8,
                _ => Ldind_Ref
            });
        }
    }
}
