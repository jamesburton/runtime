// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class StubCodeNameFactory : IContractFactory<IStubCodeName>
{
    IStubCodeName IContractFactory<IStubCodeName>.CreateContract(Target target, int version)
    {
        TargetPointer executionManagerCodeRangeMapAddress = target.ReadGlobalPointer(Constants.Globals.ExecutionManagerCodeRangeMapAddress);
        Data.RangeSectionMap rangeSectionMap = target.ProcessedData.GetOrAdd<Data.RangeSectionMap>(executionManagerCodeRangeMapAddress);

        static TargetPointer TryGetPointerGlobal(Target t, string name)
        {
            if (t.TryReadGlobal<ulong>(name, out ulong? val))
                return new TargetPointer(val.Value);
            return TargetPointer.Null;
        }

        TargetPointer thePreStubEntryPoint = TryGetPointerGlobal(target, Constants.Globals.ThePreStubEntryPoint);
        TargetPointer genericPInvokeCalliHelper = TryGetPointerGlobal(target, Constants.Globals.InteropDispatchStubGenericPInvokeCalliHelper);
        TargetPointer varargPInvokeStub = TryGetPointerGlobal(target, Constants.Globals.InteropDispatchStubVarargPInvokeStub);
        TargetPointer varargPInvokeStub_RetBuffArg = TryGetPointerGlobal(target, Constants.Globals.InteropDispatchStubVarargPInvokeStub_RetBuffArg);
        TargetPointer genericCLRToCOMCallStub = TryGetPointerGlobal(target, Constants.Globals.InteropDispatchStubGenericCLRToCOMCallStub);
        TargetPointer tailCallJitHelper = TryGetPointerGlobal(target, Constants.Globals.TailCallStubManagerStubCodeAddress);

        return version switch
        {
            1 => new StubCodeName_1(
                    target,
                    rangeSectionMap,
                    thePreStubEntryPoint,
                    genericPInvokeCalliHelper,
                    varargPInvokeStub,
                    varargPInvokeStub_RetBuffArg,
                    genericCLRToCOMCallStub,
                    tailCallJitHelper),
            _ => default(StubCodeName),
        };
    }
}
