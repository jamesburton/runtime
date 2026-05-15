// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

// Header prefixed to the byte-array storage that backs a managed exception's _stackTrace.
// It is followed in memory by a contiguous array of StackTraceElement.
internal sealed class StackTraceArrayHeader : IData<StackTraceArrayHeader>
{
    static StackTraceArrayHeader IData<StackTraceArrayHeader>.Create(Target target, TargetPointer address)
        => new StackTraceArrayHeader(target, address);

    public StackTraceArrayHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StackTraceArrayHeader);

        Size = target.ReadField<uint>(address, type, nameof(Size));
        KeepAliveItemsCount = target.ReadField<uint>(address, type, nameof(KeepAliveItemsCount));
        Thread = target.ReadPointerField(address, type, nameof(Thread));
    }

    public uint Size { get; init; }
    public uint KeepAliveItemsCount { get; init; }
    public TargetPointer Thread { get; init; }
}
