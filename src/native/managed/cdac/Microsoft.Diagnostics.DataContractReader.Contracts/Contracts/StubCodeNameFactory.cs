// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class StubCodeNameFactory : IContractFactory<IStubCodeName>
{
    IStubCodeName IContractFactory<IStubCodeName>.CreateContract(Target target, int version)
    {
        TargetPointer executionManagerCodeRangeMapAddress = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap rangeSectionMap = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(executionManagerCodeRangeMapAddress);
        return version switch
        {
            1 => new StubCodeName_1(
                    target,
                    rangeSectionMap,
                    target.ReadGlobalString(Constants.Globals.PrecodeStubManagerName)),
            _ => default(StubCodeName),
        };
    }
}
