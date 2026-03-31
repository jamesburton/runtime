// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IPrecodeStubs : IContract
{
    static string IContract.Name { get; } = nameof(PrecodeStubs);
    TargetPointer GetMethodDescFromStubAddress(TargetCodePointer entryPoint) => throw new System.NotImplementedException();

    /// <summary>
    /// Given a code address that lies somewhere inside a precode stub, returns the sequence of
    /// aligned candidate entry-point addresses that should be tried in order to identify the
    /// owning precode.  Mirrors the native loop in <c>RawGetMethodName</c> for
    /// <c>PrecodeStubManager</c>: starting from the pointer-size-aligned address of
    /// <paramref name="codeAddress"/> and walking backwards by <c>PRECODE_ALIGNMENT</c>
    /// (<c>sizeof(void*)</c>) for <c>maxPrecodeSize / PRECODE_ALIGNMENT</c> steps.
    /// </summary>
    IEnumerable<TargetCodePointer> GetPossiblePrecodeAddresses(TargetCodePointer codeAddress) => throw new System.NotImplementedException();
}

public readonly struct PrecodeStubs : IPrecodeStubs
{

}
