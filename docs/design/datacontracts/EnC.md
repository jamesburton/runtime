# Contract EnC

This contract reports Edit and Continue (EnC) function version numbers for jitted
managed methods. EnC function versions are 1-based monotonically increasing counters
that the runtime assigns to each `EnC`-emitted instance of a method body, so that
diagnostic tools (most notably the debugger DBI / DI layer) can correlate a particular
jitted native code blob with the IL version it was produced from.

When `FEATURE_METADATA_UPDATER` is not present in the target runtime, this contract
is not registered. Callers that need to behave gracefully should query the contract
through `TryGetContract<IEnC>` first.

## APIs of contract

``` csharp
// Returns the latest EnC version number associated with the method identified by
// (module, methodDef). If no EnC-jitted instance exists for that method, returns
// the default EnC function version (1).
TargetNUInt GetLatestEnCVersion(TargetPointer module, uint methodDef);

// Returns the EnC version number for the specific jitted instance of the method
// identified by (module, methodDef) whose hot region starts at the given native
// code address. If no matching jitted instance exists (for example, the method
// was never EnC-edited), returns the default EnC function version (1).
TargetNUInt GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| Module | EnCDataList | head of the singly linked list of `EnCData` entries for jitted EnC-versioned methods in this module |
| EnCData | AddrOfCode | native code start (TADDR) for the jitted instance |
| EnCData | Token | `mdMethodDef` token of the method |
| EnCData | EnCVersion | EnC function version number for this jitted instance |
| EnCData | Next | next entry in the module's `EnCData` list, or null |

Global values used: _none_

Contracts used: _none_

### Algorithm

The runtime maintains, per `Module`, a singly linked list of `EnCData` nodes. A node
is appended (at the head) every time `DebuggerJitInfo::Init` initializes a jitted
instance whose EnC version is not the default version 1 (see
`src/coreclr/debug/ee/functioninfo.cpp` and `Module::AddEncData` in
`src/coreclr/vm/ceeload.h`). Because new entries are inserted at the head, the first
entry that matches a given token is also the latest EnC version recorded for that
token.

The default EnC function version (`CorDB_DEFAULT_ENC_FUNCTION_VERSION == 1`, from
`src/coreclr/inc/cordbpriv.h`) is returned when no matching entry is found. This
mirrors the previous in-proc behavior implemented in
`DacDbiInterfaceImpl::LookupEnCVersions`, which returned the default version when no
`DebuggerMethodInfo` / `DebuggerJitInfo` existed for the method.

``` csharp
const ulong CorDB_DEFAULT_ENC_FUNCTION_VERSION = 1;

TargetNUInt GetLatestEnCVersion(TargetPointer module, uint methodDef)
{
    if (module == TargetPointer.Null)
        throw new ArgumentException();

    // First match wins because the list is in reverse-insertion (newest-first) order.
    Data.Module moduleData = target.ProcessedData.GetOrAdd<Data.Module>(module);
    TargetPointer cur = moduleData.EnCDataList;
    while (cur != TargetPointer.Null)
    {
        Data.EnCData entry = target.ProcessedData.GetOrAdd<Data.EnCData>(cur);
        if (entry.Token == methodDef)
            return entry.EnCVersion;
        cur = entry.Next;
    }
    return new TargetNUInt(CorDB_DEFAULT_ENC_FUNCTION_VERSION);
}

TargetNUInt GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress)
{
    if (module == TargetPointer.Null)
        throw new ArgumentException();
    if (nativeCodeAddress.Value == 0)
        return new TargetNUInt(CorDB_DEFAULT_ENC_FUNCTION_VERSION);

    TargetPointer addr = new TargetPointer(nativeCodeAddress.Value);
    Data.Module moduleData = target.ProcessedData.GetOrAdd<Data.Module>(module);
    TargetPointer cur = moduleData.EnCDataList;
    while (cur != TargetPointer.Null)
    {
        Data.EnCData entry = target.ProcessedData.GetOrAdd<Data.EnCData>(cur);
        if (entry.Token == methodDef && entry.AddrOfCode == addr)
            return entry.EnCVersion;
        cur = entry.Next;
    }
    return new TargetNUInt(CorDB_DEFAULT_ENC_FUNCTION_VERSION);
}
```
