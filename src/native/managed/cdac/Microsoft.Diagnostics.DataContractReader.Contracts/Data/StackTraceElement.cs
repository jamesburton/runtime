// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

// One captured frame in an exception's _stackTrace. Layout matches the native runtime's
// StackTraceElement struct (which is also consumed by SOS).
internal sealed class StackTraceElement : IData<StackTraceElement>
{
    static StackTraceElement IData<StackTraceElement>.Create(Target target, TargetPointer address)
        => new StackTraceElement(target, address);

    public StackTraceElement(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StackTraceElement);

        IP = target.ReadPointerField(address, type, nameof(IP));
        SP = target.ReadPointerField(address, type, nameof(SP));
        pFunc = target.ReadPointerField(address, type, nameof(pFunc));
        Flags = target.ReadField<int>(address, type, nameof(Flags));
    }

    public TargetPointer IP { get; init; }
    public TargetPointer SP { get; init; }
    public TargetPointer pFunc { get; init; }
    public int Flags { get; init; }
}
