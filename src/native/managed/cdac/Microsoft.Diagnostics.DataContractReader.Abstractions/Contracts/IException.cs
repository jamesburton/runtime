// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public record struct ExceptionData(
    TargetPointer Message,
    TargetPointer InnerException,
    TargetPointer StackTrace,
    TargetPointer WatsonBuckets,
    TargetPointer StackTraceString,
    TargetPointer RemoteStackTraceString,
    int HResult,
    int XCode);

// One frame of an exception's captured stack trace, as exposed by IException.GetExceptionStackFrames.
public readonly record struct ExceptionStackFrameData(
    TargetCodePointer IP,
    uint MethodDef,
    bool IsLastForeignExceptionFrame);

public interface IException : IContract
{
    static string IContract.Name { get; } = nameof(Exception);

    TargetPointer GetNestedExceptionInfo(TargetPointer exception, out TargetPointer nextNestedException, out TargetPointer thrownObjectHandle) => throw new NotImplementedException();
    ExceptionData GetExceptionData(TargetPointer managedException) => throw new NotImplementedException();

    // Enumerate the captured stack frames stored on a managed exception object (System.Exception._stackTrace).
    // Yields one entry per frame in stack-trace order. The IP value is the value stored on the frame, adjusted
    // for the AMD64 first-frame "faulting IP" quirk when STEF_IP_ADJUSTED is not set (mirroring the native DAC).
    IEnumerable<ExceptionStackFrameData> GetExceptionStackFrames(TargetPointer exception) => throw new NotImplementedException();
}

public readonly struct Exception : IException
{
    // Everything throws NotImplementedException
}
