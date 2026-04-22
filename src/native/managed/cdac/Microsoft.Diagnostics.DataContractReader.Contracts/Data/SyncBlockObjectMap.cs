// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SyncBlockObjectMap : IData<SyncBlockObjectMap>
{
    static SyncBlockObjectMap IData<SyncBlockObjectMap>.Create(Target target, TargetPointer address)
        => new SyncBlockObjectMap(target, address);

    public SyncBlockObjectMap(Target target, TargetPointer syncTableEntries)
    {
        Map = new Dictionary<TargetPointer, TargetPointer>();

        TargetPointer syncBlockCache = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        SyncBlockCache cache = target.ProcessedData.GetOrAdd<SyncBlockCache>(syncBlockCache);
        if (cache.FreeSyncTableIndex <= 1)
            return;

        uint maxSyncTableIndex = cache.FreeSyncTableIndex - 1;
        uint syncTableEntrySize = target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value;

        for (uint index = 1; index <= maxSyncTableIndex; index++)
        {
            SyncTableEntry ste = target.ProcessedData.GetOrAdd<SyncTableEntry>(syncTableEntries + index * syncTableEntrySize);
            TargetPointer syncBlock = ste.SyncBlock?.Address ?? TargetPointer.Null;
            TargetPointer obj = ste.Object?.Address ?? TargetPointer.Null;
            if (syncBlock != TargetPointer.Null && obj != TargetPointer.Null)
            {
                Map[syncBlock] = obj;
            }
        }
    }

    private Dictionary<TargetPointer, TargetPointer> Map { get; }

    public bool TryGetObject(TargetPointer syncBlock, out TargetPointer obj)
        => Map.TryGetValue(syncBlock, out obj);
}
