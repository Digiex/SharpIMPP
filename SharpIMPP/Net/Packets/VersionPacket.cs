using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Net.Packets
{
    class VersionPacket : Packet
    {
        public ushort ReadProtocolVersion { get; private set; }
        public override void Write(Chraft.Net.BigEndianStream s)
        {
            s.WriteByte(StartByte);
            s.WriteByte(0x01); // Channel byte
            s.Write(Packet.ProtocolVersion); //Let's force the protocol version
        }
        public override void Read(Chraft.Net.BigEndianStream s)
        {
            s.ReadByte(); //Start byte
            byte channelByte = s.ReadByte();
            if (channelByte != (byte)0x01)
            {
                throw new Exception("Start byte was "+channelByte+", expected 1");
            }
            this.ReadProtocolVersion = s.ReadUShort();
        }
    }
}
