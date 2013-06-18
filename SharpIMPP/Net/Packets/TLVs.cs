using Chraft.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Net.Packets
{
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
                //Console.WriteLine("Most significant bit: " + (msb ? 1 : 0));
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
            //Value = new byte[Length];
            Value = s.ReadBytesReversed(Length);
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
        public byte[] GetBytes()
        {
            return this.Value;
        }
    }
    public class StringTLV : TLV
    {
        new public string Value
        {
            get
            {
                return Encoding.UTF8.GetString(base.Value, 0, base.Value.Length);
            }
            set
            {
                base.Value = Encoding.UTF8.GetBytes(value);
            }
        }
    }

}
