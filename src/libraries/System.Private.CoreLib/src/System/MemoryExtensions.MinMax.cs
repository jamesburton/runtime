// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System
{
    public static partial class MemoryExtensions
    {
        /// <summary>Returns the minimum value in a span.</summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">A span of values to determine the minimum value of.</param>
        /// <returns>The minimum value in the span.</returns>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="T"/> is a non-nullable value type and <paramref name="span"/> is empty.
        /// </exception>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is a reference type or a nullable value type and the span is empty or contains only values that are <see langword="null"/>, this method returns <see langword="null"/>.</para>
        /// <para>This method uses <see cref="Comparer{T}.Default"/> to compare values.</para>
        /// </remarks>
        public static T? Min<T>(this ReadOnlySpan<T> span) =>
            Min(span, comparer: null);

        /// <summary>Returns the minimum value in a span according to a specified comparer.</summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">A span of values to determine the minimum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to compare values, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
        /// <returns>The minimum value in the span.</returns>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="T"/> is a non-nullable value type and <paramref name="span"/> is empty.
        /// </exception>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is a reference type or a nullable value type and the span is empty or contains only values that are <see langword="null"/>, this method returns <see langword="null"/>.</para>
        /// </remarks>
        public static T? Min<T>(this ReadOnlySpan<T> span, IComparer<T>? comparer)
        {
            if (comparer is null || comparer == Comparer<T>.Default)
            {
                if (typeof(T) == typeof(byte)) return (T)(object)MinMaxIntegerCore<byte, MinCalc<byte>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(sbyte)) return (T)(object)MinMaxIntegerCore<sbyte, MinCalc<sbyte>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, sbyte>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(ushort)) return (T)(object)MinMaxIntegerCore<ushort, MinCalc<ushort>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(short)) return (T)(object)MinMaxIntegerCore<short, MinCalc<short>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(char)) return (T)(object)MinMaxIntegerCore<char, MinCalc<char>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(uint)) return (T)(object)MinMaxIntegerCore<uint, MinCalc<uint>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(int)) return (T)(object)MinMaxIntegerCore<int, MinCalc<int>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(ulong)) return (T)(object)MinMaxIntegerCore<ulong, MinCalc<ulong>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(long)) return (T)(object)MinMaxIntegerCore<long, MinCalc<long>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(nint)) return (T)(object)MinMaxIntegerCore<nint, MinCalc<nint>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, nint>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(nuint)) return (T)(object)MinMaxIntegerCore<nuint, MinCalc<nuint>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, nuint>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(Int128)) return (T)(object)MinMaxIntegerCore<Int128, MinCalc<Int128>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, Int128>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(UInt128)) return (T)(object)MinMaxIntegerCore<UInt128, MinCalc<UInt128>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, UInt128>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(float)) return (T)(object)MinFloatCore(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, float>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(double)) return (T)(object)MinFloatCore(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, double>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(Half)) return (T)(object)MinFloatCore(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, Half>(ref MemoryMarshal.GetReference(span)), span.Length));
            }

            return MinMaxCore(span, comparer ?? Comparer<T>.Default, sign: 1);
        }

        /// <summary>Returns the maximum value in a span.</summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">A span of values to determine the maximum value of.</param>
        /// <returns>The maximum value in the span.</returns>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="T"/> is a non-nullable value type and <paramref name="span"/> is empty.
        /// </exception>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is a reference type or a nullable value type and the span is empty or contains only values that are <see langword="null"/>, this method returns <see langword="null"/>.</para>
        /// <para>This method uses <see cref="Comparer{T}.Default"/> to compare values.</para>
        /// </remarks>
        public static T? Max<T>(this ReadOnlySpan<T> span) =>
            Max(span, comparer: null);

        /// <summary>Returns the maximum value in a span according to a specified comparer.</summary>
        /// <typeparam name="T">The type of the elements in the span.</typeparam>
        /// <param name="span">A span of values to determine the maximum value of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> to compare values, or <see langword="null"/> to use <see cref="Comparer{T}.Default"/>.</param>
        /// <returns>The maximum value in the span.</returns>
        /// <exception cref="InvalidOperationException">
        /// <typeparamref name="T"/> is a non-nullable value type and <paramref name="span"/> is empty.
        /// </exception>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is a reference type or a nullable value type and the span is empty or contains only values that are <see langword="null"/>, this method returns <see langword="null"/>.</para>
        /// </remarks>
        public static T? Max<T>(this ReadOnlySpan<T> span, IComparer<T>? comparer)
        {
            if (comparer is null || comparer == Comparer<T>.Default)
            {
                if (typeof(T) == typeof(byte)) return (T)(object)MinMaxIntegerCore<byte, MaxCalc<byte>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(sbyte)) return (T)(object)MinMaxIntegerCore<sbyte, MaxCalc<sbyte>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, sbyte>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(ushort)) return (T)(object)MinMaxIntegerCore<ushort, MaxCalc<ushort>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(short)) return (T)(object)MinMaxIntegerCore<short, MaxCalc<short>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, short>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(char)) return (T)(object)MinMaxIntegerCore<char, MaxCalc<char>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(uint)) return (T)(object)MinMaxIntegerCore<uint, MaxCalc<uint>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, uint>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(int)) return (T)(object)MinMaxIntegerCore<int, MaxCalc<int>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, int>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(ulong)) return (T)(object)MinMaxIntegerCore<ulong, MaxCalc<ulong>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ulong>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(long)) return (T)(object)MinMaxIntegerCore<long, MaxCalc<long>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, long>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(nint)) return (T)(object)MinMaxIntegerCore<nint, MaxCalc<nint>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, nint>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(nuint)) return (T)(object)MinMaxIntegerCore<nuint, MaxCalc<nuint>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, nuint>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(Int128)) return (T)(object)MinMaxIntegerCore<Int128, MaxCalc<Int128>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, Int128>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(UInt128)) return (T)(object)MinMaxIntegerCore<UInt128, MaxCalc<UInt128>>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, UInt128>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(float)) return (T)(object)MaxFloatCore(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, float>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(double)) return (T)(object)MaxFloatCore(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, double>(ref MemoryMarshal.GetReference(span)), span.Length));
                if (typeof(T) == typeof(Half)) return (T)(object)MaxFloatCore(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, Half>(ref MemoryMarshal.GetReference(span)), span.Length));
            }

            return MinMaxCore(span, comparer ?? Comparer<T>.Default, sign: -1);
        }

        /// <summary>Core implementation for Min/Max over a span using IComparer.</summary>
        private static T? MinMaxCore<T>(ReadOnlySpan<T> span, IComparer<T> comparer, int sign)
        {
            T? value = default;
            if (value is null)
            {
                // T is a reference type or nullable value type.
                // For empty spans or spans containing only nulls, return null.
                int i = 0;
                for (; i < span.Length; i++)
                {
                    value = span[i];
                    if (value is not null)
                    {
                        break;
                    }
                }

                if (value is null)
                {
                    return value;
                }

                for (i++; i < span.Length; i++)
                {
                    T next = span[i];
                    if (next is not null && (comparer.Compare(next, value) * sign) < 0)
                    {
                        value = next;
                    }
                }
            }
            else
            {
                // T is a non-nullable value type.
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowInvalidOperationException_NoElements();
                }

                value = span[0];
                if (comparer == Comparer<T>.Default)
                {
                    for (int i = 1; i < span.Length; i++)
                    {
                        T next = span[i];
                        if ((Comparer<T>.Default.Compare(next, value) * sign) < 0)
                        {
                            value = next;
                        }
                    }
                }
                else
                {
                    for (int i = 1; i < span.Length; i++)
                    {
                        T next = span[i];
                        if ((comparer.Compare(next, value) * sign) < 0)
                        {
                            value = next;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>Vectorized Min/Max for integer types.</summary>
        private static T MinMaxIntegerCore<T, TMinMax>(ReadOnlySpan<T> span)
            where T : struct, IBinaryInteger<T>
            where TMinMax : IMinMaxCalc<T>
        {
            if (span.IsEmpty)
            {
                ThrowHelper.ThrowInvalidOperationException_NoElements();
            }

            T value;

            if (!Vector128.IsHardwareAccelerated || !Vector128<T>.IsSupported || span.Length < Vector128<T>.Count)
            {
                value = span[0];
                for (int i = 1; i < span.Length; i++)
                {
                    if (TMinMax.Compare(span[i], value))
                    {
                        value = span[i];
                    }
                }
            }
            else if (!Vector256.IsHardwareAccelerated || !Vector256<T>.IsSupported || span.Length < Vector256<T>.Count)
            {
                ref T current = ref MemoryMarshal.GetReference(span);
                ref T lastVectorStart = ref Unsafe.Add(ref current, span.Length - Vector128<T>.Count);

                Vector128<T> best = Vector128.LoadUnsafe(ref current);
                current = ref Unsafe.Add(ref current, Vector128<T>.Count);

                while (Unsafe.IsAddressLessThan(ref current, ref lastVectorStart))
                {
                    best = TMinMax.Compare(best, Vector128.LoadUnsafe(ref current));
                    current = ref Unsafe.Add(ref current, Vector128<T>.Count);
                }
                best = TMinMax.Compare(best, Vector128.LoadUnsafe(ref lastVectorStart));

                value = best[0];
                for (int i = 1; i < Vector128<T>.Count; i++)
                {
                    if (TMinMax.Compare(best[i], value))
                    {
                        value = best[i];
                    }
                }
            }
            else if (!Vector512.IsHardwareAccelerated || !Vector512<T>.IsSupported || span.Length < Vector512<T>.Count)
            {
                ref T current = ref MemoryMarshal.GetReference(span);
                ref T lastVectorStart = ref Unsafe.Add(ref current, span.Length - Vector256<T>.Count);

                Vector256<T> best = Vector256.LoadUnsafe(ref current);
                current = ref Unsafe.Add(ref current, Vector256<T>.Count);

                while (Unsafe.IsAddressLessThan(ref current, ref lastVectorStart))
                {
                    best = TMinMax.Compare(best, Vector256.LoadUnsafe(ref current));
                    current = ref Unsafe.Add(ref current, Vector256<T>.Count);
                }
                best = TMinMax.Compare(best, Vector256.LoadUnsafe(ref lastVectorStart));

                value = best[0];
                for (int i = 1; i < Vector256<T>.Count; i++)
                {
                    if (TMinMax.Compare(best[i], value))
                    {
                        value = best[i];
                    }
                }
            }
            else
            {
                ref T current = ref MemoryMarshal.GetReference(span);
                ref T lastVectorStart = ref Unsafe.Add(ref current, span.Length - Vector512<T>.Count);

                Vector512<T> best = Vector512.LoadUnsafe(ref current);
                current = ref Unsafe.Add(ref current, Vector512<T>.Count);

                while (Unsafe.IsAddressLessThan(ref current, ref lastVectorStart))
                {
                    best = TMinMax.Compare(best, Vector512.LoadUnsafe(ref current));
                    current = ref Unsafe.Add(ref current, Vector512<T>.Count);
                }
                best = TMinMax.Compare(best, Vector512.LoadUnsafe(ref lastVectorStart));

                value = best[0];
                for (int i = 1; i < Vector512<T>.Count; i++)
                {
                    if (TMinMax.Compare(best[i], value))
                    {
                        value = best[i];
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Min for floating-point spans. NaN is ordered as smaller than all other values
        /// (matching Enumerable.Min behavior).
        /// </summary>
        private static T MinFloatCore<T>(ReadOnlySpan<T> span) where T : struct, IFloatingPointIeee754<T>
        {
            if (span.IsEmpty)
            {
                ThrowHelper.ThrowInvalidOperationException_NoElements();
            }

            T value = span[0];
            for (int i = 1; (uint)i < (uint)span.Length; i++)
            {
                T current = span[i];
                if (current < value)
                {
                    value = current;
                }
                else if (T.IsNaN(current))
                {
                    return current;
                }
            }

            return value;
        }

        /// <summary>
        /// Max for floating-point spans. NaN is ordered as smaller than all other values
        /// (matching Enumerable.Max behavior) - NaN values are skipped unless all values are NaN.
        /// </summary>
        private static T MaxFloatCore<T>(ReadOnlySpan<T> span) where T : struct, IFloatingPointIeee754<T>
        {
            if (span.IsEmpty)
            {
                ThrowHelper.ThrowInvalidOperationException_NoElements();
            }

            int i;
            for (i = 0; i < span.Length && T.IsNaN(span[i]); i++) ;

            if (i == span.Length)
            {
                return span[^1];
            }

            T value;
            for (value = span[i]; (uint)i < (uint)span.Length; i++)
            {
                if (span[i] > value)
                {
                    value = span[i];
                }
            }

            return value;
        }

        private interface IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            static abstract bool Compare(T left, T right);
            static abstract Vector128<T> Compare(Vector128<T> left, Vector128<T> right);
            static abstract Vector256<T> Compare(Vector256<T> left, Vector256<T> right);
            static abstract Vector512<T> Compare(Vector512<T> left, Vector512<T> right);
        }

        private readonly struct MinCalc<T> : IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            public static bool Compare(T left, T right) => left < right;
            public static Vector128<T> Compare(Vector128<T> left, Vector128<T> right) => Vector128.Min(left, right);
            public static Vector256<T> Compare(Vector256<T> left, Vector256<T> right) => Vector256.Min(left, right);
            public static Vector512<T> Compare(Vector512<T> left, Vector512<T> right) => Vector512.Min(left, right);
        }

        private readonly struct MaxCalc<T> : IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            public static bool Compare(T left, T right) => left > right;
            public static Vector128<T> Compare(Vector128<T> left, Vector128<T> right) => Vector128.Max(left, right);
            public static Vector256<T> Compare(Vector256<T> left, Vector256<T> right) => Vector256.Max(left, right);
            public static Vector512<T> Compare(Vector512<T> left, Vector512<T> right) => Vector512.Max(left, right);
        }
    }
}
