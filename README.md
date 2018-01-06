# Triton

Triton provides an easy and efficient way to embed Lua 5.3 into your .NET application!

## Usage

To get started, you should create a new `Lua` instance, which will create a new Lua environment:
```csharp
using (var lua = new Lua()) {
    lua.DoString("print('Hello, world!')");
}
```

The `DoString` method will execute Lua code within the context of that environment. To access the globals from .NET, you can index into the `Lua` instance as follows:
```csharp
lua.DoString("x = 'test'");
var x = (string)lua["x"];
Assert.Equal("test", x);
```

If you're going to execute a certain string many times, you can also use the `LoadString` method, which will return a `LuaFunction` that you can then call multiple times:
```csharp
var function = lua.LoadString("print('Hello!')");
for (var i = 0; i < 10000; ++i) {
    function.Call();
}
```

#### Passing .NET objects

To pass .NET objects over to the Lua environment, all you have to do is pass the object directly, e.g., using it as a function call argument or setting a global. Any Lua code will then be able to make use of the .NET object:
```csharp
var obj = new List<int>();
lua["obj"] = obj;
lua.DoString("obj:Add(2018)");
lua.DoString("count = obj.Count");

Assert.Single(obj);
Assert.Equal(2018, obj[0]);
Assert.Equal(1L, lua["count"]);
```

##### Generic Methods

You can access a generic method by "calling" it first with its type arguments, and then calling the method:
```csharp
class Test {
    public void Generic<T>(T t);
}

lua["Int32"] = typeof(int);
lua["obj"] = new Test();
lua.DoString("obj:Generic(Int32)(5)");
```

##### Indexed Properties

You must access indexed properties using a wrapper object, as follows:
```csharp
lua["obj"] = new List<int> { 55 };
lua.DoString("obj.Item:Set(obj.Item:Get(0) + 1, 0)");
```

The arguments to `Get` are the indices, and the first argument to `Set` is the value, with the rest of the arguments being the indices.

##### Events

You must access events using a wrapper object, as follows:
```csharp
class Test {
    public event EventHandler Event;
}

lua["obj"] = new Test();
lua.DoString("callback = function(obj, args) print(obj) end)");
lua.DoString("event = obj.Event");
lua.DoString("event:Add(callback)");
// ...
lua.DoString("event:Remove(callback)");
```

Note that `obj.Event` will return new wrapper objects each time, so to successfully remove your callback, you must save the value!

#### Passing .NET types

.NET types can be passed in using `ImportType`, and from the Lua side, .NET types can be imported using the `import` function. These types can then be used to access static members and create objects.
```csharp
lua.ImportType(typeof(int));
lua.DoString("import 'System.Collections.Generic.List`1'");
lua.DoString("list = List(Int32)()");
lua.DoString("list:Add(2018)");
```

## Comparison with NLua

#### Advantages

* Triton works with an unmodified Lua library, and targets Lua 5.3.
* Triton supports generic method invocation and generic type instantiation.
* Triton supports generalized indexed properties (including those declared in VB.NET or F# with names other than `Item`) with a variable number of indices.
* Triton will always correctly deduce overloads in the following situation, picking the method with the least number of default values applied:
  ```csharp
  void Method(int a);
  void Method(int a, int b = 0);
  ```
* Triton will always correctly call overloaded operators in the following scenario:
  ```csharp
  class Test1 {
      public static int operator +(Test2 t2, Test1 t1);
  }
  class Test2 {
      public static int operator +(Test2 t2, int i);
  }
  
  lua["t1"] = new Test1();
  lua["t2"] = new Test2();
  lua.DoString("x = t2 + t1");
  ```
* Triton implements finalizers on Lua references (`LuaFunction`, `LuaTable`, `LuaThread`), meaning that if you forget to `Dispose` them (which happens a lot!), you won't be leaking unmanaged memory.
* Triton will reuse Lua references, which saves memory. This, of course, comes with the caveat that `Dispose` must be called carefully.
* Triton is, in general, faster for .NET to Lua context switches and vice versa. See below for the one caveat.

#### Disadvantages
* Triton only supports event handler types that are "compatible" with the signature `void (object, EventArgs)`. Other types would require dynamic method generation, which is not possible on AOT.
* Triton does not support calling extension methods on objects as instance methods.
* Triton does not have a simple namespace-level `import`, since unfortunately `AppDomain` doesn't exist in the targeted version of .NET standard. This can be worked around by getting the `Assembly` of a type and then iterating through its exported types.
* Triton does not cache method lookups. This can result in a roughly 2x slowdown for the Lua to .NET context switch.