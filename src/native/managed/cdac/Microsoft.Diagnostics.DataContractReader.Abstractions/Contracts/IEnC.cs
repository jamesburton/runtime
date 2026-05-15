// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IEnC : IContract
{
    static string IContract.Name { get; } = nameof(EnC);

    // Returns the latest EnC version number associated with the method identified by
    // (module, methodDef). If no EnC-jitted instance exists for that method, returns
    // the default EnC function version (1).
    TargetNUInt GetLatestEnCVersion(TargetPointer module, uint methodDef) => throw new NotImplementedException();

    // Returns the EnC version number for the specific jitted instance of the method
    // identified by (module, methodDef) whose hot region starts at the given native
    // code address. If no matching jitted instance exists (for example, the method
    // was never EnC-edited), returns the default EnC function version (1).
    TargetNUInt GetEnCVersion(TargetPointer module, uint methodDef, TargetCodePointer nativeCodeAddress) => throw new NotImplementedException();
}

public readonly struct EnC : IEnC
{
}
