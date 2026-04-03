// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Version 1 implementation of the <see cref="IStubCodeName"/> contract.
/// </summary>
/// <remarks>
/// Mirrors the order of checks performed by the native <c>StubManager::FindStubManager</c>:
/// ThePreStubManager, PrecodeStubManager, StubLinkStubManager (via range-list section kind),
/// RangeSectionStubManager (VSD/JumpStub/MethodCallThunk), InteropDispatchStubManager, and
/// TailCallStubManager (x86 only).  CallCountingStubManager is intentionally left NYI.
/// </remarks>
internal sealed class StubCodeName_1 : IStubCodeName
{
    // RangeSection flag bits (RangeSection::RangeSectionFlags).
    private const int RangeSectionFlagRangeList = 0x04;

    // StubCodeBlockKind values mirrored from vm/codeman.h.
    private const int StubCodeBlock_JumpStub = 1;
    private const int StubCodeBlock_StubPrecode = 4;
    private const int StubCodeBlock_FixupPrecode = 5;
    private const int StubCodeBlock_VsdDispatchStub = 6;
    private const int StubCodeBlock_VsdResolveStub = 7;
    private const int StubCodeBlock_VsdLookupStub = 8;
    private const int StubCodeBlock_VsdVTableStub = 9;
    private const int StubCodeBlock_StubLink = 0x12;

    // Stub manager name strings (contract constants matching native GetStubManagerName() returns).
    private const string NameThePreStub = "ThePreStub";
    private const string NameMethodDescPrestub = "MethodDescPrestub";
    private const string NameStubLinkStub = "StubLinkStub";
    private const string NameJumpStub = "JumpStub";
    private const string NameVsdDispatchStub = "VSD_DispatchStub";
    private const string NameVsdResolveStub = "VSD_ResolveStub";
    private const string NameVsdLookupStub = "VSD_LookupStub";
    private const string NameVsdVTableStub = "VSD_VTableStub";
    private const string NameMethodCallThunk = "MethodCallThunk";
    private const string NameInteropDispatchStub = "InteropDispatchStub";
    private const string NameTailCallStub = "TailCallStub";

    private readonly Target _target;
    private readonly Data.RangeSectionMap _topRangeSectionMap;
    private readonly RangeSectionMap _rangeSectionMapLookup;

    // Optional entry-point addresses for managers that match specific PCODEs.
    // Null when the global is not present in the data descriptor for this platform.
    private readonly TargetPointer _thePreStubEntryPoint;
    private readonly TargetPointer _genericPInvokeCalliHelper;
    private readonly TargetPointer _varargPInvokeStub;
    private readonly TargetPointer _varargPInvokeStub_RetBuffArg;
    private readonly TargetPointer _genericCLRToCOMCallStub;
    private readonly TargetPointer _tailCallJitHelper;

    internal StubCodeName_1(
        Target target,
        Data.RangeSectionMap topRangeSectionMap,
        TargetPointer thePreStubEntryPoint,
        TargetPointer genericPInvokeCalliHelper,
        TargetPointer varargPInvokeStub,
        TargetPointer varargPInvokeStub_RetBuffArg,
        TargetPointer genericCLRToCOMCallStub,
        TargetPointer tailCallJitHelper)
    {
        _target = target;
        _topRangeSectionMap = topRangeSectionMap;
        _rangeSectionMapLookup = RangeSectionMap.Create(target);
        _thePreStubEntryPoint = thePreStubEntryPoint;
        _genericPInvokeCalliHelper = genericPInvokeCalliHelper;
        _varargPInvokeStub = varargPInvokeStub;
        _varargPInvokeStub_RetBuffArg = varargPInvokeStub_RetBuffArg;
        _genericCLRToCOMCallStub = genericCLRToCOMCallStub;
        _tailCallJitHelper = tailCallJitHelper;
    }

    bool IStubCodeName.TryGetStubTypeAndName(
        TargetCodePointer codeAddress,
        out StubManagerKind kind,
        out string? managerName)
    {
        // ---- ThePreStubManager ----
        // Check first: single exact address comparison (GetPreStubEntryPoint).
        if (TryAsThePreStub(codeAddress, out kind, out managerName))
            return true;

        // ---- PrecodeStubManager / StubLinkStubManager / RangeSectionStubManager ----
        // These are all detected via the range-section map.
        if (TryAsRangeSectionStub(codeAddress, out kind, out managerName))
            return true;

        // ---- InteropDispatchStubManager ----
        if (TryAsInteropDispatchStub(codeAddress, out kind, out managerName))
            return true;

        // ---- TailCallStubManager (x86 only) ----
        if (TryAsTailCallStub(codeAddress, out kind, out managerName))
            return true;

        kind = default;
        managerName = null;
        return false;
    }

    /// <summary>Mirrors <c>ThePreStubManager::CheckIsStub_Internal</c>: exact address match.</summary>
    private bool TryAsThePreStub(TargetCodePointer codeAddress, out StubManagerKind kind, out string? managerName)
    {
        kind = default;
        managerName = null;
        if (_thePreStubEntryPoint == TargetPointer.Null)
            return false;
        if (codeAddress.Value != _thePreStubEntryPoint.Value)
            return false;
        kind = StubManagerKind.ThePreStub;
        managerName = NameThePreStub;
        return true;
    }

    /// <summary>
    /// Mirrors the range-section checks performed by <c>PrecodeStubManager</c>,
    /// <c>StubLinkStubManager</c>, and <c>RangeSectionStubManager</c>:
    /// look up the range section and read its <c>CodeRangeMapRangeList::_rangeListType</c>
    /// to determine the exact stub kind.
    /// </summary>
    private bool TryAsRangeSectionStub(TargetCodePointer codeAddress, out StubManagerKind kind, out string? managerName)
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
            return false;

        Data.RangeSection rangeSection = _target.ProcessedData.GetOrAdd<Data.RangeSection>(matchedFragment.RangeSection);

        // Skip range sections that are in the process of being deleted.
        if (rangeSection.NextForDelete != TargetPointer.Null)
            return false;

        bool isRangeList = (rangeSection.Flags & RangeSectionFlagRangeList) != 0;
        if (!isRangeList)
        {
            // JIT code-heap section: try to read the stub code block kind from the code header.
            // The CodeHeader immediately precedes the code (at codeAddress - sizeof(pointer)).
            // pRealCodeHeader for stub blocks is the StubCodeBlockKind value itself (<= STUB_CODE_BLOCK_LAST=0xF).
            return TryAsCodeHeapStub(codeAddress, out kind, out managerName);
        }

        // Range-list section: the RangeList object itself records the kind for all code within.
        if (rangeSection.RangeList == TargetPointer.Null)
            return false;

        Data.CodeRangeMapRangeList rangeList = _target.ProcessedData.GetOrAdd<Data.CodeRangeMapRangeList>(rangeSection.RangeList);
        return MapRangeListKindToStubKind(rangeList.RangeListKind, out kind, out managerName);
    }

    /// <summary>
    /// For code-heap (non-range-list) sections: read the "code header" slot at
    /// <c>codeAddress - sizeof(pointer)</c>.  If the value is a valid
    /// <c>StubCodeBlockKind</c> (≤ STUB_CODE_BLOCK_LAST = 0xF) the slot stores the
    /// kind directly instead of a real <c>RealCodeHeader</c> pointer.
    /// Mirrors the native <c>EEJitManager::GetStubCodeBlockKind</c> code-heap path.
    /// </summary>
    private bool TryAsCodeHeapStub(TargetCodePointer codeAddress, out StubManagerKind kind, out string? managerName)
    {
        kind = default;
        managerName = null;

        // Guard against addresses too small to have a header prefix.
        ulong headerSlotAddress = codeAddress.Value - (ulong)_target.PointerSize;
        if (headerSlotAddress == 0 || headerSlotAddress > codeAddress.Value)
            return false;

        TargetPointer headerValue;
        try
        {
            headerValue = _target.ReadPointer(new TargetPointer(headerSlotAddress));
        }
        catch
        {
            return false;
        }

        // If the "pointer" value is <= STUB_CODE_BLOCK_LAST it is a StubCodeBlockKind tag.
        byte stubCodeBlockLast = _target.ReadGlobal<byte>(Constants.Globals.StubCodeBlockLast);
        if (headerValue.Value > stubCodeBlockLast)
            return false;

        return MapRangeListKindToStubKind((int)headerValue.Value, out kind, out managerName);
    }

    /// <summary>Maps a <c>StubCodeBlockKind</c> integer to the corresponding <see cref="StubManagerKind"/>.</summary>
    private static bool MapRangeListKindToStubKind(int rangeListKind, out StubManagerKind kind, out string? managerName)
    {
        kind = default;
        managerName = null;

        switch (rangeListKind)
        {
            case StubCodeBlock_StubPrecode:
            case StubCodeBlock_FixupPrecode:
                kind = StubManagerKind.Precode;
                managerName = NameMethodDescPrestub;
                return true;

            case StubCodeBlock_StubLink:
                kind = StubManagerKind.StubLink;
                managerName = NameStubLinkStub;
                return true;

            case StubCodeBlock_JumpStub:
                kind = StubManagerKind.JumpStub;
                managerName = NameJumpStub;
                return true;

            case StubCodeBlock_VsdDispatchStub:
                kind = StubManagerKind.RangeSection;
                managerName = NameVsdDispatchStub;
                return true;

            case StubCodeBlock_VsdResolveStub:
                kind = StubManagerKind.RangeSection;
                managerName = NameVsdResolveStub;
                return true;

            case StubCodeBlock_VsdLookupStub:
                kind = StubManagerKind.RangeSection;
                managerName = NameVsdLookupStub;
                return true;

            case StubCodeBlock_VsdVTableStub:
                kind = StubManagerKind.RangeSection;
                managerName = NameVsdVTableStub;
                return true;

            // STUB_CODE_BLOCK_METHOD_CALL_THUNK = 0x13 (ReadyToRun)
            case 0x13:
                kind = StubManagerKind.RangeSection;
                managerName = NameMethodCallThunk;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Mirrors <c>InteropDispatchStubManager::CheckIsStub_Internal</c>: exact address
    /// comparisons against GenericCLRToCOMCallStub, VarargPInvokeStub[_RetBuffArg], and
    /// GenericPInvokeCalliHelper.
    /// </summary>
    private bool TryAsInteropDispatchStub(TargetCodePointer codeAddress, out StubManagerKind kind, out string? managerName)
    {
        kind = default;
        managerName = null;

        bool isInterop =
            (_genericCLRToCOMCallStub != TargetPointer.Null && codeAddress.Value == _genericCLRToCOMCallStub.Value) ||
            (_varargPInvokeStub != TargetPointer.Null && codeAddress.Value == _varargPInvokeStub.Value) ||
            (_varargPInvokeStub_RetBuffArg != TargetPointer.Null && codeAddress.Value == _varargPInvokeStub_RetBuffArg.Value) ||
            (_genericPInvokeCalliHelper != TargetPointer.Null && codeAddress.Value == _genericPInvokeCalliHelper.Value);

        if (!isInterop)
            return false;

        kind = StubManagerKind.InteropDispatch;
        managerName = NameInteropDispatchStub;
        return true;
    }

    /// <summary>
    /// Mirrors <c>TailCallStubManager::CheckIsStub_Internal</c> (x86 only): exact address
    /// match against JIT_TailCall.
    /// </summary>
    private bool TryAsTailCallStub(TargetCodePointer codeAddress, out StubManagerKind kind, out string? managerName)
    {
        kind = default;
        managerName = null;
        if (_tailCallJitHelper == TargetPointer.Null)
            return false;
        if (codeAddress.Value != _tailCallJitHelper.Value)
            return false;
        kind = StubManagerKind.TailCall;
        managerName = NameTailCallStub;
        return true;
    }
}
