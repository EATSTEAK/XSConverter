using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace XSConverter.Format
{

    #region Label_Definition
    public sealed class Label
    {
        public string Name;
        public string Text;
        public int TextID;

        public Label()
        {
            Name = string.Empty;
            Text = string.Empty;
            TextID = 0;
        }
    }
    #endregion

    [DebuggerDisplay("{(string)this}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Magic
    {
        int value;
        public static implicit operator string(Magic magic) => Encoding.ASCII.GetString(BitConverter.GetBytes(magic.value));
        public static implicit operator Magic(string s) => new Magic { value = BitConverter.ToInt32(Encoding.ASCII.GetBytes(s), 0) };
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Header
    {
        public Magic magic;
        public short table0EntryCount;
        public short table0Offset;
        public int table1EntryCount;
        public int table1Offset;
        public int stringTableOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Entry
    {
        public short entryType;
        public short subEntryCount;
        public int subEntryOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SubEntry
    {
        public int ident;
        public uint value;
    }
}
