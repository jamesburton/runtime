# Contract StubCodeName

This contract identifies CLR stubs at arbitrary code addresses and returns either
the `MethodDesc` the stub belongs to (for precode stubs) or a descriptive stub name
(for other CLR stub types).

## APIs of contract

```csharp
// Tries to determine whether codeAddress is a CLR stub.
//
// Returns true if the address is a recognized CLR stub.  On success:
//   - methodDescAddress is set to the MethodDesc if the stub is a precode; TargetPointer.Null otherwise.
//   - stubName is set to a descriptive name for non-precode stubs when a name is known;
//     null when no specific name is available.
//     When both methodDescAddress is Null and stubName is null the caller should format the
//     result as "CLRStub@<address>".
//
// Returns false if the address is not a recognizable CLR stub.
bool TryGetStubTypeAndName(
    TargetCodePointer codeAddress,
    out TargetPointer methodDescAddress,
    out string? stubName);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `RangeSectionMap` | `TopLevelData` | Top-level pointer of the range-section map |
| `RangeSectionFragment` | `RangeBegin` | Inclusive start of the address range covered by this fragment |
| `RangeSectionFragment` | `RangeEndOpen` | Exclusive end of the address range covered by this fragment |
| `RangeSectionFragment` | `RangeSection` | Pointer to the owning `RangeSection` |
| `RangeSectionFragment` | `Next` | Next fragment in the same map bucket (low bit used for collectible flag) |
| `RangeSection` | `RangeBegin` | Inclusive start address |
| `RangeSection` | `RangeEndOpen` | Exclusive end address |
| `RangeSection` | `Flags` | Bitfield: 0x02 = CodeHeap, 0x04 = RangeList |
| `RangeSection` | `NextForDelete` | Non-null while the section is being removed |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ExecutionManagerCodeRangeMapAddress` | TargetPointer | Address of the top-level range-section map |

Contracts used:
| Contract Name | Purpose |
| --- | --- |
| `PrecodeStubs` | Resolves a precode address to its `MethodDesc` |

### Algorithm

```csharp
bool TryGetStubTypeAndName(TargetCodePointer codeAddress,
    out TargetPointer methodDescAddress, out string? stubName)
{
    methodDescAddress = TargetPointer.Null;
    stubName = null;

    // 1. Locate the range section that contains the address.
    TargetPointer topMapAddr = target.ReadGlobalPointer("ExecutionManagerCodeRangeMapAddress");
    Data.RangeSectionMap topMap = /* read topMapAddr */;

    TargetPointer fragmentPtr = RangeSectionMap.FindFragment(target, topMap, codeAddress);
    while (fragmentPtr != Null)
    {
        Data.RangeSectionFragment frag = /* read fragmentPtr */;
        if (frag.Contains(codeAddress)) break;
        fragmentPtr = frag.Next;
    }
    if (fragmentPtr == Null)
        return false; // Address is not in any known range section.

    Data.RangeSectionFragment fragment = /* read fragmentPtr */;
    Data.RangeSection rangeSection = /* read fragment.RangeSection */;

    if (rangeSection.NextForDelete != Null)
        return false; // Range section is being deleted.

    bool isRangeList = (rangeSection.Flags & 0x04) != 0;
    if (!isRangeList)
        return false; // Code-heap stubs are not yet identified in this version.

    // 2. The address is in a precode / range-list section.
    try
    {
        methodDescAddress = PrecodeStubs.GetMethodDescFromStubAddress(codeAddress);
        return true; // Precode pointing to a MethodDesc.
    }
    catch (InvalidOperationException)
    {
        // Not a valid precode layout (e.g., a DynamicHelper stub).
        // Still a CLR stub — report it with null methodDescAddress and null stubName
        // so the caller formats it as "CLRStub@address".
        return true;
    }
}
```

### Notes

- **Code-heap stubs** (e.g., VSD dispatch stubs, StubLink stubs) that live inside JIT
  code heaps are **not yet identified** by version 1.  `TryGetStubTypeAndName` returns
  `false` for such addresses.  Callers must fall back to a generic representation.
- **JumpStub** handling from the native DAC's `RawGetMethodName` is intentionally
  omitted: it was dead code in the C++ implementation and is not reproduced here.
- **Interpreter precode** (`GetInterpreterCodeFromInterpreterPrecodeIfPresent`) is
  treated as a no-op in this version.
