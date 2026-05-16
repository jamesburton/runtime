// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    [GeneratedComInterface]
    [Guid("78F11E0E-C576-4E3D-BC40-E9A3297D4DB7")]
    partial interface IActivationFactoryIidOutObject
    {
        void GetActivationFactory(
            in Guid iid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object factory);
    }

    [GeneratedComInterface]
    [Guid("78F11E0E-C576-4E3D-BC40-E9A3297D4DB7")]
    partial interface IIidSharedOutObjects
    {
        void M(
            in Guid iid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object a,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object b);
    }

    [GeneratedComInterface]
    [Guid("78F11E0E-C576-4E3D-BC40-E9A3297D4DB7")]
    partial interface IIidDifferentOutObjects
    {
        void M(
            in Guid iidA,
            in Guid iidB,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object a,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 1)] out object b);
    }

    [GeneratedComInterface]
    [Guid("78F11E0E-C576-4E3D-BC40-E9A3297D4DB7")]
    partial interface IIidInterleavedParameters
    {
        void M(
            int extra,
            in Guid iidA,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 1)] out object a,
            [MarshalAs(UnmanagedType.Interface)] out object b,
            in Guid iidC,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 4)] out object c);
    }

    [GeneratedComInterface]
    [Guid("78F11E0E-C576-4E3D-BC40-E9A3297D4DB7")]
    partial interface IIidByValueGuid
    {
        void M(
            Guid iid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object factory);
    }

    [GeneratedComInterface]
    [Guid("78F11E0E-C576-4E3D-BC40-E9A3297D4DB7")]
    partial interface IIidRefGuid
    {
        void M(
            ref Guid iid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 0)] out object factory);
    }
}
