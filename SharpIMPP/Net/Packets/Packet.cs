using Chraft.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Net.Packets
{
    class Packet
    {
        public const ushort ProtocolVersion = 8;
        public const byte StartByte = 0x6f;
        public virtual void Write(BigEndianStream s, bool inclHeader = true)
        {
            throw new NotImplementedException();
        }

        public virtual void Read(BigEndianStream s, bool inclHeader = true)
        {
            throw new NotImplementedException();
        }
    }
}
