// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct SyncBlock_1 : ISyncBlock
{
    private const string MonitorName = "Monitor";
    private const string MonitorConditionTableFieldName = "s_conditionTable";
    private const string ConditionName = "Condition";
    private const string ConditionWaitersHeadFieldName = "_waitersHead";
    private const string ConditionWaiterName = "Condition+Waiter";
    private const string ConditionWaiterNextFieldName = "next";
    private const uint MaxAdditionalThreadCount = 1000;
    private const string LockStateName = "_state";
    private const string LockOwningThreadIdName = "_owningThreadId";
    private const string LockRecursionCountName = "_recursionCount";
    private const string LockName = "Lock";
    private const string LockNamespace = "System.Threading";
    private readonly Target _target;
    private readonly TargetPointer _syncTableEntries;

    internal SyncBlock_1(Target target)
    {
        _target = target;
        _syncTableEntries = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.SyncTableEntries));
    }

    public TargetPointer GetSyncBlock(uint index)
    {
        Data.SyncTableEntry ste = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + index * _target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value);
        return ste.SyncBlock?.Address ?? TargetPointer.Null;
    }

    public TargetPointer GetSyncBlockObject(uint index)
    {
        Data.SyncTableEntry ste = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + index * _target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value);
        return ste.Object?.Address ?? TargetPointer.Null;
    }

    public bool IsSyncBlockFree(uint index)
    {
        Data.SyncTableEntry ste = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + index * _target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value);
        return (ste.Object?.Address & 1) != 0;
    }

    public uint GetSyncBlockCount()
    {
        TargetPointer syncBlockCache = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        Data.SyncBlockCache cache = _target.ProcessedData.GetOrAdd<Data.SyncBlockCache>(syncBlockCache);
        return cache.FreeSyncTableIndex - 1;
    }

    public bool TryGetLockInfo(TargetPointer syncBlock, out uint owningThreadId, out uint recursion)
    {
        owningThreadId = 0;
        recursion = 0;
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);

        if (sb.Lock != null)
        {
            ILoader loader = _target.Contracts.Loader;
            TargetPointer systemAssembly = loader.GetSystemAssembly();
            ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
            TypeHandle lockType = rts.GetTypeByNameAndModule(LockName, LockNamespace, moduleHandle);
            MetadataReader mdReader = ecmaMetadataContract.GetMetadata(moduleHandle)!;
            TargetPointer lockObjPtr = sb.Lock.Object;
            Data.Object lockObj = _target.ProcessedData.GetOrAdd<Data.Object>(lockObjPtr);
            TargetPointer dataAddr = lockObj.Data;
            uint state = ReadUintField(lockType, LockStateName, rts, mdReader, dataAddr);
            bool monitorHeld = (state & 1) != 0;
            if (monitorHeld)
            {
                owningThreadId = ReadUintField(lockType, LockOwningThreadIdName, rts, mdReader, dataAddr);
                recursion = ReadUintField(lockType, LockRecursionCountName, rts, mdReader, dataAddr);
            }
            return monitorHeld;
        }

        else if (sb.ThinLock != 0)
        {
            owningThreadId = sb.ThinLock & _target.ReadGlobal<uint>(Constants.Globals.SyncBlockMaskLockThreadId);
            bool monitorHeld = owningThreadId != 0;
            if (monitorHeld)
            {
                recursion = (sb.ThinLock & _target.ReadGlobal<uint>(Constants.Globals.SyncBlockMaskLockRecursionLevel)) >> (int)_target.ReadGlobal<uint>(Constants.Globals.SyncBlockRecursionLevelShift);
            }
            return monitorHeld;
        }

        else
        {
            return false;
        }
    }

    public uint GetAdditionalThreadCount(TargetPointer syncBlock)
    {
        TargetPointer obj = GetSyncBlockAssociatedObject(syncBlock);
        if (obj == TargetPointer.Null)
            return 0;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        rts.GetCoreLibFieldDescAndDef(LockNamespace, MonitorName, MonitorConditionTableFieldName, out TargetPointer conditionTableFieldDescAddr, out _);
        TargetPointer conditionTable = _target.ReadPointer(rts.GetFieldDescStaticAddress(conditionTableFieldDescAddr));
        if (conditionTable == TargetPointer.Null)
            return 0;

        IConditionalWeakTable cwt = _target.Contracts.ConditionalWeakTable;
        if (!cwt.TryGetValue(conditionTable, obj, out TargetPointer condition))
            return 0;

        rts.GetCoreLibFieldDescAndDef(LockNamespace, ConditionName, ConditionWaitersHeadFieldName, out TargetPointer waitersHeadFieldDescAddr, out FieldDefinition waitersHeadFieldDef);
        uint waitersHeadOffset = rts.GetFieldDescOffset(waitersHeadFieldDescAddr, waitersHeadFieldDef);

        rts.GetCoreLibFieldDescAndDef(LockNamespace, ConditionWaiterName, ConditionWaiterNextFieldName, out TargetPointer waiterNextFieldDescAddr, out FieldDefinition waiterNextFieldDef);
        uint waiterNextOffset = rts.GetFieldDescOffset(waiterNextFieldDescAddr, waiterNextFieldDef);

        Data.Object conditionObj = _target.ProcessedData.GetOrAdd<Data.Object>(condition);
        TargetPointer waiter = _target.ReadPointer(conditionObj.Data + waitersHeadOffset);
        uint additionalThreadCount = 0;
        // The result is capped to guard against corrupted waiter lists.
        while (waiter != TargetPointer.Null && additionalThreadCount < MaxAdditionalThreadCount)
        {
            additionalThreadCount++;
            Data.Object waiterObj = _target.ProcessedData.GetOrAdd<Data.Object>(waiter);
            waiter = _target.ReadPointer(waiterObj.Data + waiterNextOffset);
        }

        return additionalThreadCount;
    }

    public TargetPointer GetSyncBlockFromCleanupList()
    {
        TargetPointer syncBlockCache = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        Data.SyncBlockCache cache = _target.ProcessedData.GetOrAdd<Data.SyncBlockCache>(syncBlockCache);
        TargetPointer cleanupBlockList = cache.CleanupBlockList;
        if (cleanupBlockList == TargetPointer.Null)
            return TargetPointer.Null;
        return cleanupBlockList;
    }

    public TargetPointer GetNextSyncBlock(TargetPointer syncBlock)
    {
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        if (sb.LinkNext == TargetPointer.Null)
            return TargetPointer.Null;
        return sb.LinkNext;
    }

    public bool GetBuiltInComData(TargetPointer syncBlock, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf)
    {
        rcw = TargetPointer.Null;
        ccw = TargetPointer.Null;
        ccf = TargetPointer.Null;

        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        Data.InteropSyncBlockInfo? interopInfo = sb.InteropInfo;
        if (interopInfo == null)
            return false;

        rcw = interopInfo.RCW & ~1ul;
        ccw = interopInfo.CCW == 1 ? TargetPointer.Null : interopInfo.CCW;
        ccf = interopInfo.CCF == 1 ? TargetPointer.Null : interopInfo.CCF;
        return rcw != TargetPointer.Null || ccw != TargetPointer.Null || ccf != TargetPointer.Null;
    }

    private uint ReadUintField(TypeHandle enclosingType, string fieldName, IRuntimeTypeSystem rts, MetadataReader mdReader, TargetPointer dataAddr)
    {
        TargetPointer field = rts.GetFieldDescByName(enclosingType, fieldName);
        uint token = rts.GetFieldDescMemberDef(field);
        FieldDefinitionHandle fieldHandle = (FieldDefinitionHandle)MetadataTokens.Handle((int)token);
        FieldDefinition fieldDef = mdReader.GetFieldDefinition(fieldHandle);
        uint offset = rts.GetFieldDescOffset(field, fieldDef);
        return _target.Read<uint>(dataAddr + offset);
    }

    private TargetPointer GetSyncBlockAssociatedObject(TargetPointer syncBlock)
    {
        Data.SyncBlockObjectMap map = _target.ProcessedData.GetOrAdd<Data.SyncBlockObjectMap>(_syncTableEntries);
        if (map.TryGetObject(syncBlock, out TargetPointer obj))
            return obj;
        return TargetPointer.Null;
    }
}
