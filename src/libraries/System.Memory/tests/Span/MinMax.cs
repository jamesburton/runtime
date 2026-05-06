// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void Min_EmptySpan_ValueType_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => ReadOnlySpan<int>.Empty.Min());
            Assert.Throws<InvalidOperationException>(() => ReadOnlySpan<int>.Empty.Min(Comparer<int>.Default));
        }

        [Fact]
        public static void Max_EmptySpan_ValueType_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => ReadOnlySpan<int>.Empty.Max());
            Assert.Throws<InvalidOperationException>(() => ReadOnlySpan<int>.Empty.Max(Comparer<int>.Default));
        }

        [Fact]
        public static void Min_EmptySpan_ReferenceType_ReturnsNull()
        {
            Assert.Null(ReadOnlySpan<string>.Empty.Min());
            Assert.Null(ReadOnlySpan<string>.Empty.Min(Comparer<string>.Default));
        }

        [Fact]
        public static void Max_EmptySpan_ReferenceType_ReturnsNull()
        {
            Assert.Null(ReadOnlySpan<string>.Empty.Max());
            Assert.Null(ReadOnlySpan<string>.Empty.Max(Comparer<string>.Default));
        }

        [Fact]
        public static void Min_EmptySpan_NullableValueType_ReturnsNull()
        {
            Assert.Null(ReadOnlySpan<int?>.Empty.Min());
            Assert.Null(ReadOnlySpan<int?>.Empty.Min(Comparer<int?>.Default));
        }

        [Fact]
        public static void Max_EmptySpan_NullableValueType_ReturnsNull()
        {
            Assert.Null(ReadOnlySpan<int?>.Empty.Max());
            Assert.Null(ReadOnlySpan<int?>.Empty.Max(Comparer<int?>.Default));
        }

        [Theory]
        [InlineData(new int[] { 5 }, 5)]
        [InlineData(new int[] { 3, 1, 4, 1, 5, 9, 2, 6 }, 1)]
        [InlineData(new int[] { int.MinValue, 0, int.MaxValue }, int.MinValue)]
        [InlineData(new int[] { -1, -2, -3 }, -3)]
        public static void Min_Int32(int[] values, int expected)
        {
            Assert.Equal(expected, ((ReadOnlySpan<int>)values).Min());
            Assert.Equal(expected, ((ReadOnlySpan<int>)values).Min(null));
            Assert.Equal(expected, ((ReadOnlySpan<int>)values).Min(Comparer<int>.Default));
        }

        [Theory]
        [InlineData(new int[] { 5 }, 5)]
        [InlineData(new int[] { 3, 1, 4, 1, 5, 9, 2, 6 }, 9)]
        [InlineData(new int[] { int.MinValue, 0, int.MaxValue }, int.MaxValue)]
        [InlineData(new int[] { -1, -2, -3 }, -1)]
        public static void Max_Int32(int[] values, int expected)
        {
            Assert.Equal(expected, ((ReadOnlySpan<int>)values).Max());
            Assert.Equal(expected, ((ReadOnlySpan<int>)values).Max(null));
            Assert.Equal(expected, ((ReadOnlySpan<int>)values).Max(Comparer<int>.Default));
        }

        [Fact]
        public static void Min_Long()
        {
            ReadOnlySpan<long> span = new long[] { 3L, 1L, 4L, 1L, 5L };
            Assert.Equal(1L, span.Min());
        }

        [Fact]
        public static void Max_Long()
        {
            ReadOnlySpan<long> span = new long[] { 3L, 1L, 4L, 1L, 5L };
            Assert.Equal(5L, span.Max());
        }

        [Fact]
        public static void Min_Byte()
        {
            ReadOnlySpan<byte> span = new byte[] { 200, 50, 100, 255, 0 };
            Assert.Equal((byte)0, span.Min());
        }

        [Fact]
        public static void Max_Byte()
        {
            ReadOnlySpan<byte> span = new byte[] { 200, 50, 100, 255, 0 };
            Assert.Equal((byte)255, span.Max());
        }

        [Fact]
        public static void Min_Float_Basic()
        {
            ReadOnlySpan<float> span = new float[] { 3.0f, 1.0f, 4.0f, 1.5f, 5.0f };
            Assert.Equal(1.0f, span.Min());
        }

        [Fact]
        public static void Max_Float_Basic()
        {
            ReadOnlySpan<float> span = new float[] { 3.0f, 1.0f, 4.0f, 1.5f, 5.0f };
            Assert.Equal(5.0f, span.Max());
        }

        [Fact]
        public static void Min_Float_NaN_ReturnsNaN()
        {
            ReadOnlySpan<float> span1 = new float[] { float.NaN, 1.0f, 2.0f };
            Assert.True(float.IsNaN((float)span1.Min()!));

            ReadOnlySpan<float> span2 = new float[] { 1.0f, float.NaN, 2.0f };
            Assert.True(float.IsNaN((float)span2.Min()!));

            ReadOnlySpan<float> span3 = new float[] { 1.0f, 2.0f, float.NaN };
            Assert.True(float.IsNaN((float)span3.Min()!));
        }

        [Fact]
        public static void Max_Float_NaN_Skipped()
        {
            ReadOnlySpan<float> span1 = new float[] { float.NaN, 1.0f, 2.0f };
            Assert.Equal(2.0f, span1.Max());

            ReadOnlySpan<float> span2 = new float[] { 1.0f, float.NaN, 2.0f };
            Assert.Equal(2.0f, span2.Max());

            ReadOnlySpan<float> span3 = new float[] { 1.0f, 2.0f, float.NaN };
            Assert.Equal(2.0f, span3.Max());
        }

        [Fact]
        public static void Max_Float_AllNaN_ReturnsNaN()
        {
            ReadOnlySpan<float> span = new float[] { float.NaN, float.NaN, float.NaN };
            Assert.True(float.IsNaN((float)span.Max()!));
        }

        [Fact]
        public static void Min_Double_NaN_ReturnsNaN()
        {
            ReadOnlySpan<double> span = new double[] { 1.0, double.NaN, 2.0 };
            Assert.True(double.IsNaN((double)span.Min()!));
        }

        [Fact]
        public static void Max_Double_NaN_Skipped()
        {
            ReadOnlySpan<double> span = new double[] { double.NaN, 1.0, 2.0 };
            Assert.Equal(2.0, span.Max());
        }

        [Fact]
        public static void Min_Float_NegativeInfinity()
        {
            ReadOnlySpan<float> span = new float[] { 1.0f, float.NegativeInfinity, 2.0f };
            Assert.Equal(float.NegativeInfinity, span.Min());
        }

        [Fact]
        public static void Max_Float_PositiveInfinity()
        {
            ReadOnlySpan<float> span = new float[] { 1.0f, float.PositiveInfinity, 2.0f };
            Assert.Equal(float.PositiveInfinity, span.Max());
        }

        [Fact]
        public static void Min_String()
        {
            ReadOnlySpan<string> span = new string[] { "banana", "apple", "cherry" };
            Assert.Equal("apple", span.Min());
        }

        [Fact]
        public static void Max_String()
        {
            ReadOnlySpan<string> span = new string[] { "banana", "apple", "cherry" };
            Assert.Equal("cherry", span.Max());
        }

        [Fact]
        public static void Min_String_WithNulls_SkipsNulls()
        {
            ReadOnlySpan<string?> span = new string?[] { null, "banana", null, "apple", null };
            Assert.Equal("apple", span.Min());
        }

        [Fact]
        public static void Max_String_WithNulls_SkipsNulls()
        {
            ReadOnlySpan<string?> span = new string?[] { null, "banana", null, "apple", null };
            Assert.Equal("banana", span.Max());
        }

        [Fact]
        public static void Min_String_AllNull_ReturnsNull()
        {
            ReadOnlySpan<string?> span = new string?[] { null, null, null };
            Assert.Null(span.Min());
        }

        [Fact]
        public static void Max_String_AllNull_ReturnsNull()
        {
            ReadOnlySpan<string?> span = new string?[] { null, null, null };
            Assert.Null(span.Max());
        }

        [Fact]
        public static void Min_CustomComparer()
        {
            int[] values = { 3, 1, 4, 1, 5 };
            var reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            Assert.Equal(5, ((ReadOnlySpan<int>)values).Min(reverseComparer));
        }

        [Fact]
        public static void Max_CustomComparer()
        {
            int[] values = { 3, 1, 4, 1, 5 };
            var reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            Assert.Equal(1, ((ReadOnlySpan<int>)values).Max(reverseComparer));
        }

        [Fact]
        public static void Min_LargeSpan_Int()
        {
            int[] values = Enumerable.Range(0, 1000).Reverse().ToArray();
            Assert.Equal(0, ((ReadOnlySpan<int>)values).Min());
        }

        [Fact]
        public static void Max_LargeSpan_Int()
        {
            int[] values = Enumerable.Range(0, 1000).ToArray();
            Assert.Equal(999, ((ReadOnlySpan<int>)values).Max());
        }

        [Fact]
        public static void Min_SingleElement()
        {
            ReadOnlySpan<int> span = new int[] { 42 };
            Assert.Equal(42, span.Min());
        }

        [Fact]
        public static void Max_SingleElement()
        {
            ReadOnlySpan<int> span = new int[] { 42 };
            Assert.Equal(42, span.Max());
        }

        [Fact]
        public static void Min_DateTimeOffset()
        {
            DateTimeOffset dt1 = new DateTimeOffset(2026, 3, 14, 16, 18, 4, TimeSpan.FromHours(1));
            DateTimeOffset dt2 = new DateTimeOffset(2026, 4, 16, 0, 50, 35, TimeSpan.FromHours(1));
            DateTimeOffset dt3 = new DateTimeOffset(2026, 3, 23, 18, 13, 43, TimeSpan.FromHours(1));
            ReadOnlySpan<DateTimeOffset> span = new DateTimeOffset[] { dt1, dt2, dt3 };
            Assert.Equal(dt1, span.Min());
        }

        [Fact]
        public static void Max_DateTimeOffset()
        {
            DateTimeOffset dt1 = new DateTimeOffset(2026, 3, 14, 16, 18, 4, TimeSpan.FromHours(1));
            DateTimeOffset dt2 = new DateTimeOffset(2026, 4, 16, 0, 50, 35, TimeSpan.FromHours(1));
            DateTimeOffset dt3 = new DateTimeOffset(2026, 3, 23, 18, 13, 43, TimeSpan.FromHours(1));
            ReadOnlySpan<DateTimeOffset> span = new DateTimeOffset[] { dt1, dt2, dt3 };
            Assert.Equal(dt2, span.Max());
        }

        [Fact]
        public static void Min_MatchesEnumerableMin()
        {
            int[] values = { 7, 2, 9, 4, 1, 8, 3, 6, 5 };
            Assert.Equal(values.Min(), ((ReadOnlySpan<int>)values).Min());
        }

        [Fact]
        public static void Max_MatchesEnumerableMax()
        {
            int[] values = { 7, 2, 9, 4, 1, 8, 3, 6, 5 };
            Assert.Equal(values.Max(), ((ReadOnlySpan<int>)values).Max());
        }

        [Fact]
        public static void Min_Float_MatchesEnumerableMin()
        {
            float[] values = { 3.14f, float.NaN, 2.71f, 1.0f };
            float expected = values.Min();
            float? actual = ((ReadOnlySpan<float>)values).Min();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Max_Float_MatchesEnumerableMax()
        {
            float[] values = { 3.14f, float.NaN, 2.71f, 1.0f };
            float expected = values.Max();
            float? actual = ((ReadOnlySpan<float>)values).Max();
            Assert.Equal(expected, actual);
        }
    }
}
