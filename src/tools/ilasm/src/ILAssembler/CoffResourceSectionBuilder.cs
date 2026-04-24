// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler
{
    /// <summary>
    /// Reads Win32 resources from a COFF .obj file (containing .rsrc$01 and .rsrc$02
    /// sections) and serializes them into a PE .rsrc section for <see cref="ManagedPEBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The .obj file must be in COFF format (not raw .res — use CvtRes.exe to convert).
    /// The format contains:
    ///   - .rsrc$01: resource directory (headers/entries with relocations pointing into .rsrc$02)
    ///   - .rsrc$02: resource raw data
    /// Relocations in .rsrc$01 are patched to use offsets relative to the merged section start.
    /// </remarks>
    internal sealed class CoffResourceSectionBuilder : ResourceSectionBuilder
    {
        // Resource directory (.rsrc$01, NOT yet patched — patching happens in Serialize)
        private readonly byte[] _resourceHeader;

        // Raw resource data (.rsrc$02)
        private readonly byte[] _resourceData;

        // Relocation entries: (offsetInHeader, offsetInRawData)
        // Each entry means: at _resourceHeader[offsetInHeader], write (sectionRVA + rsrc01Size + offsetInRawData)
        private readonly (int OffsetInHeader, uint OffsetInRawData)[] _relocations;

        private CoffResourceSectionBuilder(byte[] resourceHeader, byte[] resourceData, (int, uint)[] relocations)
        {
            _resourceHeader = resourceHeader;
            _resourceData = resourceData;
            _relocations = relocations;
        }

        /// <summary>
        /// Reads a COFF .obj resource file and builds a <see cref="CoffResourceSectionBuilder"/>.
        /// </summary>
        public static CoffResourceSectionBuilder FromObjectFile(string objFilePath)
        {
            if (!File.Exists(objFilePath))
            {
                throw new FileNotFoundException($"Resource file not found: '{objFilePath}'", objFilePath);
            }

            byte[] fileBytes = File.ReadAllBytes(objFilePath);
            using var peReader = new PEReader(new MemoryStream(fileBytes));
            var headers = peReader.PEHeaders;

            if (!headers.IsCoffOnly)
            {
                throw new BadImageFormatException("Resource file must be a COFF .obj file, not a PE executable. Use CvtRes.exe to convert .res files.");
            }

            // Find .rsrc$01 and .rsrc$02 sections
            SectionHeader? rsrc01 = null;
            SectionHeader? rsrc02 = null;
            int rsrc01Index = -1;

            for (int i = 0; i < headers.SectionHeaders.Length; i++)
            {
                var section = headers.SectionHeaders[i];
                if (section.Name == ".rsrc$01")
                {
                    rsrc01 = section;
                    rsrc01Index = i;
                }
                else if (section.Name == ".rsrc$02")
                {
                    rsrc02 = section;
                }
            }

            if (rsrc01 is null || rsrc02 is null)
            {
                throw new BadImageFormatException("COFF .obj file must contain both .rsrc$01 and .rsrc$02 sections.");
            }

            // Read section data
            byte[] headerData = new byte[rsrc01.Value.SizeOfRawData];
            Array.Copy(fileBytes, rsrc01.Value.PointerToRawData, headerData, 0, rsrc01.Value.SizeOfRawData);

            byte[] rawData = new byte[rsrc02.Value.SizeOfRawData];
            Array.Copy(fileBytes, rsrc02.Value.PointerToRawData, rawData, 0, rsrc02.Value.SizeOfRawData);

            // Parse relocations from .rsrc$01 to build deferred relocation entries.
            int relocCount = rsrc01.Value.NumberOfRelocations;
            int relocOffset = rsrc01.Value.PointerToRelocations;

            int symbolTableOffset = headers.CoffHeader.PointerToSymbolTable;
            int symbolCount = headers.CoffHeader.NumberOfSymbols;

            const int imageRelocationSize = 10; // IMAGE_RELOCATION: VirtualAddress(4) + SymbolTableIndex(4) + Type(2)
            const int imageSymbolSize = 18;      // IMAGE_SYMBOL: Name(8) + Value(4) + SectionNumber(2) + Type(2) + StorageClass(1) + NumberOfAuxSymbols(1)

            var relocations = new (int OffsetInHeader, uint OffsetInRawData)[relocCount];

            for (int i = 0; i < relocCount; i++)
            {
                int relocPos = relocOffset + (i * imageRelocationSize);

                uint virtualAddress = BitConverter.ToUInt32(fileBytes, relocPos);
                uint symbolTableIndex = BitConverter.ToUInt32(fileBytes, relocPos + 4);

                if (symbolTableIndex >= (uint)symbolCount)
                {
                    throw new BadImageFormatException($"Relocation {i} references symbol index {symbolTableIndex} but only {symbolCount} symbols exist.");
                }

                int symbolPos = symbolTableOffset + ((int)symbolTableIndex * imageSymbolSize);
                uint symbolValue = BitConverter.ToUInt32(fileBytes, symbolPos + 8);

                if (virtualAddress + 4 > (uint)headerData.Length)
                {
                    throw new BadImageFormatException($"Relocation virtual address {virtualAddress} exceeds .rsrc$01 size.");
                }

                // Store: at headerData[virtualAddress], we need to write (sectionRVA + rsrc01Size + symbolValue)
                relocations[i] = ((int)virtualAddress, symbolValue);
            }

            return new CoffResourceSectionBuilder(headerData, rawData, relocations);
        }

        protected override void Serialize(BlobBuilder builder, SectionLocation location)
        {
            // Apply relocations now that we know the section RVA.
            // Each relocation patches a 4-byte address in the resource header to point
            // to the correct RVA of data in .rsrc$02 (which follows .rsrc$01 in the merged section).
            byte[] patchedHeader = (byte[])_resourceHeader.Clone();
            uint rsrc01Size = (uint)_resourceHeader.Length;

            foreach (var (offsetInHeader, offsetInRawData) in _relocations)
            {
                uint rva = (uint)location.RelativeVirtualAddress + rsrc01Size + offsetInRawData;
                BitConverter.TryWriteBytes(patchedHeader.AsSpan(offsetInHeader), rva);
            }

            builder.WriteBytes(patchedHeader);
            builder.WriteBytes(_resourceData);
        }
    }
}
