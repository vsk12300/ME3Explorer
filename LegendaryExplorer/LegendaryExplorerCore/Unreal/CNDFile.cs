﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace LegendaryExplorerCore.Unreal
{
    public class CNDFile
    {
        private const int Magic = 0x434F4E44;
        private const int Version = 1;

        private short unk1; //TODO: figure out what this is

        public List<ConditionalEntry> ConditionalEntries;

        public string FilePath;

        private void Read(Stream stream)
        {
            if (stream.ReadInt32() != Magic)
            {
                throw new Exception("This is not a conditional file!");
            }

            int version = stream.ReadInt32();
            if (version != Version)
            {
                throw new Exception($"Wrong file version! Expected '{Version}', got '{version}'");
            }

            unk1 = stream.ReadInt16();

            int entryCount = stream.ReadInt16();

            ConditionalEntries = new List<ConditionalEntry>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                ConditionalEntries.Add(new ConditionalEntry
                {
                    ID = stream.ReadInt32(),
                    Offset = stream.ReadInt32()
                });
            }

            int streamLength = (int)stream.Length;
            List<ConditionalEntry> sortedEntries = ConditionalEntries.OrderBy(entry => entry.Offset).ToList();
            for (int i = 0; i < entryCount; i++)
            {
                ConditionalEntry entry = sortedEntries[i];
                int nextOffset = streamLength;
                //we have to scan ahead because multiple entries can share the same offset
                for (int j = i; j < entryCount; j++)
                {
                    if (sortedEntries[j].Offset > entry.Offset)
                    {
                        nextOffset = sortedEntries[j].Offset;
                        break;
                    }
                }
                int size = nextOffset - entry.Offset;
                stream.JumpTo(entry.Offset);
                entry.Data = stream.ReadToBuffer(size);
            }
        }

        private void Write(Stream stream)
        {
            stream.WriteInt32(Magic);
            stream.WriteInt32(Version);
            stream.WriteInt16(unk1);
            stream.WriteUInt16((ushort)ConditionalEntries.Count);

            //This works, but is not the saving method bioware used.
            //tO replicate that, we would need to sort by Data size and combine conditions with the same Data 

            int sumOffset = (int)(stream.Position) + ConditionalEntries.Count * 8;
            //DO NOT CONVERT TO FOREACH! breaks it for some unknown reason
            for (int i = 0; i < ConditionalEntries.Count; i++)
            {
                ConditionalEntry entry = ConditionalEntries[i];
                stream.WriteInt32(entry.ID);
                stream.WriteInt32(sumOffset);
                sumOffset += entry.Data.Length;
            }

            foreach (ConditionalEntry entry in ConditionalEntries)
            {
                stream.WriteFromBuffer(entry.Data);
            }
        }

        public void ToFile(string filePath = null)
        {
            filePath ??= FilePath;
            using var fs = new FileStream(filePath, FileMode.Create);
            Write(fs);
        }

        public static CNDFile FromFile(string filePath)
        {
            var cnd = new CNDFile
            {
                FilePath = filePath
            };
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            cnd.Read(fs);
            return cnd;
        }

        [DebuggerDisplay("ID: {" + nameof(ID) + ("}, Offset: {" + nameof(Offset) + "}"))]
        public class ConditionalEntry
        {
            public int ID { get; init; }
            public int Offset;
            public byte[] Data;
        }

        
    }
}
