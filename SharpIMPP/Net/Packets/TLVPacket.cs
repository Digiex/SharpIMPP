using Chraft.Net;
using SharpIMPP.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Net.Packets
{
    class TLVPacket : Packet
    {
        public const byte TLVChannelByte = 0x02;
        #region TLV Headers
        public ushort Flags { get; set; }
        public ushort MessageFamily { get; set; }
        public ushort MessageType { get; set; }
        public uint SequenceNumber { get; set; }
        public uint BlockSize { get; set; }
        #endregion
        public TLV[] Block { get; set; }

        public TLVPacket()
        {

        }
        public override void Read(BigEndianStream s)
        {
            s.ReadByte(); //Start byte
            s.ReadByte(); //Channel byte
            Flags = s.ReadUShort();
            MessageFamily = s.ReadUShort();
            MessageType = s.ReadUShort();
            SequenceNumber = s.ReadUInt();
            BlockSize = s.ReadUInt();
            if (BlockSize == 0)
            {
                Block = new TLV[0];
            }
            else
            {
                uint left = BlockSize;
                ArrayList bList = new ArrayList();
                while (left > 0)
                {
                    var b = new TLV();
                    b.Read(s);
                    bList.Add(b);
                    left -= b.GetSize();
                }
                Block = (TLV[])bList.ToArray(typeof(TLV));
            }
            //byte[] block = s.ReadBytes(BlockSize);
            //Console.WriteLine(BitConverter.ToString(block));
        }
        public override void Write(BigEndianStream s)
        {
            s.WriteByte(StartByte);
            s.WriteByte(TLVChannelByte);
            s.Write(Flags);
            s.Write(MessageFamily);
            s.Write(MessageType);
            s.Write(SequenceNumber);
            BlockSize = 0;
            foreach (TLV t in Block)
            {
                BlockSize += t.GetSize();
            }
            s.Write(BlockSize);
            foreach (TLV t in Block)
            {
                t.Write(s);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
                sb.Append("{Flags: " + Flags + ", MessageFamily: " + MessageFamily + ", MessageType: " + MessageType
                + ", SequenceNumber: " + SequenceNumber + ", BlockSize: " + BlockSize + ", Block: TLV[");
                int i = 0;
                foreach (TLV t in Block)
                {
                    i++;
                    sb.Append(t.ToString());
                    if (i != Block.Length)
                    {
                        sb.Append(", ");
                    }
                }
            sb.Append("]}");
                return sb.ToString();
        }
    }
}
