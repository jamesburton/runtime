// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Data;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

public unsafe class DacDbiStackWalkDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetContext_MatchesFilterOrTargetDelegateContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        TargetPointer threadPointer = FindThreadWithContext(dbi, out byte[] actualContext);

        byte[] expectedContext = GetExpectedContext(threadPointer);
        Assert.Equal(expectedContext, actualContext);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void IsLeafFrame_ReturnsTrueForLeafContextAndFalseForDifferentContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        TargetPointer threadPointer = FindThreadWithContext(dbi, out byte[] contextBytes);

        Interop.BOOL isLeaf;
        fixed (byte* contextPtr = contextBytes)
        {
            int hr = dbi.IsLeafFrame(threadPointer.Value, (nint)contextPtr, &isLeaf);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.TRUE, isLeaf);
        }

        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(Target);
        context.FillFromBuffer(contextBytes);
        context.InstructionPointer = new TargetPointer(context.InstructionPointer.Value + 1);
        byte[] nonLeafContextBytes = context.GetBytes();

        fixed (byte* contextPtr = nonLeafContextBytes)
        {
            int hr = dbi.IsLeafFrame(threadPointer.Value, (nint)contextPtr, &isLeaf);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.FALSE, isLeaf);
        }
    }

    private TargetPointer FindThreadWithContext(DacDbiImpl dbi, out byte[] contextBytes)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(Target);
        contextBytes = new byte[context.Size];

        ThreadStoreData storeData = Target.Contracts.Thread.GetThreadStoreData();
        TargetPointer current = storeData.FirstThread;
        while (current != TargetPointer.Null)
        {
            fixed (byte* contextPtr = contextBytes)
            {
                int hr = dbi.GetContext(current.Value, (nint)contextPtr);
                if (hr == System.HResults.S_OK)
                    return current;
            }

            current = Target.Contracts.Thread.GetThreadData(current).NextThread;
        }

        throw new Xunit.Sdk.XunitException("No thread with retrievable context was found in the dump.");
    }

    private byte[] GetExpectedContext(TargetPointer threadPointer)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(Target);
        byte[] expectedContext = new byte[context.Size];
        Span<byte> buffer = expectedContext;

        ThreadData threadData = Target.Contracts.Thread.GetThreadData(threadPointer);
        Thread thread = Target.ProcessedData.GetOrAdd<Thread>(threadData.ThreadAddress);

        TargetPointer filterContext = thread.DebuggerFilterContext;
        if (filterContext == TargetPointer.Null)
        {
            filterContext = thread.ProfilerFilterContext;
        }

        if (filterContext != TargetPointer.Null)
        {
            Target.ReadBuffer(filterContext.Value, buffer);
        }
        else
        {
            Assert.True(
                Target.TryGetThreadContext(threadData.OSId.Value, context.DefaultContextFlags, buffer),
                $"Expected GetThreadContext to succeed for thread {threadData.OSId.Value}");
        }

        return expectedContext;
    }
}
