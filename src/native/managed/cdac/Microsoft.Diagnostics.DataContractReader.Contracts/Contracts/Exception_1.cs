// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Exception_1 : IException
{
    // From src/coreclr/vm/clrex.h - keep in sync.
    [Flags]
    private enum StackTraceElementFlags
    {
        STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE = 0x0001,
        STEF_IP_ADJUSTED = 0x0002,
        STEF_KEEPALIVE = 0x0004,
        STEF_CONTINUATION = 0x0008,
    }

    private readonly Target _target;

    internal Exception_1(Target target)
    {
        _target = target;
    }

    TargetPointer IException.GetNestedExceptionInfo(TargetPointer exceptionInfoAddr, out TargetPointer nextNestedExceptionInfo, out TargetPointer thrownObjectHandle)
    {
        Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exceptionInfoAddr);
        nextNestedExceptionInfo = exceptionInfo.PreviousNestedInfo;
        // ThrownObject is a direct object pointer stored in ExInfo::m_exception.
        // Return the address of the field as a "handle" - reading through it yields the
        // exception Object*. This has the same lifetime as the ExInfo (both are invalidated
        // when PopExInfos calls ReleaseResources). See dacimpl.h for the equivalent native
        // DAC documentation.
        Target.TypeInfo type = _target.GetTypeInfo(DataType.ExceptionInfo);
        thrownObjectHandle = exceptionInfoAddr + (ulong)type.Fields[nameof(Data.ExceptionInfo.ThrownObject)].Offset;
        return exceptionInfo.ThrownObject;
    }

    ExceptionData IException.GetExceptionData(TargetPointer exceptionAddr)
    {
        Data.Exception exception = _target.ProcessedData.GetOrAdd<Data.Exception>(exceptionAddr);
        return new ExceptionData(
            exception.Message,
            exception.InnerException,
            exception.StackTrace,
            exception.WatsonBuckets,
            exception.StackTraceString,
            exception.RemoteStackTraceString,
            exception.HResult,
            exception.XCode);
    }

    IEnumerable<ExceptionStackFrameData> IException.GetExceptionStackFrames(TargetPointer exceptionAddr)
    {
        // Resolve the stack trace storage object from System.Exception._stackTrace. There are two
        // possible storage shapes for this slot, mirroring ExceptionObject::GetStackTraceParts in
        // src/coreclr/vm/object.cpp:
        //
        //   1. _stackTrace points directly to the I1 (byte) array that holds the stack trace data.
        //   2. _stackTrace points to a PtrArray (Object[]) whose slot[0] is the I1 byte array and
        //      whose remaining slots are the keepAlive objects. This shape is used when any frame
        //      requires a keepAlive (e.g. dynamic / collectible methods). We do not consume the
        //      keepAlive entries here.
        Data.Exception exception = _target.ProcessedData.GetOrAdd<Data.Exception>(exceptionAddr);
        TargetPointer stackTraceObj = exception.StackTrace;
        if (stackTraceObj == TargetPointer.Null)
            yield break;

        TargetPointer byteArrayObj = ResolveStackTraceByteArray(stackTraceObj);
        if (byteArrayObj == TargetPointer.Null)
            yield break;

        // The byte array's element data begins after the fixed Array header. Within that data the
        // layout is:
        //   StackTraceArrayHeader header;
        //   StackTraceElement     elements[header.Size];
        Data.Array arrayData = _target.ProcessedData.GetOrAdd<Data.Array>(byteArrayObj);
        TargetPointer rawDataStart = arrayData.DataPointer;

        Data.StackTraceArrayHeader header = _target.ProcessedData.GetOrAdd<Data.StackTraceArrayHeader>(rawDataStart);
        uint count = header.Size;
        if (count == 0)
            yield break;

        Target.TypeInfo headerInfo = _target.GetTypeInfo(DataType.StackTraceArrayHeader);
        Target.TypeInfo elementInfo = _target.GetTypeInfo(DataType.StackTraceElement);
        Debug.Assert(headerInfo.Size is not null, "StackTraceArrayHeader must have a known size");
        Debug.Assert(elementInfo.Size is not null, "StackTraceElement must have a known size");

        ulong elementSize = elementInfo.Size!.Value;
        TargetPointer elementsBase = rawDataStart + headerInfo.Size!.Value;

        bool isAmd64 = _target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.X64;

        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        for (uint i = 0; i < count; i++)
        {
            TargetPointer elementAddr = elementsBase + i * elementSize;
            Data.StackTraceElement element = _target.ProcessedData.GetOrAdd<Data.StackTraceElement>(elementAddr);

            StackTraceElementFlags flags = (StackTraceElementFlags)element.Flags;

            // Mirror the AMD64 DAC fix-up: for the bottom frame whose IP has not been pre-adjusted
            // by the runtime, decrement IP by one to step from the return-address-style IP back
            // into the faulting instruction. See DebugStackTrace::GetStackFramesFromException
            // in src/coreclr/vm/debugdebugger.cpp.
            ulong ipValue = element.IP.Value;
            if (isAmd64 && i == 0 && (flags & StackTraceElementFlags.STEF_IP_ADJUSTED) == 0 && ipValue != 0)
            {
                ipValue -= 1;
            }

            uint methodDef = 0;
            if (element.pFunc != TargetPointer.Null)
            {
                MethodDescHandle md = rts.GetMethodDescHandle(element.pFunc);
                methodDef = rts.GetMethodToken(md);
            }

            bool isLastForeign = (flags & StackTraceElementFlags.STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE) != 0;

            yield return new ExceptionStackFrameData(
                IP: new TargetCodePointer(ipValue),
                MethodDef: methodDef,
                IsLastForeignExceptionFrame: isLastForeign);
        }
    }

    // Resolve the I1 byte array that backs the stack trace from the raw _stackTrace object.
    // _stackTrace is either the I1 byte array directly, or a PtrArray whose slot[0] is the byte
    // array (the rest of the slots are keepAlive entries we ignore). See
    // ExceptionObject::GetStackTraceParts.
    private TargetPointer ResolveStackTraceByteArray(TargetPointer stackTraceObj)
    {
        Contracts.IObject objectContract = _target.Contracts.Object;
        TargetPointer mt = objectContract.GetMethodTableAddress(stackTraceObj);
        if (mt == TargetPointer.Null)
            return TargetPointer.Null;

        Contracts.IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TypeHandle th = rts.GetTypeHandle(mt);

        if (rts.ContainsGCPointers(th))
        {
            // PtrArray combined form: read slot[0] which is the actual byte array.
            Data.Array arr = _target.ProcessedData.GetOrAdd<Data.Array>(stackTraceObj);
            if (arr.NumComponents == 0)
                return TargetPointer.Null;
            return _target.ReadPointer(arr.DataPointer);
        }

        // Plain byte-array form.
        return stackTraceObj;
    }
}
