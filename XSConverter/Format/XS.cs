using System.Collections.Generic;
using System.IO;
using XSConverter.IO;
using XSConverter.Compression;
using System.Text;
using System.Linq;
using System;

namespace XSConverter.Format
{
    public sealed class XS
    {
        public List<Label> Labels = new List<Label>();

        public Header header;
        public Level5.Method table0Comp;
        public List<Entry> entries;
        public Level5.Method table1Comp;
        public List<SubEntry> subEntries;
        public Level5.Method stringComp;
        public List<uint> offsets = new List<uint>();

        public XS(string filename)
        {
            using (BinaryReaderX br = new BinaryReaderX(File.OpenRead(filename)))
            {
                //Header
                header = br.ReadStruct<Header>();

                //Table0
                br.BaseStream.Position = header.table0Offset << 2;
                table0Comp = (Level5.Method)(br.ReadInt32() & 0x7);
                br.BaseStream.Position -= 4;
                entries = new BinaryReaderX(new MemoryStream(Level5.Decompress(br.BaseStream))).ReadMultiple<Entry>(header.table0EntryCount);

                //Table2
                br.BaseStream.Position = header.table1Offset << 2;
                table1Comp = (Level5.Method)(br.ReadInt32() & 0x7);
                br.BaseStream.Position -= 4;
                subEntries = new BinaryReaderX(new MemoryStream(Level5.Decompress(br.BaseStream))).ReadMultiple<SubEntry>(header.table1EntryCount);

                //Text
                br.BaseStream.Position = header.stringTableOffset << 2;
                stringComp = (Level5.Method)(br.ReadInt32() & 0x7);
                br.BaseStream.Position -= 4;
                using (var text = new BinaryReaderX(new MemoryStream(Level5.Decompress(br.BaseStream))))
                {
                    var entryCount = 0;
                    foreach (var entry in entries)
                    {
                        for(int i=entry.subEntryOffset;i<entry.subEntryOffset+entry.subEntryCount;i++)
                        {
                            var subEntry = subEntries[i];
                            if (subEntry.ident == 0x18 && !offsets.Contains(subEntry.value))
                            {
                                text.BaseStream.Position = subEntry.value;
                                offsets.Add(subEntry.value);
                                Labels.Add(new Label
                                {
                                    Name = $"{entryCount}:{i-entry.subEntryOffset}",
                                    TextID = i,
                                    Text = text.ReadCStringSJIS()
                                });
                            }
                        }
                        entryCount++;
                    }
                }
            }
        }

        public void Save(string filename)
        {
            var sjis = Encoding.GetEncoding("Shift-JIS");

            using (BinaryWriterX bw = new BinaryWriterX(File.Create(filename)))
            {
                //Table 0
                bw.BaseStream.Position = 0x14;
                bw.WriteMultipleCompressed(entries, table0Comp);
                bw.WriteAlignment(4);

                //Table 1
                header.table1Offset = (int)bw.BaseStream.Position >> 2;

                uint relOffset = 0;
                var count = 1;
                foreach (var label in Labels)
                {
                    var byteCount = (uint)sjis.GetByteCount(label.Text) + 1;
                    var entryValue = subEntries[label.TextID].value;
                    foreach (var entry in subEntries)
                    {
                        if (entry.ident == 0x18 && entry.value == entryValue)
                        {
                            entry.value = relOffset;
                        }
                    }
                    relOffset += byteCount;
                    count++;
                }
                offsets = subEntries.Where(e => e.ident == 0x18).Select(e => e.value).Distinct().ToList();

                bw.WriteMultipleCompressed(subEntries, table1Comp);
                bw.WriteAlignment(4);

                //Text
                header.stringTableOffset = (int)bw.BaseStream.Position >> 2;
                bw.WriteStringsCompressed(Labels.Select(l => l.Text), stringComp, sjis);
                bw.WriteAlignment(4);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(header);
            }
        }
    }

    public static class Extensions
    {
        public static void WriteMultipleCompressed<T>(this BinaryWriterX bw, IEnumerable<T> list, Level5.Method comp)
        {
            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterX(ms, true))
                foreach (var t in list)
                    bwIntern.WriteStruct(t);
            bw.Write(Level5.Compress(ms, comp));
        }

        public static void WriteStringsCompressed(this BinaryWriterX bw, IEnumerable<string> list, Level5.Method comp, Encoding enc)
        {
            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterX(ms, true))
                foreach (var t in list)
                {
                    bwIntern.Write(enc.GetBytes(t));
                    bwIntern.Write((byte)0);
                }
            bw.Write(Level5.Compress(ms, comp));
        }
    }
}
