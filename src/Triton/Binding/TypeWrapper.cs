using System;

namespace Triton.Binding {
    /// <summary>
    /// A wrapper class that exposes static members of a type to Lua.
    /// </summary>
    internal sealed class TypeWrapper {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeWrapper"/> class with the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        public TypeWrapper(Type type) => Type = type;

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        public Type Type { get; }
    }
}
