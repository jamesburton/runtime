// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private const string TestPrecodeStubManagerName = "MethodDescPrestub";

    private static Target CreateTarget(MockDescriptors.ExecutionManager emBuilder)
    {
        var arch = emBuilder.Builder.TargetTestHelpers.Arch;
        TestPlaceholderTarget.ReadFromTargetDelegate reader = emBuilder.Builder.GetMemoryContext().ReadFromTarget;
        var target = new TestPlaceholderTarget(
            arch,
            reader,
            emBuilder.Types,
            emBuilder.Globals,
            [(Constants.Globals.PrecodeStubManagerName, TestPrecodeStubManagerName)]);

        IContractFactory<IStubCodeName> stubCodeNameFactory = new StubCodeNameFactory();
        Mock<ContractRegistry> reg = new();
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

        var target = CreateTarget(emBuilder);
        var stubCodeName = target.Contracts.StubCodeName;

        // An address not in any range section should return false.
        bool found = stubCodeName.TryGetStubTypeAndName(
            new TargetCodePointer(0xDEAD_BEF0), out StubManagerKind kind, out string? managerName);

        Assert.False(found);
        Assert.Null(managerName);
    }

    [Theory]
    [MemberData(nameof(AllVersions))]
    public void TryGetStubTypeAndName_PrecodeRangeSection_ReturnsPrecodeKindAndManagerName(int version, MockTarget.Architecture arch)
    {
        MockDescriptors.ExecutionManager emBuilder = new(version, arch, MockDescriptors.ExecutionManager.DefaultAllocationRange);

        // Set up a RangeList (precode) range section.
        TargetPointer jitManagerAddress = new(0x000b_ff00);
        MockDescriptors.ExecutionManager.JittedCodeRange precodeRange = emBuilder.AllocateJittedCodeRange(PrecodeRangeStart, PrecodeRangeSize);
        TargetPointer rangeSectionAddress = emBuilder.AddRangeListRangeSection(precodeRange, jitManagerAddress);
        _ = emBuilder.AddRangeSectionFragment(precodeRange, rangeSectionAddress);

        TargetCodePointer stubAddress = new TargetCodePointer(PrecodeRangeStart + 0x100);

        var target = CreateTarget(emBuilder);
        var stubCodeName = target.Contracts.StubCodeName;

        bool found = stubCodeName.TryGetStubTypeAndName(
            stubAddress, out StubManagerKind kind, out string? managerName);

        Assert.True(found);
        Assert.Equal(StubManagerKind.Precode, kind);
        Assert.Equal(TestPrecodeStubManagerName, managerName);
    }
}
