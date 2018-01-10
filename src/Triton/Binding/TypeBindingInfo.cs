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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Triton.Binding {
    /// <summary>
    /// Holds binding information about a type.
    /// </summary>
    internal sealed class TypeBindingInfo {
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.Static;

        private List<ConstructorInfo> _constructors;
        private Dictionary<string, MemberInfo> _members;
        private Dictionary<string, MemberInfo> _staticMembers;
        private ILookup<string, MethodInfo> _methods;
        private ILookup<string, MethodInfo> _staticMethods;
        private ILookup<string, MethodInfo> _operators;

        private TypeBindingInfo() {
        }

        /// <summary>
        /// Gets the constructors.
        /// </summary>
        /// <returns>The constructors.</returns>
        public IList<ConstructorInfo> GetConstructors() => _constructors;

        /// <summary>
        /// Gets the member with the given name, or <c>null</c> if there is none.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isStatic"><c>true</c> to check static members; <c>false</c> otherwise.</param>
        /// <returns>The member.</returns>
        public MemberInfo GetMember(string name, bool isStatic) {
            var members = isStatic ? _staticMembers : _members;
            return members.GetValueOrDefault(name);
        }

        /// <summary>
        /// Gets the methods with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isStatic"><c>true</c> to check static methods; <c>false</c> otherwise.</param>
        /// <param name="numTypeArgs">The number of type arguments, or <c>null</c> for any.</param>
        /// <returns>The methods.</returns>
        public IEnumerable<MethodInfo> GetMethods(string name, bool isStatic, int? numTypeArgs = null) {
            var methods = isStatic ? _staticMethods : _methods;
            return numTypeArgs == null ? methods[name] : methods[name].Where(m => m.GetGenericArguments().Length == numTypeArgs);
        }

        /// <summary>
        /// Gets the operators with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The operators.</returns>
        public IEnumerable<MethodInfo> GetOperators(string name) => _operators[name];

        /// <summary>
        /// Constructs a <see cref="TypeBindingInfo"/> for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The <see cref="TypeBindingInfo"/>.</returns>
        public static TypeBindingInfo Construct(Type type) {
            var constructors = type.GetConstructors().Where(IsBound);
            var methods = type.GetMethods(InstanceFlags).Where(m => !m.IsSpecialName && IsBound(m)).ToList();
            var staticMethods = type.GetMethods(StaticFlags).Where(m => !m.IsSpecialName && IsBound(m)).ToList();
            var members = type.GetFields(InstanceFlags).Where(f => !f.IsSpecialName).Cast<MemberInfo>()
                .Concat(type.GetEvents(InstanceFlags).Where(e => !e.IsSpecialName).Cast<MemberInfo>())
                .Concat(type.GetProperties(InstanceFlags).Where(p => !p.IsSpecialName).Cast<MemberInfo>())
                .Concat(methods.Cast<MemberInfo>()).Where(IsBound);
            var staticMembers = type.GetFields(StaticFlags).Where(f => !f.IsSpecialName).Cast<MemberInfo>()
                .Concat(type.GetEvents(StaticFlags).Where(e => !e.IsSpecialName).Cast<MemberInfo>())
                .Concat(type.GetProperties(StaticFlags).Where(p => !p.IsSpecialName).Cast<MemberInfo>())
                .Concat(type.GetMethods(StaticFlags).Where(m => !m.IsSpecialName).Cast<MemberInfo>())
                .Concat(staticMethods.Cast<MemberInfo>())
                .Concat(type.GetNestedTypes().Cast<MemberInfo>()).Where(IsBound);
            var operators = type.GetMethods(StaticFlags).Where(m => IsBound(m) && m.IsSpecialName && m.Name.StartsWith("op_"));

            return new TypeBindingInfo {
                _constructors = constructors.ToList(),
                _members = members.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.First()),
                _staticMembers = staticMembers.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.First()),
                _methods = methods.ToLookup(m => m.Name),
                _staticMethods = staticMethods.ToLookup(m => m.Name),
                _operators = operators.ToLookup(m => m.Name),
            };

            bool IsBound(MemberInfo member) {
                return !member.IsDefined(typeof(LuaIgnoreAttribute), false);
            }
        }
    }
}
