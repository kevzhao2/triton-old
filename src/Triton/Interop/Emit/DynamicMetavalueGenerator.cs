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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Triton.Interop.Emit.Helpers;
using static System.Reflection.Emit.OpCodes;
using static Triton.Lua;

// Required for visibility checks.
[assembly: InternalsVisibleTo("Triton.Interop.Emit.Generated")]

namespace Triton.Interop.Emit
{
    /// <summary>
    /// Provides the base class for a dynamic metavalue generator and helper methods supporting code generation.
    /// </summary>
    internal abstract class DynamicMetavalueGenerator : IMetavalueGenerator
    {
        private static readonly CustomAttributeBuilder _unmanagedCallersOnlyAttribute =
            new(typeof(UnmanagedCallersOnlyAttribute).GetConstructor(Type.EmptyTypes)!,
                Array.Empty<object?>(),
                new[] { typeof(UnmanagedCallersOnlyAttribute).GetField("CallConvs")! },
                new object?[] { new[] { typeof(CallConvCdecl) } });

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public virtual bool IsApplicable(object entity, bool isTypes) => true;

        /// <inheritdoc/>
        public virtual unsafe void Push(lua_State* state, object entity, bool isTypes)
        {
            var type = lua_getenvironment(state).ModuleBuilder.DefineType(Guid.NewGuid().ToString());
            var metamethodImpl = GenerateMetamethodImpl();
            var metamethod = GenerateMetamethod();

            lua_pushcfunction(state,
                (delegate* unmanaged[Cdecl]<lua_State*, int>)
                    type.CreateType()!
                        .GetMethod(metamethod.Name)!
                        .MethodHandle.GetFunctionPointer());
            return;

            MethodBuilder GenerateMetamethodImpl()
            {
                var metamethodImpl = type.DefineMethod(
                    "MetamethodImpl", MethodAttributes.Static,
                    typeof(int), new[] { typeof(lua_State*) });
                metamethodImpl.DefineParameter(1, ParameterAttributes.None, nameof(state));

                var ilg = metamethodImpl.GetILGenerator();

                if (isTypes)
                {
                    EmitMetamethodImpl(state, ilg, (IReadOnlyList<Type>)entity);
                }
                else
                {
                    EmitMetamethodImpl(state, ilg, entity);
                }

                return metamethodImpl;
            }

            MethodBuilder GenerateMetamethod()
            {
                var metamethod = type.DefineMethod(
                    "Metamethod", MethodAttributes.Public | MethodAttributes.Static,
                    typeof(int), new[] { typeof(lua_State*) });
                metamethod.SetCustomAttribute(_unmanagedCallersOnlyAttribute);
                metamethod.DefineParameter(1, ParameterAttributes.None, nameof(state));

                var ilg = metamethod.GetILGenerator();

                var result = ilg.DeclareLocal(typeof(int));

                // Call the implementation, preventing any CLR exceptions from being thrown (since they should be
                // avoided in unmanaged callbacks).

                ilg.BeginExceptionBlock();
                {
                    ilg.Emit(Ldarg_0);  // Lua state
                    ilg.Emit(Call, metamethodImpl);
                    ilg.Emit(Stloc, result);
                }

                ilg.BeginCatchBlock(typeof(Exception));
                {
                    var ex = ilg.DeclareLocal(typeof(Exception));
                    ilg.Emit(Stloc, ex);

                    EmitHelpers.LuaError(
                        ilg, "uncaught CLR exception: {0}\n",
                        ilg => ilg.Emit(Ldloc, ex));
                    ilg.Emit(Stloc, result);
                }

                ilg.EndExceptionBlock();

                ilg.Emit(Ldloc, result);
                ilg.Emit(Ret);

                return metamethod;
            }
        }

        /// <summary>
        /// Emits the metamethod implementation for the given CLR object.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="obj">The object whose metamethod to generate.</param>
        protected abstract unsafe void EmitMetamethodImpl(lua_State* state, ILGenerator ilg, object obj);

        /// <summary>
        /// Emits the metamethod implementation for the given CLR types.
        /// </summary>
        /// <param name="state">The Lua state.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="types">The types whose metamethod to generate.</param>
        protected abstract unsafe void EmitMetamethodImpl(lua_State* state, ILGenerator ilg, IReadOnlyList<Type> types);
    }
}
