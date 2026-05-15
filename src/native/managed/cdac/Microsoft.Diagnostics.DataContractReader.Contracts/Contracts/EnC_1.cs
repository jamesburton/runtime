// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct EnC_1 : IEnC
{
    // Default EnC function version (matches CorDB_DEFAULT_ENC_FUNCTION_VERSION in src/coreclr/inc/cordbpriv.h).
    // Methods that have never been EnC-edited report this version number.
    internal const ulong CorDB_DEFAULT_ENC_FUNCTION_VERSION = 1;

    private readonly Target _target;

    public EnC_1(Target target)
    {
        _target = target;
    }

    TargetNUInt IEnC.GetLatestEnCVersion(TargetPointer module, uint methodDef)
    {
        if (module == TargetPointer.Null)
        {
            throw new ArgumentException("Module pointer must not be null.", nameof(module));
        }

        Data.EnCData? entry = FindFirstByToken(module, methodDef);
        return entry is null
            ? new TargetNUInt(CorDB_DEFAULT_ENC_FUNCTION_VERSION)
            : entry.EnCVersion;
    }

    TargetNUInt IEnC.GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress)
    {
        if (module == TargetPointer.Null)
        {
            throw new ArgumentException("Module pointer must not be null.", nameof(module));
        }

        if (nativeCodeAddress.Value == 0)
        {
            return new TargetNUInt(CorDB_DEFAULT_ENC_FUNCTION_VERSION);
        }

        // EnCData entries store native code start addresses as TADDR (already stripped of any thumb bit).
        TargetPointer addr = new TargetPointer(nativeCodeAddress.Value);

        Data.Module moduleData = _target.ProcessedData.GetOrAdd<Data.Module>(module);
        TargetPointer cur = moduleData.EnCDataList;
        while (cur != TargetPointer.Null)
        {
            Data.EnCData entry = _target.ProcessedData.GetOrAdd<Data.EnCData>(cur);
            if (entry.Token == methodDef && entry.AddrOfCode == addr)
            {
                return entry.EnCVersion;
            }
            cur = entry.Next;
        }

        return new TargetNUInt(CorDB_DEFAULT_ENC_FUNCTION_VERSION);
    }

    // Find the first EnCData entry on the module's list whose token matches.
    // The list is maintained in reverse-insertion order (newest first; see
    // Module::AddEncData in src/coreclr/vm/ceeload.h), so the first match is
    // also the latest version for that method.
    private Data.EnCData? FindFirstByToken(TargetPointer module, uint methodDef)
    {
        Data.Module moduleData = _target.ProcessedData.GetOrAdd<Data.Module>(module);
        TargetPointer cur = moduleData.EnCDataList;
        while (cur != TargetPointer.Null)
        {
            Data.EnCData entry = _target.ProcessedData.GetOrAdd<Data.EnCData>(cur);
            if (entry.Token == methodDef)
            {
                Debug.Assert(entry.EnCVersion.Value >= CorDB_DEFAULT_ENC_FUNCTION_VERSION,
                    $"EnCData at 0x{cur.Value:X} has invalid EnC version {entry.EnCVersion.Value}.");
                return entry;
            }
            cur = entry.Next;
        }

        return null;
    }
}
