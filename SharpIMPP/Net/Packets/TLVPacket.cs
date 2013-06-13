using Chraft.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Net.Packets
{
    class TLVPacket
    {
        #region TLV Headers
        public ushort Flags { get; set; }
        public ushort MessageFamily { get; set; }
        public ushort MessageType { get; set; }
        public uint SequenceNumber { get; set; }
        public uint BlockSize { get; set; }
        #endregion
        public TLVBlock Block { get; set; }

        public TLVPacket()
        {

        }
        public void Read(BigEndianStream s)
        {
            Flags = s.ReadUShort();
            MessageFamily = s.ReadUShort();
            MessageType = s.ReadUShort();
            SequenceNumber = s.ReadUInt();
            BlockSize = s.ReadUInt();
            Block = new TLVBlock();
            Block.Read(s, BlockSize);
        }
        public void Write(BigEndianStream s)
        {
            s.Write(Flags);
            s.Write(MessageFamily);
            s.Write(MessageType);
            s.Write(SequenceNumber);
            s.Write(BlockSize);
            Block.Write(s);
        }
        public class TLVBlock
        {
            public ushort TLVType { get; set; }
            public uint Length32 { get; set; }
            public ushort Length16 { get; set; }
            public byte[] Value { get; set; }
            public bool Is32 { get; set; }
            public TLVBlock()
            {

            }
            public void Read(BigEndianStream s, uint size)
            {
                TLVType = s.ReadUShort();
                bool msb = getmsb(TLVType);
                Console.WriteLine("Most significant bit: " + (msb ? 1 : 0));
                if (msb)
                {
                    Is32 = true;
                    Length32 = s.ReadUInt();
                    Value = new byte[Length32];
                    Value = s.ReadBytes(Length32);
                }
                else
                {
                    Is32 = false;
                    Length16 = s.ReadUShort();
                    Value = new byte[Length16];
                    Value = s.ReadBytes(Length16);
                }
            }
            public void Write(BigEndianStream s)
            {
                s.Write(TLVType);
                if (Is32)
                {
                    s.Write(Length32);
                }
                else
                {
                    s.Write(Length16);
                }
                foreach (byte b in Value)
                {
                    s.WriteByte(b);
                }
            }
            public uint GetSize()
            {
                uint s = 4;
                if (Is32)
                {
                    s += 2 + Length32;
                }
                else
                {
                    s += Length16;
                }
                return s;
            }
            bool getmsb(uint n)
            {
                if (n > ~n)
                    return true;
                else
                    return false;
            }
            public override string ToString()
            {
                return "{TLVType: " + TLVType + (Is32 ? ", Length32: " + Length32 : ", Length16: " + Length16) + ", Value: " + Value + "}";
            }

        }
        public override string ToString()
        {
            return ("{Flags: " + Flags + ", MessageFamily: " + MessageFamily + ", MessageType: " + MessageType
                + ", SequenceNumber: " + SequenceNumber + ", BlockSize: " + BlockSize + ", Block: " + Block + "}");
        }
    }
}
