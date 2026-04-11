// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

/// <summary>
/// Tests for the ARM64 JIT intrinsic expansion of Vector128.IndexOfWhereAllBitsSet
/// and Vector128.LastIndexOfWhereAllBitsSet, covering both the generic path (raw vectors)
/// and the optimized SHRN fast-path (input from comparisons / bitwise combos).
/// </summary>
public static class IndexOfWhereAllBitsSet
{
    // ===================== IndexOfWhereAllBitsSet: generic path =====================

    [Fact]
    public static void IndexOf_Byte_Generic()
    {
        var v = Vector128.Create((byte)0, 0, 0, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.Equal(3, GenericByte(v));
    }

    [Fact]
    public static void IndexOf_Int_Generic()
    {
        Assert.Equal(1, GenericInt(Vector128.Create(0, -1, 0, 0)));
    }

    [Fact]
    public static void IndexOf_NoMatch_Generic()
    {
        Assert.Equal(-1, GenericInt(Vector128.Create(0, 0, 0, 0)));
    }

    [Fact]
    public static void IndexOf_FirstElem_Generic()
    {
        Assert.Equal(0, GenericInt(Vector128.Create(-1, 0, 0, 0)));
    }

    [Fact]
    public static void IndexOf_AllSet_Generic()
    {
        Assert.Equal(0, GenericInt(Vector128.Create(-1, -1, -1, -1)));
    }

    // ===================== IndexOfWhereAllBitsSet: optimized path =====================

    [Fact]
    public static void IndexOf_Byte_Optimized()
    {
        var v = Vector128.Create((byte)0, 0, 0, 42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.Equal(3, OptimizedByteEq(v, 42));
    }

    [Fact]
    public static void IndexOf_Int_Optimized()
    {
        Assert.Equal(2, OptimizedIntEq(Vector128.Create(0, 0, 99, 0), 99));
    }

    [Fact]
    public static void IndexOf_Short_Optimized()
    {
        Assert.Equal(5, OptimizedShortEq(Vector128.Create((short)0, 0, 0, 0, 0, 7, 0, 0), 7));
    }

    [Fact]
    public static void IndexOf_Long_Optimized()
    {
        Assert.Equal(1, OptimizedLongEq(Vector128.Create(0L, -1L), -1L));
    }

    [Fact]
    public static void IndexOf_NoMatch_Optimized()
    {
        Assert.Equal(-1, OptimizedIntEq(Vector128.Create(1, 2, 3, 4), 99));
    }

    [Fact]
    public static void IndexOf_FirstElem_Optimized()
    {
        Assert.Equal(0, OptimizedIntEq(Vector128.Create(99, 0, 0, 0), 99));
    }

    // ===================== LastIndexOfWhereAllBitsSet: generic path =====================

    [Fact]
    public static void LastIndexOf_Int_Generic()
    {
        Assert.Equal(3, GenericLastInt(Vector128.Create(-1, 0, 0, -1)));
    }

    [Fact]
    public static void LastIndexOf_NoMatch_Generic()
    {
        Assert.Equal(-1, GenericLastInt(Vector128.Create(0, 0, 0, 0)));
    }

    // ===================== LastIndexOfWhereAllBitsSet: optimized path =====================

    [Fact]
    public static void LastIndexOf_Int_Optimized()
    {
        Assert.Equal(2, OptimizedLastIntEq(Vector128.Create(99, 0, 99, 0), 99));
    }

    [Fact]
    public static void LastIndexOf_Byte_Optimized()
    {
        var v = Vector128.Create((byte)42, 0, 0, 42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 42);
        Assert.Equal(15, OptimizedLastByteEq(v, 42));
    }

    [Fact]
    public static void LastIndexOf_NoMatch_Optimized()
    {
        Assert.Equal(-1, OptimizedLastIntEq(Vector128.Create(1, 2, 3, 4), 99));
    }

    // ===================== Helpers =====================

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int GenericByte(Vector128<byte> v) => Vector128.IndexOfWhereAllBitsSet(v);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int GenericInt(Vector128<int> v) => Vector128.IndexOfWhereAllBitsSet(v);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int GenericLastInt(Vector128<int> v) => Vector128.LastIndexOfWhereAllBitsSet(v);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int OptimizedByteEq(Vector128<byte> v, byte needle)
        => Vector128.IndexOfWhereAllBitsSet(Vector128.Equals(v, Vector128.Create(needle)));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int OptimizedIntEq(Vector128<int> v, int needle)
        => Vector128.IndexOfWhereAllBitsSet(Vector128.Equals(v, Vector128.Create(needle)));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int OptimizedShortEq(Vector128<short> v, short needle)
        => Vector128.IndexOfWhereAllBitsSet(Vector128.Equals(v, Vector128.Create(needle)));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int OptimizedLongEq(Vector128<long> v, long needle)
        => Vector128.IndexOfWhereAllBitsSet(Vector128.Equals(v, Vector128.Create(needle)));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int OptimizedLastIntEq(Vector128<int> v, int needle)
        => Vector128.LastIndexOfWhereAllBitsSet(Vector128.Equals(v, Vector128.Create(needle)));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int OptimizedLastByteEq(Vector128<byte> v, byte needle)
        => Vector128.LastIndexOfWhereAllBitsSet(Vector128.Equals(v, Vector128.Create(needle)));
}
