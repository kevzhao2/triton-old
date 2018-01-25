# Triton [![Build Status](https://travis-ci.org/kevzhao2/Triton.svg?branch=master)](https://travis-ci.org/kevzhao2/Triton) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Triton provides an easy and efficient way to embed Lua 5.3 into your .NET application! Check out [the wiki](https://github.com/kevzhao2/Triton/wiki) for more comprehensive documentation.
## Comparison with NLua

### Advantages

* Triton works with an unmodified Lua library, and targets Lua 5.3, which has native support for integer types among other things.
* Triton supports .NET callbacks from coroutines.
* Triton supports `LuaThread` manipulation.
* Triton supports generic method invocation and generic type instantiation.
* Triton supports generalized indexed properties (including those declared in VB.NET or F# with names other than `Item`) with a variable number of indices.
* Triton supports `dynamic` usage of `Lua`, `LuaFunction`, and `LuaTable`.
* Triton reuses `LuaReference` objects and will clean up unused references, saving as much memory in a transparent way as possible.
* Triton will always correctly call the 'correct' overloads for methods.
* Triton is, in general, faster in its .NET -> Lua context switches.
* Triton is safer and, in general, faster in its Lua -> .NET context switches.

### Disadvantages
* Triton only supports event handler types that are "compatible" with the signature `void (object, EventArgs)`.
* Triton does not support calling extension methods on objects as instance methods.

### Roadmap
* Improve general performance.
* Look into exposing debugging facilities.