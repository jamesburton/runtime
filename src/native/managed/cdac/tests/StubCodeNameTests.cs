// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class StubCodeNameTests
{
    // Arbitrary precode address range for tests.
    private const ulong PrecodeRangeStart = 0x0b0b_0000u;
    private const uint PrecodeRangeSize = 0x1_0000u;

    private static Target CreateTarget(
        MockDescriptors.ExecutionManager emBuilder,
        Mock<IPrecodeStubs> precodeStubsMock)
    {
        var arch = emBuilder.Builder.TargetTestHelpers.Arch;
        TestPlaceholderTarget.ReadFromTargetDelegate reader = emBuilder.Builder.GetMemoryContext().ReadFromTarget;
        var target = new TestPlaceholderTarget(arch, reader, emBuilder.Types, emBuilder.Globals);

        IContractFactory<IStubCodeName> stubCodeNameFactory = new StubCodeNameFactory();
        Mock<ContractRegistry> reg = new();
        reg.SetupGet(c => c.PrecodeStubs).Returns(precodeStubsMock.Object);
        reg.SetupGet(c => c.StubCodeName).Returns(() => stubCodeNameFactory.CreateContract(target, 1));
        target.SetContracts(reg.Object);
        return target;
    }

    public static IEnumerable<object[]> AllVersions()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return [1, arch];
        }
    }

    [Theory]
    [MemberData(nameof(AllVersions))]
    public void TryGetStubTypeAndName_AddressNotInRangeSection_ReturnsFalse(int version, MockTarget.Architecture arch)
    {
        MockDescriptors.ExecutionManager emBuilder = new(version, arch, MockDescriptors.ExecutionManager.DefaultAllocationRange);
        Mock<IPrecodeStubs> precodeStubsMock = new();

        var target = CreateTarget(emBuilder, precodeStubsMock);
        var stubCodeName = target.Contracts.StubCodeName;

        // An address not in any range section should return false.
        bool found = stubCodeName.TryGetStubTypeAndName(
            new TargetCodePointer(0xDEAD_BEF0), out TargetPointer methodDescAddress, out string? stubName);

        Assert.False(found);
        Assert.Equal(TargetPointer.Null, methodDescAddress);
        Assert.Null(stubName);
    }

    [Theory]
    [MemberData(nameof(AllVersions))]
    public void TryGetStubTypeAndName_PrecodeRangeSection_ReturnsMethodDesc(int version, MockTarget.Architecture arch)
    {
        MockDescriptors.ExecutionManager emBuilder = new(version, arch, MockDescriptors.ExecutionManager.DefaultAllocationRange);

        // Set up a RangeList (precode) range section.
        TargetPointer jitManagerAddress = new(0x000b_ff00);
        MockDescriptors.ExecutionManager.JittedCodeRange precodeRange = emBuilder.AllocateJittedCodeRange(PrecodeRangeStart, PrecodeRangeSize);
        TargetPointer rangeSectionAddress = emBuilder.AddRangeListRangeSection(precodeRange, jitManagerAddress);
        _ = emBuilder.AddRangeSectionFragment(precodeRange, rangeSectionAddress);

        TargetPointer expectedMethodDesc = new TargetPointer(0xeeee_eee0);
        TargetCodePointer stubAddress = new TargetCodePointer(PrecodeRangeStart + 0x100);

        Mock<IPrecodeStubs> precodeStubsMock = new();
        precodeStubsMock
            .Setup(p => p.GetMethodDescFromStubAddress(stubAddress))
            .Returns(expectedMethodDesc);

        var target = CreateTarget(emBuilder, precodeStubsMock);
        var stubCodeName = target.Contracts.StubCodeName;

        bool found = stubCodeName.TryGetStubTypeAndName(
            stubAddress, out TargetPointer methodDescAddress, out string? stubName);

        Assert.True(found);
        Assert.Equal(expectedMethodDesc, methodDescAddress);
        Assert.Null(stubName);
    }

    [Theory]
    [MemberData(nameof(AllVersions))]
    public void TryGetStubTypeAndName_PrecodeRangeSection_InvalidPrecode_ReturnsTrueWithNullMethodDesc(int version, MockTarget.Architecture arch)
    {
        MockDescriptors.ExecutionManager emBuilder = new(version, arch, MockDescriptors.ExecutionManager.DefaultAllocationRange);

        // Set up a RangeList (precode) range section.
        TargetPointer jitManagerAddress = new(0x000b_ff00);
        MockDescriptors.ExecutionManager.JittedCodeRange precodeRange = emBuilder.AllocateJittedCodeRange(PrecodeRangeStart, PrecodeRangeSize);
        TargetPointer rangeSectionAddress = emBuilder.AddRangeListRangeSection(precodeRange, jitManagerAddress);
        _ = emBuilder.AddRangeSectionFragment(precodeRange, rangeSectionAddress);

        TargetCodePointer stubAddress = new TargetCodePointer(PrecodeRangeStart + 0x200);

        Mock<IPrecodeStubs> precodeStubsMock = new();
        // Simulate a non-precode range-list stub (e.g., DynamicHelper) by throwing.
        precodeStubsMock
            .Setup(p => p.GetMethodDescFromStubAddress(stubAddress))
            .Throws<InvalidOperationException>();

        var target = CreateTarget(emBuilder, precodeStubsMock);
        var stubCodeName = target.Contracts.StubCodeName;

        // Should return true (it IS a CLR stub) but with null method desc and null name
        // so the caller can format it as "CLRStub@address".
        bool found = stubCodeName.TryGetStubTypeAndName(
            stubAddress, out TargetPointer methodDescAddress, out string? stubName);

        Assert.True(found);
        Assert.Equal(TargetPointer.Null, methodDescAddress);
        Assert.Null(stubName);
    }
}
