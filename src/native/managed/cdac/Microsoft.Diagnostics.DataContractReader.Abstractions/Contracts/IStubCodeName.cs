// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Identifies which CLR stub manager category owns a code address.
/// Corresponds to the native <c>StubManager</c> subclass hierarchy.
/// </summary>
public enum StubManagerKind
{
    /// <summary>The address is ThePreStub (ThePreStubManager). The manager name is <c>"ThePreStub"</c>.</summary>
    ThePreStub,
    /// <summary>A precode stub (PrecodeStubManager). The manager name is typically <c>"MethodDescPrestub"</c>.</summary>
    Precode,
    /// <summary>A stub-link stub, such as a multicast delegate stub (StubLinkStubManager).</summary>
    StubLink,
    /// <summary>A back-to-back jump stub (JumpStubStubManager).</summary>
    JumpStub,
    /// <summary>A code-heap stub identified by RangeSectionStubManager, such as a VSD dispatch or resolve stub.</summary>
    RangeSection,
    /// <summary>An IL stub generated for interop (ILStubManager).</summary>
    ILStub,
    /// <summary>A P/Invoke thunk stub (PInvokeILStubManager).</summary>
    PInvoke,
    /// <summary>An interop dispatch stub (InteropDispatchStubManager): CLR-to-COM, vararg P/Invoke, or generic P/Invoke calli.</summary>
    InteropDispatch,
    /// <summary>A tail-call stub (TailCallStubManager, x86 only).</summary>
    TailCall,
}

/// <summary>
/// Contract for identifying and naming CLR stubs at code addresses.
/// </summary>
public interface IStubCodeName : IContract
{
    static string IContract.Name { get; } = nameof(StubCodeName);

    /// <summary>
    /// Tries to determine whether the given code address is a CLR stub, and if so, what
    /// kind of stub manager owns it and what name is associated with it.
    /// </summary>
    /// <param name="codeAddress">The code address to inspect.</param>
    /// <param name="kind">
    ///   On success, the <see cref="StubManagerKind"/> identifying the stub manager.
    /// </param>
    /// <param name="managerName">
    ///   On success, the stub manager name returned by <c>GetStubManagerName</c> in the
    ///   native runtime (for example <c>"MethodDescPrestub"</c> for
    ///   <c>PrecodeStubManager</c>); <see langword="null"/> if no name is available.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the address is a recognized CLR stub;
    ///   <see langword="false"/> if the address is not a CLR stub known to this contract.
    /// </returns>
    bool TryGetStubTypeAndName(
        TargetCodePointer codeAddress,
        out StubManagerKind kind,
        out string? managerName) => throw new System.NotImplementedException();
}

/// <summary>Default (no-op) implementation returned when the StubCodeName contract is unavailable.</summary>
public readonly struct StubCodeName : IStubCodeName
{
}
