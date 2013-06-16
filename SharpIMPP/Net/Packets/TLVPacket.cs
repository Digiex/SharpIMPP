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
        public class TLV
        {
            private ushort _tlvtype;
            public ushort TLVType
            {
                get { return _tlvtype; }
                set
                {
                    _tlvtype = value;
                    bool msb = getmsb(_tlvtype);
                    Console.WriteLine("Most significant bit: " + (msb ? 1 : 0));
                    if (msb)
                    {
                        Is32 = true;
                    }
                    else
                    {
                        Is32 = false;
                    }
                }
            }
            public uint Length { get; private set; }
            private byte[] _value;
            public byte[] Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    Length = (uint)value.Length;
                }
            }
            public bool Is32 { get; private set; }
            public TLV()
            {

            }
            public void Read(BigEndianStream s)
            {
                TLVType = s.ReadUShort();
                if (Is32)
                {
                    Length = s.ReadUInt();
                }
                else
                {
                    Length = s.ReadUShort();
                }
                Value = new byte[Length];
                Value = s.ReadBytes((int)Length);
            }
            public void Write(BigEndianStream s)
            {
                s.Write(TLVType);
                if (Is32)
                {
                    s.Write(Length);
                }
                else
                {
                    s.Write((ushort)Length);
                }
                s.Write(Value, 0, Value.Length);
            }
            public uint GetSize()
            {
                uint s = 4;
                if (Is32)
                {
                    s += 2;
                }
                s += Length;
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
                StringBuilder sb = new StringBuilder();
                sb.Append("{TLVType: " + TLVType + (Is32 ? ", Length32: " + Length : ", Length16: " + Length) + ", Value: byte[");
                int i = 0;
                foreach (byte b in Value)
                {
                    i++;
                    sb.AppendFormat("{0:x2}", b);
                    if (i != Value.Length)
                    {
                        sb.Append(", ");
                    }
                }
                    sb.Append("]}");
                return sb.ToString();
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
