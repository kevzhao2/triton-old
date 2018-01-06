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
using Xunit;

namespace Triton.Tests.Binding {
    public class OperatorTests {
        private class Complex {
            public double Real { get; }
            public double Imaginary { get; }
            public double RadiusSq => Real * Real + Imaginary * Imaginary;

            public Complex(double real, double imaginary) {
                Real = real;
                Imaginary = imaginary;
            }

            public override string ToString() => $"{Real} + {Imaginary}i";

            #region Equality
            public override bool Equals(object obj) {
                return obj is Complex c && Real == c.Real && Imaginary == c.Imaginary;
            }
            public override int GetHashCode() {
                return Real.GetHashCode() ^ Imaginary.GetHashCode();
            }

            public static bool operator ==(Complex c1, Complex c2) => c1.Equals(c2);
            public static bool operator !=(Complex c1, Complex c2) => !c1.Equals(c2);
            public static bool operator <=(Complex c1, Complex c2) => c1.RadiusSq <= c2.RadiusSq;
            public static bool operator >=(Complex c1, Complex c2) => c1.RadiusSq >= c2.RadiusSq;
            public static bool operator <(Complex c1, Complex c2) => c1.RadiusSq < c2.RadiusSq;
            public static bool operator >(Complex c1, Complex c2) => c1.RadiusSq > c2.RadiusSq;
            #endregion

            #region Addition
            public static Complex operator +(Complex c1, Complex c2) {
                return new Complex(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
            }
            public static Complex operator +(Complex c, double d) {
                return new Complex(c.Real + d, c.Imaginary);
            }
            public static Complex operator +(Complex c, int i) {
                return new Complex(c.Real + i, c.Imaginary);
            }
            public static Complex operator +(double d, Complex c) {
                return new Complex(d + c.Real, c.Imaginary);
            }
            public static Complex operator +(int i, Complex c) {
                return new Complex(i + c.Real, c.Imaginary);
            }
            #endregion

            #region Subtraction
            public static Complex operator -(Complex c1, Complex c2) {
                return new Complex(c1.Real - c2.Real, c1.Imaginary - c2.Imaginary);
            }
            public static Complex operator -(Complex c, double d) {
                return new Complex(c.Real - d, c.Imaginary);
            }
            public static Complex operator -(Complex c, int i) {
                return new Complex(c.Real - i, c.Imaginary);
            }
            public static Complex operator -(double d, Complex c) {
                return new Complex(d - c.Real, c.Imaginary);
            }
            public static Complex operator -(int i, Complex c) {
                return new Complex(i - c.Real, c.Imaginary);
            }
            #endregion

            #region Multiplication
            public static Complex operator *(Complex c1, Complex c2) {
                return new Complex(c1.Real * c2.Real - c1.Imaginary * c2.Imaginary,
                                   c1.Real * c2.Imaginary + c2.Real * c1.Imaginary);
            }
            public static Complex operator *(Complex c, double d) {
                return new Complex(d * c.Real, d * c.Imaginary);
            }
            public static Complex operator *(Complex c, int i) {
                return new Complex(i * c.Real, i * c.Imaginary);
            }
            public static Complex operator *(double d, Complex c) {
                return new Complex(d * c.Real, d * c.Imaginary);
            }
            public static Complex operator *(int i, Complex c) {
                return new Complex(i * c.Real, i * c.Imaginary);
            }
            #endregion

            #region Division
            public static Complex operator /(Complex c1, Complex c2) {
                var denom = c2.Real * c2.Real + c2.Imaginary + c2.Imaginary;
                return new Complex((c1.Real * c2.Real + c1.Imaginary * c2.Imaginary) / denom,
                                   (c1.Imaginary * c2.Real - c1.Real * c2.Imaginary) / denom);
            }
            public static Complex operator /(Complex c, double d) {
                return new Complex(c.Real / d, c.Imaginary / d);
            }
            public static Complex operator /(Complex c, int i) {
                return new Complex(c.Real / i, c.Imaginary / i);
            }
            public static Complex operator /(double d, Complex c) {
                return new Complex(d, 0) / c;
            }
            public static Complex operator /(int i, Complex c) {
                return new Complex(i, 0) / c;
            }
            #endregion

            #region Unary Negation
            public static Complex operator -(Complex c) {
                return new Complex(-c.Real, -c.Imaginary);
            }
            #endregion

            public static int operator ~(Complex c) => throw new NotImplementedException();
            public static int operator %(Complex c, int d) => throw new NotImplementedException();
        }

        private class TestOp {
            public int X { get; }

            public TestOp(int x) => X = x;

            public static TestOp operator %(TestOp op, int x) => new TestOp(op.X % x);
            public static TestOp operator &(TestOp op, int x) => new TestOp(op.X & x);
            public static TestOp operator |(TestOp op, int x) => new TestOp(op.X | x);
            public static TestOp operator ^(TestOp op, int x) => new TestOp(op.X ^ x);
            public static TestOp operator ~(TestOp op) => new TestOp(~op.X);
            public static TestOp operator <<(TestOp op, int x) => new TestOp(op.X << x);
            public static TestOp operator >>(TestOp op, int x) => new TestOp(op.X >> x);
        }

        [Fact]
        public void Addition_TwoObjs() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);
                lua["c2"] = new Complex(1, 10);

                lua.DoString("c3 = c1 + c2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(11.0, c3.Real);
                Assert.Equal(11.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Addition_ObjNumber() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = c1 + 1.2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(11.2, c3.Real);
                Assert.Equal(1.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Addition_NumberObj() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = 1.6 + c1");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(11.6, c3.Real);
                Assert.Equal(1.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Subtraction_TwoObjs() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);
                lua["c2"] = new Complex(1, 10);

                lua.DoString("c3 = c1 - c2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(9.0, c3.Real);
                Assert.Equal(-9.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Subtraction_ObjNumber() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = c1 - 1.2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(8.8, c3.Real);
                Assert.Equal(1.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Subtraction_NumberObj() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = 1.6 - c1");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(-8.4, c3.Real);
                Assert.Equal(1.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Multiplication_TwoObjs() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);
                lua["c2"] = new Complex(1, 10);

                lua.DoString("c3 = c1 * c2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(0.0, c3.Real);
                Assert.Equal(101.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Multiplication_ObjNumber() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = c1 * 2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(20.0, c3.Real);
                Assert.Equal(2.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Multiplication_NumberObj() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = 0.5 * c1");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(5.0, c3.Real);
                Assert.Equal(0.5, c3.Imaginary);
            }
        }

        [Fact]
        public void Division_TwoObjs() {
            using (var lua = new Lua()) {
                var c1 = new Complex(10, 1);
                var c2 = new Complex(1, 10);
                lua["c1"] = c1;
                lua["c2"] = c2;

                lua.DoString("c3 = c1 / c2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal((c1 / c2).Real, c3.Real);
                Assert.Equal((c1 / c2).Imaginary, c3.Imaginary);
            }
        }

        [Fact]
        public void Division_ObjNumber() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = c1 / 2");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(5.0, c3.Real);
                Assert.Equal(0.5, c3.Imaginary);
            }
        }

        [Fact]
        public void Division_NumberObj() {
            using (var lua = new Lua()) {
                var c1 = new Complex(10, 1);
                lua["c1"] = c1;

                lua.DoString("c3 = 1 / c1");

                var c3 = lua["c3"] as Complex;
                Assert.Equal((1 / c1).Real, c3.Real);
                Assert.Equal((1 / c1).Imaginary, c3.Imaginary);
            }
        }

        [Fact]
        public void UnaryNegation() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);

                lua.DoString("c3 = -c1");

                var c3 = lua["c3"] as Complex;
                Assert.Equal(-10.0, c3.Real);
                Assert.Equal(-1.0, c3.Imaginary);
            }
        }

        [Fact]
        public void Equality() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);
                lua["c2"] = new Complex(10, 1);

                lua.DoString("b = c1 == c2");

                Assert.True((bool)lua["b"]);
            }
        }

        [Fact]
        public void LessThan() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(11, 1);
                lua["c2"] = new Complex(10, 1);

                lua.DoString("b = c1 < c2");

                Assert.False((bool)lua["b"]);
            }
        }

        [Fact]
        public void LessThanOrEqual() {
            using (var lua = new Lua()) {
                lua["c1"] = new Complex(10, 1);
                lua["c2"] = new Complex(10, 1);

                lua.DoString("b = c1 <= c2");

                Assert.True((bool)lua["b"]);
            }
        }

        [Fact]
        public void Modulus() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (x % 200).X");

                Assert.Equal(1156L % 200, lua["b"]);
            }
        }

        [Fact]
        public void BitwiseAnd() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (x & 1678).X");

                Assert.Equal(1156L & 1678L, lua["b"]);
            }
        }

        [Fact]
        public void BitwiseOr() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (x | 1678).X");

                Assert.Equal(1156L | 1678L, lua["b"]);
            }
        }

        [Fact]
        public void BitwiseXor() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (x ~ 1678).X");

                Assert.Equal(1156L ^ 1678L, lua["b"]);
            }
        }

        [Fact]
        public void BitwiseNot() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (~x).X");

                Assert.Equal(~1156L, lua["b"]);
            }
        }

        [Fact]
        public void LeftShift() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (x << 5).X");

                Assert.Equal(1156L << 5, lua["b"]);
            }
        }

        [Fact]
        public void RightShift() {
            using (var lua = new Lua()) {
                lua["x"] = new TestOp(1156);

                lua.DoString("b = (x >> 6).X");

                Assert.Equal(1156L >> 6, lua["b"]);
            }
        }

        [Fact]
        public void BinaryOp_ThrowsException() {
            using (var lua = new Lua()) {
                lua["x"] = new Complex(1, 5);

                Assert.Throws<LuaException>(() => lua.DoString("a = x % 5"));
            }
        }

        [Fact]
        public void BinaryOp_Invalid() {
            using (var lua = new Lua()) {
                lua["x"] = new object();

                Assert.Throws<LuaException>(() => lua.DoString("a = x + x"));
            }
        }

        [Fact]
        public void UnaryOp_ThrowsException() {
            using (var lua = new Lua()) {
                lua["x"] = new Complex(1, 5);

                Assert.Throws<LuaException>(() => lua.DoString("a = ~x"));
            }
        }

        [Fact]
        public void UnaryOp_Invalid() {
            using (var lua = new Lua()) {
                lua["x"] = new object();

                Assert.Throws<LuaException>(() => lua.DoString("a = ~x"));
            }
        }
    }
}
