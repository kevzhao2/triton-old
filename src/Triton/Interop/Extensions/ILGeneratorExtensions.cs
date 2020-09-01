// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Reflection.Emit.OpCodes;
using Debug = System.Diagnostics.Debug;

namespace Triton.Interop.Extensions
{
    /// <summary>
    /// Provides extensions for the <see cref="ILGenerator"/> class.
    /// </summary>
    internal static class ILGeneratorExtensions
    {
        /// <summary>
        /// Represents a reusable local variable.
        /// </summary>
        public sealed class ReusableLocalBuilder : IDisposable
        {
            private readonly ILGenerator _ilg;
            private readonly LocalBuilder _local;

            internal ReusableLocalBuilder(ILGenerator ilg, LocalBuilder local)
            {
                _ilg = ilg;
                _local = local;
            }

            /// <inheritdoc/>
            public void Dispose() => _ilg.FreeReusableLocal(this);

            /// <summary>
            /// Converts the given reusable local variable into a local variable.
            /// </summary>
            /// <param name="reusableLocal">The reusable local variable.</param>
            public static implicit operator LocalBuilder(ReusableLocalBuilder reusableLocal) => reusableLocal._local;
        }

        private static readonly ConditionalWeakTable<ILGenerator, Dictionary<Type, Stack<ReusableLocalBuilder>>>
            _freeLocalsByType = new ConditionalWeakTable<ILGenerator, Dictionary<Type, Stack<ReusableLocalBuilder>>>();

        /// <summary>
        /// Declares a reusable local variable with the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type.</param>
        /// <returns>The reusable local variable.</returns>
        public static ReusableLocalBuilder DeclareReusableLocal(this ILGenerator ilg, Type type)
        {
            var freeLocalsByType = _freeLocalsByType.GetOrCreateValue(ilg);
            return freeLocalsByType.TryGetValue(type, out var freeLocals) && freeLocals.Count > 0
                ? freeLocals.Pop()
                : new ReusableLocalBuilder(ilg, ilg.DeclareLocal(type));
        }

        /// <summary>
        /// Frees the given reusable local variable.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="reusableLocal">The reusable local variable.</param>
        public static void FreeReusableLocal(this ILGenerator ilg, ReusableLocalBuilder reusableLocal)
        {
            var freeLocalsByType = _freeLocalsByType.GetOrCreateValue(ilg);
            var type = ((LocalBuilder)reusableLocal).LocalType;
            if (!freeLocalsByType.TryGetValue(type, out var freeLocals))
            {
                freeLocals = new Stack<ReusableLocalBuilder>();
                freeLocalsByType.Add(type, freeLocals);
            }

            freeLocals.Push(reusableLocal);
        }

        /// <summary>
        /// Defines and marks a label.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <returns>The label.</returns>
        public static Label DefineAndMarkLabel(this ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            ilg.MarkLabel(label);
            return label;
        }

        /// <summary>
        /// Defines the given number of labels.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="count">The number of labels.</param>
        /// <returns>The labels.</returns>
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
        /// Marks the given labels.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="labels">The labels.</param>
        public static void MarkLabels(this ILGenerator ilg, IEnumerable<Label> labels)
        {
            foreach (var label in labels)
            {
                ilg.MarkLabel(label);
            }
        }

        /// <summary>
        /// Emits a load field for the given target and field.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The target.</param>
        /// <param name="field">The field.</param>
        public static void EmitLdfld(this ILGenerator ilg, LocalBuilder? target, FieldInfo field)
        {
            Debug.Assert(target is null == field.IsStatic);

            if (target is null)
            {
                ilg.Emit(Ldsfld, field);
            }
            else
            {
                ilg.Emit(Ldloc, target);
                ilg.Emit(Ldfld, field);
            }
        }

        /// <summary>
        /// Emits a store field for the given target and field.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The target.</param>
        /// <param name="field">The field.</param>
        public static void EmitStfld(this ILGenerator ilg, LocalBuilder? target, FieldInfo field)
        {
            Debug.Assert(target is null == field.IsStatic);

            if (target is null)
            {
                ilg.Emit(Stsfld, field);
            }
            else
            {
                ilg.Emit(Ldloc, target);
                ilg.Emit(Stfld, field);
            }
        }

        /// <summary>
        /// Emits a call for the given target and method.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The target.</param>
        /// <param name="method">The method.</param>
        public static void EmitCall(this ILGenerator ilg, LocalBuilder? target, MethodInfo method)
        {
            Debug.Assert(target is null == method.IsStatic);

            if (target is null)
            {
                ilg.Emit(Call, method);
            }
            else
            {
                ilg.Emit(Ldloc, target);
                ilg.Emit(Callvirt, method);
            }
        }

        /// <summary>
        /// Emits a call for the given method.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="method">The method.</param>
        public static void EmitCall(this ILGenerator ilg, MethodInfo method) =>
            ilg.Emit(method.IsVirtual ? Callvirt : Call, method);

        /// <summary>
        /// Emits a load element for the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type.</param>
        public static void EmitLdelem(this ILGenerator ilg, Type type)
        {
            type = type.Simplify();

            if (type.IsValueType && !type.IsPrimitive)
            {
                ilg.Emit(Ldelem, type);
                return;
            }

            ilg.Emit(true switch
            {
                _ when type == typeof(bool)    => Ldelem_U1,
                _ when type == typeof(byte)    => Ldelem_U1,
                _ when type == typeof(sbyte)   => Ldelem_I1,
                _ when type == typeof(short)   => Ldelem_I2,
                _ when type == typeof(ushort)  => Ldelem_U2,
                _ when type == typeof(int)     => Ldelem_I4,
                _ when type == typeof(uint)    => Ldelem_U4,
                _ when type == typeof(long)    => Ldelem_I8,
                _ when type == typeof(ulong)   => Ldelem_I8,
                _ when type == typeof(IntPtr)  => Ldelem_I,
                _ when type == typeof(UIntPtr) => Ldelem_I,
                _ when type == typeof(char)    => Ldelem_U2,
                _ when type == typeof(float)   => Ldelem_R4,
                _ when type == typeof(double)  => Ldelem_R8,
                _                              => Ldelem_Ref
            });
        }

        /// <summary>
        /// Emits a load indirect for the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type.</param>
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
        /// Emits a load element for the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type.</param>
        public static void EmitStelem(this ILGenerator ilg, Type type)
        {
            type = type.Simplify();

            if (type.IsValueType && !type.IsPrimitive)
            {
                ilg.Emit(Stelem, type);
                return;
            }

            ilg.Emit(true switch
            {
                _ when type == typeof(bool)    => Stelem_I1,
                _ when type == typeof(byte)    => Stelem_I1,
                _ when type == typeof(sbyte)   => Stelem_I1,
                _ when type == typeof(short)   => Stelem_I2,
                _ when type == typeof(ushort)  => Stelem_I2,
                _ when type == typeof(int)     => Stelem_I4,
                _ when type == typeof(uint)    => Stelem_I4,
                _ when type == typeof(long)    => Stelem_I8,
                _ when type == typeof(ulong)   => Stelem_I8,
                _ when type == typeof(IntPtr)  => Stelem_I,
                _ when type == typeof(UIntPtr) => Stelem_I,
                _ when type == typeof(char)    => Stelem_I2,
                _ when type == typeof(float)   => Stelem_R4,
                _ when type == typeof(double)  => Stelem_R8,
                _                              => Stelem_Ref
            });
        }

        /// <summary>
        /// Emits a store indirect for the given type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="type">The type.</param>
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

        /// <summary>
        /// Declares a local variable of type <see cref="Span{T}"/> with the given length and maximum stack-allocated
        /// length.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="length">The length of the span.</param>
        /// <param name="maxStackLength">The maximum stack-allocated length.</param>
        /// <returns>The local variable.</returns>
        public static LocalBuilder DeclareLocalSpan<T>(this ILGenerator ilg, LocalBuilder length, int maxStackLength)
        {
            var span = ilg.DeclareLocal(typeof(Span<T>));

            var isArray = ilg.DefineLabel();
            var skip = ilg.DefineLabel();

            ilg.Emit(Ldloc, length);
            ilg.Emit(Ldc_I4, maxStackLength);
            ilg.Emit(Bgt_S, isArray);
            {
                ilg.Emit(Ldloc, length);
                ilg.Emit(Ldc_I4, Unsafe.SizeOf<T>());
                ilg.Emit(Mul);
                ilg.Emit(Conv_U);
                ilg.Emit(Localloc);
                ilg.Emit(Ldloc, length);
                ilg.Emit(Newobj, typeof(Span<T>).GetConstructor(new[] { typeof(void*), typeof(int) })!);
                ilg.Emit(Stloc, span);

                ilg.Emit(Br_S, skip);
            }

            ilg.MarkLabel(isArray);

            ilg.Emit(Ldloc, length);
            ilg.Emit(Newarr, typeof(T));
            ilg.Emit(Call, typeof(Span<T>).GetMethod("op_Implicit", new[] { typeof(T[]) })!);
            ilg.Emit(Stloc, span);

            ilg.MarkLabel(skip);

            return span;
        }
    }
}
