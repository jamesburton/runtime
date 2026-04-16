// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

class Program
{
    static int Main()
    {
        int passed = 0;
        int failed = 0;

        // Test 1: Basic stack trace resolution works
        string trace = Environment.StackTrace;
        Check("Basic StackTrace", trace.Contains("Program.Main"), ref passed, ref failed);

        // Test 2: StackTraceHidden is respected (exercises IsHidden flag on bit 1)
        try { ThrowFromHidden(); }
        catch (Exception ex)
        {
            Check("StackTraceHidden filtered", !ex.StackTrace!.Contains("HiddenMethod"), ref passed, ref failed);
            Check("StackTraceHidden caller visible", ex.StackTrace.Contains("ThrowFromHidden"), ref passed, ref failed);
        }

        // Test 3: Multiple methods resolve correctly (exercises BinarySearch with aligned RVAs)
        trace = CaptureNestedTrace();
        Check("Nested - Method1 visible", trace.Contains("Method1"), ref passed, ref failed);
        Check("Nested - Method2 visible", trace.Contains("Method2"), ref passed, ref failed);
        Check("Nested - Method3 visible", trace.Contains("Method3"), ref passed, ref failed);

        // Test 4: Exception stack trace works
        try { Method1Throw(); }
        catch (Exception ex)
        {
            Check("Exception - Method1Throw", ex.StackTrace!.Contains("Method1Throw"), ref passed, ref failed);
            Check("Exception - Method2Throw", ex.StackTrace.Contains("Method2Throw"), ref passed, ref failed);
        }

        Console.WriteLine($"\nResults: {passed} passed, {failed} failed");
        return failed == 0 ? 100 : 101;
    }

    static void Check(string name, bool condition, ref int passed, ref int failed)
    {
        if (condition) { passed++; Console.WriteLine($"  PASS: {name}"); }
        else { failed++; Console.WriteLine($"  FAIL: {name}"); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowFromHidden() => HiddenMethod();

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void HiddenMethod() => throw new InvalidOperationException("hidden");

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string CaptureNestedTrace() => Method1();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string Method1() => Method2();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string Method2() => Method3();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string Method3() => Environment.StackTrace;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Method1Throw() => Method2Throw();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Method2Throw() => throw new Exception("test");
}
