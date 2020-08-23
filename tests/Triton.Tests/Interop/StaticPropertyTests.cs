// Copyright (c) 2020 Kevin Zhao. All rights reserved.
//
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xunit;

namespace Triton.Interop
{
    public class StaticPropertyTests
    {
        public class LuaValueProperty
        {
            public static LuaValue Value { get; set; }
        }

        public class BoolProperty
        {
            public static bool Value { get; set; }
        }

        public class ByteProperty
        {
            public static byte Value { get; set; }
        }

        public class ShortProperty
        {
            public static short Value { get; set; }
        }

        public class IntProperty
        {
            public static int Value { get; set; }
        }

        public class LongProperty
        {
            public static long Value { get; set; }
        }

        public class SByteProperty
        {
            public static sbyte Value { get; set; }
        }

        public class UShortProperty
        {
            public static ushort Value { get; set; }
        }

        public class UIntProperty
        {
            public static uint Value { get; set; }
        }

        public class ULongProperty
        {
            public static ulong Value { get; set; }
        }

        public class FloatProperty
        {
            public static float Value { get; set; }
        }

        public class DoubleProperty
        {
            public static double Value { get; set; }
        }

        public class StringProperty
        {
            public static string? Value { get; set; }
        }

        public class CharProperty
        {
            public static char Value { get; set; }
        }

        public class LuaObjectProperty
        {
            public static LuaObject? Value { get; set; }
            public static LuaTable? TableValue { get; set; }
            public static LuaFunction? FunctionValue { get; set; }
            public static LuaThread? ThreadValue { get; set; }
        }

        public class ClrClassProperty
        {
            public static List<int>? ListValue { get; set; }
        }

        public class ClrStructProperty
        {
            public static DateTime DateTimeValue { get; set; }
        }

        public class ByRefProperty
        {
            [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Testing")]
            private static int _value;

            public static ref int Value => ref _value;
            public static unsafe ref int NullValue => ref Unsafe.AsRef<int>(null);
        }

        public class NullableIntProperty
        {
            public static int? Value { get; set; }
        }

        public class ByRefNullableIntProperty
        {
            [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Testing")]
            private static int? _value;

            public static ref int? Value => ref _value;
        }

        public class NonReadableProperty
        {
            [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Testing")]
            private static int _value;

            public static int PrivateGetter { private get; set; }
            public static int NoGetter { set => _value = value; }
        }

        public class NonWritableProperty
        {
            public static int PrivateSetter { get; private set; }
            public static int NoSetter => 1234;
        }

        public class ByRefLikeProperty
        {
            public static Span<int> Value { get => default; set { } }
        }

        [Fact]
        public void LuaValue_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LuaValueProperty"] = LuaValue.FromClrType(typeof(LuaValueProperty));

            LuaValueProperty.Value = 1234;

            environment.Eval("assert(LuaValueProperty.Value == 1234)");
        }

        [Fact]
        public void LuaValue_Set()
        {
            using var environment = new LuaEnvironment();
            environment["LuaValueProperty"] = LuaValue.FromClrType(typeof(LuaValueProperty));

            environment.Eval("LuaValueProperty.Value = 1234");

            Assert.Equal(1234, LuaValueProperty.Value);
        }

        [Fact]
        public void Bool_Get()
        {
            using var environment = new LuaEnvironment();
            environment["BoolProperty"] = LuaValue.FromClrType(typeof(BoolProperty));

            BoolProperty.Value = true;

            environment.Eval("assert(BoolProperty.Value)");
        }

        [Fact]
        public void Bool_Set()
        {
            using var environment = new LuaEnvironment();
            environment["BoolProperty"] = LuaValue.FromClrType(typeof(BoolProperty));

            environment.Eval("BoolProperty.Value = true");

            Assert.True(BoolProperty.Value);
        }

        [Fact]
        public void Byte_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ByteProperty"] = LuaValue.FromClrType(typeof(ByteProperty));

            ByteProperty.Value = 123;

            environment.Eval("assert(ByteProperty.Value == 123)");
        }

        [Fact]
        public void Byte_Set()
        {
            using var environment = new LuaEnvironment();
            environment["ByteProperty"] = LuaValue.FromClrType(typeof(ByteProperty));

            environment.Eval("ByteProperty.Value = 123");

            Assert.Equal(123, ByteProperty.Value);
        }

        [Fact]
        public void Byte_SetOverflow_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["ByteProperty"] = LuaValue.FromClrType(typeof(ByteProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("ByteProperty.Value = 12345"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void Short_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ShortProperty"] = LuaValue.FromClrType(typeof(ShortProperty));

            ShortProperty.Value = 12345;

            environment.Eval("assert(ShortProperty.Value == 12345)");
        }

        [Fact]
        public void Short_Set()
        {
            using var environment = new LuaEnvironment();
            environment["ShortProperty"] = LuaValue.FromClrType(typeof(ShortProperty));

            environment.Eval("ShortProperty.Value = 12345");

            Assert.Equal(12345, ShortProperty.Value);
        }

        [Fact]
        public void Short_SetOverflow_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["ShortProperty"] = LuaValue.FromClrType(typeof(ShortProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("ShortProperty.Value = 123456789"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void Int_Get()
        {
            using var environment = new LuaEnvironment();
            environment["IntProperty"] = LuaValue.FromClrType(typeof(IntProperty));

            IntProperty.Value = 123456789;

            environment.Eval("assert(IntProperty.Value == 123456789)");
        }

        [Fact]
        public void Int_Set()
        {
            using var environment = new LuaEnvironment();
            environment["IntProperty"] = LuaValue.FromClrType(typeof(IntProperty));

            environment.Eval("IntProperty.Value = 123456789");

            Assert.Equal(123456789, IntProperty.Value);
        }

        [Fact]
        public void Int_SetOverflow_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["IntProperty"] = LuaValue.FromClrType(typeof(IntProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("IntProperty.Value = 1234567891011"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void Long_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LongProperty"] = LuaValue.FromClrType(typeof(LongProperty));

            LongProperty.Value = 1234567891011;

            environment.Eval("assert(LongProperty.Value == 1234567891011)");
        }

        [Fact]
        public void Long_Set()
        {
            using var environment = new LuaEnvironment();
            environment["LongProperty"] = LuaValue.FromClrType(typeof(LongProperty));

            environment.Eval("LongProperty.Value = 1234567891011");

            Assert.Equal(1234567891011, LongProperty.Value);
        }

        [Fact]
        public void SByte_Get()
        {
            using var environment = new LuaEnvironment();
            environment["SByteProperty"] = LuaValue.FromClrType(typeof(SByteProperty));

            SByteProperty.Value = 123;

            environment.Eval("assert(SByteProperty.Value == 123)");
        }

        [Fact]
        public void SByte_Set()
        {
            using var environment = new LuaEnvironment();
            environment["SByteProperty"] = LuaValue.FromClrType(typeof(SByteProperty));

            environment.Eval("SByteProperty.Value = 123");

            Assert.Equal(123, SByteProperty.Value);
        }

        [Fact]
        public void SByte_SetOverflow_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["SByteProperty"] = LuaValue.FromClrType(typeof(SByteProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("SByteProperty.Value = 128"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void UShort_Get()
        {
            using var environment = new LuaEnvironment();
            environment["UShortProperty"] = LuaValue.FromClrType(typeof(UShortProperty));

            UShortProperty.Value = 12345;

            environment.Eval("assert(UShortProperty.Value == 12345)");
        }

        [Fact]
        public void UShort_Set()
        {
            using var environment = new LuaEnvironment();
            environment["UShortProperty"] = LuaValue.FromClrType(typeof(UShortProperty));

            environment.Eval("UShortProperty.Value = 12345");

            Assert.Equal(12345, UShortProperty.Value);
        }

        [Fact]
        public void UShort_SetUnderflow_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["UShortProperty"] = LuaValue.FromClrType(typeof(UShortProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("UShortProperty.Value = -1"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void UInt_Get()
        {
            using var environment = new LuaEnvironment();
            environment["UIntProperty"] = LuaValue.FromClrType(typeof(UIntProperty));

            UIntProperty.Value = 123456789U;

            environment.Eval("assert(UIntProperty.Value == 123456789)");
        }

        [Fact]
        public void UInt_Set()
        {
            using var environment = new LuaEnvironment();
            environment["UIntProperty"] = LuaValue.FromClrType(typeof(UIntProperty));

            environment.Eval("UIntProperty.Value = 123456789");

            Assert.Equal(123456789U, UIntProperty.Value);
        }

        [Fact]
        public void UInt_SetUnderflow_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["UIntProperty"] = LuaValue.FromClrType(typeof(UIntProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("UIntProperty.Value = -1"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void ULong_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ULongProperty"] = LuaValue.FromClrType(typeof(ULongProperty));

            ULongProperty.Value = 1234567891011UL;

            environment.Eval("assert(ULongProperty.Value == 1234567891011)");
        }

        [Fact]
        public void ULong_Set()
        {
            using var environment = new LuaEnvironment();
            environment["ULongProperty"] = LuaValue.FromClrType(typeof(ULongProperty));

            environment.Eval("ULongProperty.Value = 1234567891011");

            Assert.Equal(1234567891011UL, ULongProperty.Value);
        }

        [Fact]
        public void Float_Get()
        {
            using var environment = new LuaEnvironment();
            environment["FloatProperty"] = LuaValue.FromClrType(typeof(FloatProperty));

            FloatProperty.Value = 1.234f;

            environment.Eval("assert(FloatProperty.Value == 1.2339999675750732)");
        }

        [Fact]
        public void Float_Set()
        {
            using var environment = new LuaEnvironment();
            environment["FloatProperty"] = LuaValue.FromClrType(typeof(FloatProperty));

            environment.Eval("FloatProperty.Value = 1.234");

            Assert.Equal(1.234f, FloatProperty.Value);
        }

        [Fact]
        public void Double_Get()
        {
            using var environment = new LuaEnvironment();
            environment["DoubleProperty"] = LuaValue.FromClrType(typeof(DoubleProperty));

            DoubleProperty.Value = 1.234;

            environment.Eval("assert(DoubleProperty.Value == 1.234)");
        }

        [Fact]
        public void Double_Set()
        {
            using var environment = new LuaEnvironment();
            environment["DoubleProperty"] = LuaValue.FromClrType(typeof(DoubleProperty));

            environment.Eval("DoubleProperty.Value = 1.234");

            Assert.Equal(1.234, DoubleProperty.Value);
        }

        [Fact]
        public void String_Get()
        {
            using var environment = new LuaEnvironment();
            environment["StringProperty"] = LuaValue.FromClrType(typeof(StringProperty));

            StringProperty.Value = "test";

            environment.Eval("assert(StringProperty.Value == 'test')");
        }

        [Fact]
        public void String_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["StringProperty"] = LuaValue.FromClrType(typeof(StringProperty));

            StringProperty.Value = null;

            environment.Eval("assert(StringProperty.Value == nil)");
        }

        [Fact]
        public void String_Set()
        {
            using var environment = new LuaEnvironment();
            environment["StringProperty"] = LuaValue.FromClrType(typeof(StringProperty));

            environment.Eval("StringProperty.Value = 'test'");

            Assert.Equal("test", StringProperty.Value);
        }

        [Fact]
        public void String_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["StringProperty"] = LuaValue.FromClrType(typeof(StringProperty));

            environment.Eval("StringProperty.Value = nil");

            Assert.Null(StringProperty.Value);
        }

        [Fact]
        public void Char_Get()
        {
            using var environment = new LuaEnvironment();
            environment["CharProperty"] = LuaValue.FromClrType(typeof(CharProperty));

            CharProperty.Value = 'f';

            environment.Eval("assert(CharProperty.Value == 'f')");
        }
        
        [Fact]
        public void Char_Set()
        {
            using var environment = new LuaEnvironment();
            environment["CharProperty"] = LuaValue.FromClrType(typeof(CharProperty));

            environment.Eval("CharProperty.Value = 'f'");

            Assert.Equal('f', CharProperty.Value);
        }

        [Fact]
        public void Char_SetNotLengthOne_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["CharProperty"] = LuaValue.FromClrType(typeof(CharProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("CharProperty.Value = 'test'"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void LuaObject_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];

            LuaObjectProperty.Value = table;

            environment.Eval("assert(LuaObjectProperty.Value == table)");
        }

        [Fact]
        public void LuaObject_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            LuaObjectProperty.Value = null;

            environment.Eval("assert(LuaObjectProperty.Value == nil)");
        }

        [Fact]
        public void LuaObject_SetTable()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];

            environment.Eval("LuaObjectProperty.Value = table");

            Assert.Same(table, LuaObjectProperty.Value);
        }

        [Fact]
        public void LuaObject_SetFunction()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("func = function() end");
            var function = (LuaFunction)environment["func"];

            environment.Eval("LuaObjectProperty.Value = func");

            Assert.Same(function, LuaObjectProperty.Value);
        }

        [Fact]
        public void LuaObject_SetThread()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("thread = coroutine.create(function() end)");
            var thread = (LuaThread)environment["thread"];

            environment.Eval("LuaObjectProperty.Value = thread");

            Assert.Same(thread, LuaObjectProperty.Value);
        }

        [Fact]
        public void LuaObject_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("LuaObjectProperty.Value = nil");

            Assert.Null(LuaObjectProperty.Value);
        }

        [Fact]
        public void LuaObject_SetWrongType_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("LuaObjectProperty.Value = 1234"));
            Assert.Contains("attempt to set property 'Value' with invalid value", ex.Message);
        }

        [Fact]
        public void LuaTable_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];

            LuaObjectProperty.TableValue = table;

            environment.Eval("assert(LuaObjectProperty.TableValue == table)");
        }

        [Fact]
        public void LuaTable_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            LuaObjectProperty.TableValue = null;

            environment.Eval("assert(LuaObjectProperty.TableValue == nil)");
        }

        [Fact]
        public void LuaTable_Set()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("table = {}");
            var table = (LuaTable)environment["table"];

            environment.Eval("LuaObjectProperty.TableValue = table");

            Assert.Same(table, LuaObjectProperty.TableValue);
        }

        [Fact]
        public void LuaTable_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("LuaObjectProperty.TableValue = nil");

            Assert.Null(LuaObjectProperty.TableValue);
        }

        [Fact]
        public void LuaFunction_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("func = function() end");
            var func = (LuaFunction)environment["func"];

            LuaObjectProperty.FunctionValue = func;

            environment.Eval("assert(LuaObjectProperty.FunctionValue == func)");
        }

        [Fact]
        public void LuaFunction_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            LuaObjectProperty.FunctionValue = null;

            environment.Eval("assert(LuaObjectProperty.FunctionValue == nil)");
        }

        [Fact]
        public void LuaFunction_Set()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("func = function() end");
            var func = (LuaFunction)environment["func"];

            environment.Eval("LuaObjectProperty.FunctionValue = func");

            Assert.Same(func, LuaObjectProperty.FunctionValue);
        }

        [Fact]
        public void LuaFunction_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("LuaObjectProperty.FunctionValue = nil");

            Assert.Null(LuaObjectProperty.FunctionValue);
        }

        [Fact]
        public void LuaThread_Get()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("thread = coroutine.create(function() end)");
            var func = (LuaThread)environment["thread"];

            LuaObjectProperty.ThreadValue = func;

            environment.Eval("assert(LuaObjectProperty.ThreadValue == thread)");
        }

        [Fact]
        public void LuaThread_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            LuaObjectProperty.ThreadValue = null;

            environment.Eval("assert(LuaObjectProperty.ThreadValue == nil)");
        }

        [Fact]
        public void LuaThread_Set()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("thread = coroutine.create(function() end)");
            var func = (LuaThread)environment["thread"];

            environment.Eval("LuaObjectProperty.ThreadValue = thread");

            Assert.Same(func, LuaObjectProperty.ThreadValue);
        }

        [Fact]
        public void LuaThread_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["LuaObjectProperty"] = LuaValue.FromClrType(typeof(LuaObjectProperty));

            environment.Eval("LuaObjectProperty.ThreadValue = nil");

            Assert.Null(LuaObjectProperty.ThreadValue);
        }

        [Fact]
        public void ClrClass_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ClrClassProperty"] = LuaValue.FromClrType(typeof(ClrClassProperty));

            ClrClassProperty.ListValue = new List<int>();

            environment.Eval("assert(ClrClassProperty.ListValue ~= nil)");
        }

        [Fact]
        public void ClrClass_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["ClrClassProperty"] = LuaValue.FromClrType(typeof(ClrClassProperty));

            ClrClassProperty.ListValue = null;

            environment.Eval("assert(ClrClassProperty.ListValue == nil)");
        }

        [Fact]
        public void ClrClass_Set()
        {
            using var environment = new LuaEnvironment();
            environment["Int32"] = LuaValue.FromClrType(typeof(int));
            environment["List"] = LuaValue.FromGenericClrTypes(typeof(List<>));
            environment["ClrClassProperty"] = LuaValue.FromClrType(typeof(ClrClassProperty));

            environment.Eval("ClrClassProperty.ListValue = List[Int32]()");

            Assert.NotNull(ClrClassProperty.ListValue);
        }

        [Fact]
        public void ClrClass_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["ClrClassProperty"] = LuaValue.FromClrType(typeof(ClrClassProperty));

            environment.Eval("ClrClassProperty.ListValue = nil");

            Assert.Null(ClrClassProperty.ListValue);
        }

        [Fact]
        public void ClrStruct_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ClrStructProperty"] = LuaValue.FromClrType(typeof(ClrStructProperty));

            ClrStructProperty.DateTimeValue = new DateTime(123456789);

            environment.Eval("assert(ClrStructProperty.DateTimeValue ~= nil)");
        }

        [Fact]
        public void ClrStruct_Set()
        {
            using var environment = new LuaEnvironment();
            environment["DateTime"] = LuaValue.FromClrType(typeof(DateTime));
            environment["ClrStructProperty"] = LuaValue.FromClrType(typeof(ClrStructProperty));

            environment.Eval("ClrStructProperty.DateTimeValue = DateTime(123456789)");

            Assert.Equal(new DateTime(123456789), ClrStructProperty.DateTimeValue);
        }

        [Fact]
        public void ByRef_Get()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefProperty"] = LuaValue.FromClrType(typeof(ByRefProperty));

            ByRefProperty.Value = 1234;

            environment.Eval("assert(ByRefProperty.Value == 1234)");
        }

        [Fact]
        public void ByRef_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefProperty"] = LuaValue.FromClrType(typeof(ByRefProperty));

            environment.Eval("assert(ByRefProperty.NullValue == nil)");
        }

        [Fact]
        public void ByRef_Set()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefProperty"] = LuaValue.FromClrType(typeof(ByRefProperty));

            environment.Eval("ByRefProperty.Value = 1234");

            Assert.Equal(1234, ByRefProperty.Value);
        }

        [Fact]
        public void NullableInt_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["NullableIntProperty"] = LuaValue.FromClrType(typeof(NullableIntProperty));

            NullableIntProperty.Value = null;

            environment.Eval("assert(NullableIntProperty.Value == nil)");
        }

        [Fact]
        public void NullableInt_GetNotNull()
        {
            using var environment = new LuaEnvironment();
            environment["NullableIntProperty"] = LuaValue.FromClrType(typeof(NullableIntProperty));

            NullableIntProperty.Value = 1234;

            environment.Eval("assert(NullableIntProperty.Value == 1234)");
        }

        [Fact]
        public void ByRefNullableInt_GetNull()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefNullableIntProperty"] = LuaValue.FromClrType(typeof(ByRefNullableIntProperty));

            ByRefNullableIntProperty.Value = null;

            environment.Eval("assert(ByRefNullableIntProperty.Value == nil)");
        }

        [Fact]
        public void ByRefNullableInt_GetNotNull()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefNullableIntProperty"] = LuaValue.FromClrType(typeof(ByRefNullableIntProperty));

            ByRefNullableIntProperty.Value = 1234;

            environment.Eval("assert(ByRefNullableIntProperty.Value == 1234)");
        }

        [Fact]
        public void NullableInt_SetNull()
        {
            using var environment = new LuaEnvironment();
            environment["NullableIntProperty"] = LuaValue.FromClrType(typeof(NullableIntProperty));

            environment.Eval("NullableIntProperty.Value = nil");

            Assert.Null(NullableIntProperty.Value);
        }

        [Fact]
        public void NullableInt_SetNotNull()
        {
            using var environment = new LuaEnvironment();
            environment["NullableIntProperty"] = LuaValue.FromClrType(typeof(NullableIntProperty));

            environment.Eval("NullableIntProperty.Value = 1234");

            Assert.Equal(1234, NullableIntProperty.Value);
        }

        [Fact]
        public void NonReadable_GetPrivateGetter_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["NonReadableProperty"] = LuaValue.FromClrType(typeof(NonReadableProperty));

            var ex = Assert.Throws<LuaRuntimeException>(
                () => environment.Eval("_ = NonReadableProperty.PrivateGetter"));
            Assert.Contains("attempt to get non-readable property 'PrivateGetter'", ex.Message);
        }

        [Fact]
        public void NonReadable_GetNoGetter_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["NonReadableProperty"] = LuaValue.FromClrType(typeof(NonReadableProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("_ = NonReadableProperty.NoGetter"));
            Assert.Contains("attempt to get non-readable property 'NoGetter'", ex.Message);
        }

        [Fact]
        public void NonWritable_GetPrivateSetter_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["NonWritableProperty"] = LuaValue.FromClrType(typeof(NonWritableProperty));

            var ex = Assert.Throws<LuaRuntimeException>(
                () => environment.Eval("NonWritableProperty.PrivateSetter = 1234"));
            Assert.Contains("attempt to set non-writable property 'PrivateSetter'", ex.Message);
        }

        [Fact]
        public void NonWritable_GetNoSetter_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["NonWritableProperty"] = LuaValue.FromClrType(typeof(NonWritableProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("NonWritableProperty.NoSetter = 1234"));
            Assert.Contains("attempt to set non-writable property 'NoSetter'", ex.Message);
        }

        [Fact]
        public void ByRefLike_Get_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefLikeProperty"] = LuaValue.FromClrType(typeof(ByRefLikeProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("_ = ByRefLikeProperty.Value"));
            Assert.Contains("attempt to get byref-like property 'Value'", ex.Message);
        }

        [Fact]
        public void ByRefLike_Set_RaisesLuaError()
        {
            using var environment = new LuaEnvironment();
            environment["ByRefLikeProperty"] = LuaValue.FromClrType(typeof(ByRefLikeProperty));

            var ex = Assert.Throws<LuaRuntimeException>(() => environment.Eval("ByRefLikeProperty.Value = nil"));
            Assert.Contains("attempt to set byref-like property 'Value'", ex.Message);
        }
    }
}
