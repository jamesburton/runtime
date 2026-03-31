// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    internal StubCodeName_1(Target target, Data.RangeSectionMap topRangeSectionMap)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _rangeSectionMapLookup = RangeSectionMap.Create(target);
    }

    bool IStubCodeName.TryGetStubTypeAndName(
        TargetCodePointer codeAddress,
        out TargetPointer methodDescAddress,
        out string? stubName)
    {
        methodDescAddress = TargetPointer.Null;
        stubName = null;

        // Walk the range-section map to find the range section that owns this address.
        TargetPointer fragmentPtr = _rangeSectionMapLookup.FindFragment(_target, _topRangeSectionMap, codeAddress);
        while (fragmentPtr != TargetPointer.Null)
        {
            Data.RangeSectionFragment frag = _target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(fragmentPtr);
            if (frag.Contains(codeAddress))
                break;
            fragmentPtr = frag.Next;
        }

        if (fragmentPtr == TargetPointer.Null)
            return false; // Address is not in any known range section.

        Data.RangeSectionFragment fragment = _target.ProcessedData.GetOrAdd<Data.RangeSectionFragment>(fragmentPtr);
        Data.RangeSection rangeSection = _target.ProcessedData.GetOrAdd<Data.RangeSection>(fragment.RangeSection);

        // Skip range sections that are in the process of being deleted.
        if (rangeSection.NextForDelete != TargetPointer.Null)
            return false;

        bool isRangeList = (rangeSection.Flags & RangeSectionFlagRangeList) != 0;
        if (!isRangeList)
        {
            // This is a JIT code heap.  GetCodeBlockHandle already handles managed methods
            // and returns null for stub code blocks.  We do not yet identify named code-heap
            // stubs (e.g. VSD dispatch stubs or StubLink stubs) in this version.
            return false;
        }

        // The address is in a precode / range-list section.  Try to resolve it to a MethodDesc.
        IPrecodeStubs precodeStubs = _target.Contracts.PrecodeStubs;
        try
        {
            methodDescAddress = precodeStubs.GetMethodDescFromStubAddress(codeAddress);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The bytes at codeAddress do not match any known precode layout.  This can happen
            // for range-list stubs that are not precodes (e.g. DynamicHelper stubs).
            // We still report this as "a CLR stub" so the caller can format it as
            // "CLRStub@address".  methodDescAddress remains Null and stubName remains null.
            return true;
        }
    }
}
