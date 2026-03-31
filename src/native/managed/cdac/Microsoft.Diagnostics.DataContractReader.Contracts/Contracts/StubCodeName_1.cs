// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Version 1 implementation of the <see cref="IStubCodeName"/> contract.
/// </summary>
/// <remarks>
/// Handles the range-section-based precode stub path. Code-heap stubs (for example VSD
/// dispatch or StubLink stubs) that live inside JIT code heaps are not yet identified by
/// this version; callers that need a name for those should treat the result as an unnamed
/// CLR stub.
/// </remarks>
internal sealed class StubCodeName_1 : IStubCodeName
{
    private const int RangeSectionFlagRangeList = 0x04;

    private readonly Target _target;
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly RangeSectionMap _rangeSectionMapLookup;

    /// <summary>
    /// The stub manager name for precode range-list sections, read from the data descriptor
    /// (native <c>PrecodeStubManager::GetStubManagerName</c> returns this value).
    /// </summary>
    private readonly string _precodeStubManagerName;

    internal StubCodeName_1(Target target, Data.RangeSectionMap topRangeSectionMap, string precodeStubManagerName)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _rangeSectionMapLookup = RangeSectionMap.Create(target);
        _precodeStubManagerName = precodeStubManagerName;
    }

    bool IStubCodeName.TryGetStubTypeAndName(
        TargetCodePointer codeAddress,
        out StubManagerKind kind,
        out string? managerName)
    {
        kind = default;
        managerName = null;

        // Walk the range-section map to find the range section that owns this address.
        TargetPointer fragmentPtr = _rangeSectionMapLookup.FindFragment(_target, _topRangeSectionMap, codeAddress);
        Data.RangeSectionFragment? matchedFragment = null;
        while (fragmentPtr != TargetPointer.Null)
        {
            Data.RangeSectionFragment frag = _target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(fragmentPtr);
            if (frag.Contains(codeAddress))
            {
                matchedFragment = frag;
                break;
            }
            fragmentPtr = frag.Next;
        }

        if (matchedFragment is null)
            return false; // Address is not in any known range section.

        Data.RangeSection rangeSection = _target.ProcessedData.GetOrAdd<Data.RangeSection>(matchedFragment.RangeSection);

        // Skip range sections that are in the process of being deleted.
        if (rangeSection.NextForDelete != TargetPointer.Null)
            return false;

        bool isRangeList = (rangeSection.Flags & RangeSectionFlagRangeList) != 0;
        if (!isRangeList)
        {
            // This is a JIT code heap. GetCodeBlockHandle already handles managed methods;
            // we do not identify code-heap stubs (e.g. VSD dispatch/StubLink) here.
            return false;
        }

        // Range-list sections are owned by PrecodeStubManager. Return its manager name
        // as exposed by the data descriptor.
        kind = StubManagerKind.Precode;
        managerName = _precodeStubManagerName;
        return true;
    }
}
