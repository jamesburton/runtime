// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler
{
    /// <summary>
    /// Reads Win32 resources from a .res file (RC compiler output) and builds a
    /// resource directory tree for serialization into a PE .rsrc section.
    /// </summary>
    /// <remarks>
    /// Based on the .res parsing logic from Roslyn's CvtResFile and the resource
    /// directory tree serialization from NativeResourceWriter.
    /// </remarks>
    internal sealed class ResResourceSectionBuilder : ResourceSectionBuilder
    {
        private readonly List<ResourceEntry> _resources;

        private ResResourceSectionBuilder(List<ResourceEntry> resources)
        {
            _resources = resources;
        }

        /// <summary>
        /// Reads a .res file and builds a <see cref="ResResourceSectionBuilder"/>.
        /// </summary>
        public static ResResourceSectionBuilder FromResFile(string resFilePath)
        {
            if (!File.Exists(resFilePath))
            {
                throw new FileNotFoundException($"Resource file not found: '{resFilePath}'", resFilePath);
            }

            using var stream = File.OpenRead(resFilePath);
            var resources = ReadResFile(stream);
            return new ResResourceSectionBuilder(resources);
        }

        protected override void Serialize(BlobBuilder builder, SectionLocation location)
        {
            SerializeResourceTree(builder, _resources, location.RelativeVirtualAddress);
        }

        #region .res file parsing

        private static List<ResourceEntry> ReadResFile(Stream stream)
        {
            var reader = new BinaryReader(stream, System.Text.Encoding.Unicode);
            var resources = new List<ResourceEntry>();

            // RC.EXE output starts with a null resource (DataSize=0)
            var initial = reader.ReadUInt32();
            if (initial != 0)
            {
                throw new BadImageFormatException("Stream does not begin with a null resource and is not in .RES format.");
            }
            stream.Position = 0;

            while (stream.Position < stream.Length)
            {
                uint dataSize = reader.ReadUInt32();
                uint headerSize = reader.ReadUInt32();

                if (headerSize < 2 * sizeof(uint))
                {
                    throw new BadImageFormatException($"Resource header at offset 0x{stream.Position - 8:X} is malformed.");
                }

                // Skip null resources
                if (dataSize == 0)
                {
                    stream.Position += headerSize - 2 * sizeof(uint);
                    continue;
                }

                var type = ReadStringOrId(reader);
                var name = ReadStringOrId(reader);

                // Align to DWORD boundary
                stream.Position = (stream.Position + 3) & ~3;

                reader.ReadUInt32(); // DataVersion
                reader.ReadUInt16(); // MemoryFlags
                ushort languageId = reader.ReadUInt16();
                reader.ReadUInt32(); // Version
                reader.ReadUInt32(); // Characteristics

                byte[] data = reader.ReadBytes((int)dataSize);

                // Align to DWORD boundary
                stream.Position = (stream.Position + 3) & ~3;

                // Skip DLGINCLUDE resources (RT_DLGINCLUDE = 17)
                if (type.Name is null && type.Id == 17)
                {
                    continue;
                }

                resources.Add(new ResourceEntry
                {
                    Type = type,
                    Name = name,
                    LanguageId = languageId,
                    Data = data,
                    CodePage = 0,
                });
            }

            return resources;
        }

        private static ResourceId ReadStringOrId(BinaryReader reader)
        {
            char firstChar = reader.ReadChar();
            if (firstChar == '\xFFFF')
            {
                return new ResourceId { Id = reader.ReadUInt16() };
            }

            var sb = new System.Text.StringBuilder();
            char c = firstChar;
            while (c != '\0')
            {
                sb.Append(c);
                c = reader.ReadChar();
            }
            return new ResourceId { Name = sb.ToString(), Id = 0xFFFF };
        }

        #endregion

        #region Resource directory tree serialization

        private static void SerializeResourceTree(BlobBuilder builder, List<ResourceEntry> resources, int sectionRva)
        {
            // Sort resources: by type (string < ordinal), then by name (string < ordinal), then by language
            resources.Sort(CompareResources);

            // Build the three-level directory tree: Type → Name → Language
            var typeDir = new DirectoryNode();
            DirectoryNode? nameDir = null;
            DirectoryNode? langDir = null;
            int lastTypeId = int.MinValue;
            string? lastTypeName = null;
            int lastNameId = int.MinValue;
            string? lastNameStr = null;

            uint sizeOfDirectoryTree = 16; // root directory header

            foreach (var r in resources)
            {
                int typeId = r.Type.Name is not null ? -1 : r.Type.Id;
                string? typeName = r.Type.Name;
                int nameId = r.Name.Name is not null ? -1 : r.Name.Id;
                string? nameStr = r.Name.Name;

                bool typeDifferent = (typeId < 0 && typeName != lastTypeName) || typeId > lastTypeId;
                if (typeDifferent)
                {
                    lastTypeId = typeId;
                    lastTypeName = typeName;
                    if (typeId < 0) typeDir.NamedEntryCount++;
                    else typeDir.IdEntryCount++;

                    sizeOfDirectoryTree += 24; // directory header (16) + entry (8)
                    nameDir = new DirectoryNode();
                    typeDir.Entries.Add(new DirectoryEntry { Id = typeId, Name = typeName, Child = nameDir });
                }

                bool nameDifferent = typeDifferent || (nameId < 0 && nameStr != lastNameStr) || nameId > lastNameId;
                if (nameDifferent)
                {
                    lastNameId = nameId;
                    lastNameStr = nameStr;
                    if (nameId < 0) nameDir!.NamedEntryCount++;
                    else nameDir!.IdEntryCount++;

                    sizeOfDirectoryTree += 24; // directory header (16) + entry (8)
                    langDir = new DirectoryNode();
                    nameDir!.Entries.Add(new DirectoryEntry { Id = nameId, Name = nameStr, Child = langDir });
                }

                langDir!.IdEntryCount++;
                sizeOfDirectoryTree += 8; // entry (8)
                langDir.Entries.Add(new DirectoryEntry { Id = r.LanguageId, Resource = r });
            }

            // Serialize: directory tree first, then data entries + raw data
            var dataWriter = new BlobBuilder();
            WriteDirectory(typeDir, builder, 0, 0, sizeOfDirectoryTree, sectionRva, dataWriter);
            builder.LinkSuffix(dataWriter);
            builder.WriteByte(0);
            builder.Align(4);
        }

        private static void WriteDirectory(DirectoryNode dir, BlobBuilder writer, uint offset, uint level,
            uint sizeOfDirectoryTree, int rvaBase, BlobBuilder dataWriter)
        {
            // IMAGE_RESOURCE_DIRECTORY header
            writer.WriteUInt32(0); // Characteristics
            writer.WriteUInt32(0); // TimeDateStamp
            writer.WriteUInt32(0); // Version (Major/Minor)
            writer.WriteUInt16(dir.NamedEntryCount);
            writer.WriteUInt16(dir.IdEntryCount);

            uint entryCount = (uint)dir.Entries.Count;
            uint childOffset = offset + 16 + entryCount * 8;

            // First pass: write entries
            for (int i = 0; i < dir.Entries.Count; i++)
            {
                var entry = dir.Entries[i];
                uint nameOffset = (uint)dataWriter.Count + sizeOfDirectoryTree;

                if (entry.Child is not null)
                {
                    // Subdirectory entry
                    if (entry.Name is not null)
                    {
                        writer.WriteUInt32(nameOffset | 0x80000000);
                        dataWriter.WriteUInt16((ushort)entry.Name.Length);
                        foreach (char ch in entry.Name)
                        {
                            dataWriter.WriteUInt16(ch);
                        }
                    }
                    else
                    {
                        writer.WriteInt32(entry.Id);
                    }

                    writer.WriteUInt32(childOffset | 0x80000000);

                    if (level == 0)
                        childOffset += SizeOfDirectory(entry.Child);
                    else
                        childOffset += 16 + 8 * (uint)entry.Child.Entries.Count;
                }
                else
                {
                    // Data entry (leaf)
                    var r = entry.Resource!;

                    if (entry.Name is not null)
                    {
                        writer.WriteUInt32(nameOffset | 0x80000000);
                        dataWriter.WriteUInt16((ushort)entry.Name.Length);
                        foreach (char ch in entry.Name)
                        {
                            dataWriter.WriteUInt16(ch);
                        }
                    }
                    else
                    {
                        writer.WriteInt32(entry.Id);
                    }

                    // Offset to IMAGE_RESOURCE_DATA_ENTRY (in dataWriter)
                    writer.WriteUInt32(nameOffset);

                    // IMAGE_RESOURCE_DATA_ENTRY
                    dataWriter.WriteUInt32((uint)(rvaBase + sizeOfDirectoryTree + 16 + dataWriter.Count));
                    dataWriter.WriteUInt32((uint)r.Data.Length);
                    dataWriter.WriteUInt32(r.CodePage);
                    dataWriter.WriteUInt32(0); // Reserved

                    dataWriter.WriteBytes(r.Data);
                    while (dataWriter.Count % 4 != 0)
                    {
                        dataWriter.WriteByte(0);
                    }
                }
            }

            // Second pass: write subdirectories
            childOffset = offset + 16 + entryCount * 8;
            for (int i = 0; i < dir.Entries.Count; i++)
            {
                var entry = dir.Entries[i];
                if (entry.Child is not null)
                {
                    WriteDirectory(entry.Child, writer, childOffset, level + 1, sizeOfDirectoryTree, rvaBase, dataWriter);
                    if (level == 0)
                        childOffset += SizeOfDirectory(entry.Child);
                    else
                        childOffset += 16 + 8 * (uint)entry.Child.Entries.Count;
                }
            }
        }

        private static uint SizeOfDirectory(DirectoryNode dir)
        {
            uint size = 16 + 8 * (uint)dir.Entries.Count;
            foreach (var entry in dir.Entries)
            {
                if (entry.Child is not null)
                {
                    size += 16 + 8 * (uint)entry.Child.Entries.Count;
                }
            }
            return size;
        }

        private static int CompareResources(ResourceEntry left, ResourceEntry right)
        {
            int result = CompareIds(left.Type, right.Type);
            if (result != 0) return result;
            result = CompareIds(left.Name, right.Name);
            if (result != 0) return result;
            return left.LanguageId.CompareTo(right.LanguageId);
        }

        private static int CompareIds(ResourceId left, ResourceId right)
        {
            if (left.Name is null && right.Name is null) return left.Id.CompareTo(right.Id);
            if (left.Name is null) return 1;  // ordinals sort after strings
            if (right.Name is null) return -1;
            return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Data types

        private sealed class ResourceEntry
        {
            public ResourceId Type;
            public ResourceId Name;
            public ushort LanguageId;
            public byte[] Data = Array.Empty<byte>();
            public uint CodePage;
        }

        private struct ResourceId
        {
            public ushort Id;
            public string? Name;
        }

        private sealed class DirectoryNode
        {
            public ushort NamedEntryCount;
            public ushort IdEntryCount;
            public List<DirectoryEntry> Entries = new();
        }

        private sealed class DirectoryEntry
        {
            public int Id;
            public string? Name;
            public DirectoryNode? Child;
            public ResourceEntry? Resource;
        }

        #endregion
    }
}
