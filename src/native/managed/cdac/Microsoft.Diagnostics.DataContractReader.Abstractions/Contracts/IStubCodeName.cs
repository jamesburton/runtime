// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Contract for identifying and naming CLR stubs at code addresses.
/// </summary>
public interface IStubCodeName : IContract
{
    static string IContract.Name { get; } = nameof(StubCodeName);

    /// <summary>
    /// Tries to determine whether the given code address is a CLR stub, and if so, what
    /// stub type it is and what name is associated with it.
    /// </summary>
    /// <param name="codeAddress">The code address to inspect.</param>
    /// <param name="methodDescAddress">
    ///   On success, the address of the <c>MethodDesc</c> if the stub is a precode pointing
    ///   to a method descriptor; <see cref="TargetPointer.Null"/> for other stub kinds.
    /// </param>
    /// <param name="stubName">
    ///   On success, a descriptive name for the stub when <paramref name="methodDescAddress"/>
    ///   is <see cref="TargetPointer.Null"/> and a name is known; <see langword="null"/> when
    ///   no specific name is available (the caller should use a "CLRStub@address" format).
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the address is a recognized CLR stub;
    ///   <see langword="false"/> if the address is not a CLR stub known to this contract.
    /// </returns>
    bool TryGetStubTypeAndName(
        TargetCodePointer codeAddress,
        out TargetPointer methodDescAddress,
        out string? stubName) => throw new System.NotImplementedException();
}

/// <summary>Default (no-op) implementation returned when the StubCodeName contract is unavailable.</summary>
public readonly struct StubCodeName : IStubCodeName
{
}
