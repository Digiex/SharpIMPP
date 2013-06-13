using Chraft.Net;
using SharpIMPP.DNS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharpIMPP
{
    //IMPP Protocol documentation: https://www.trillian.im/impp/
    public class IMPPClient
    {
        const ushort ProtocolVersion = 8;
        const byte startByte = 0x6f;
        public IMPPClient()
        {

        }

        public void Connect(string UserName, string UserDomain, string Password)
        {
            var srvRec = DnsSRV.GetSRVRecords("_impp._tcp." + UserDomain).First();
            TcpClient tcpClient = new TcpClient(srvRec.NameTarget, srvRec.Port);
            Console.WriteLine("Connected to " + srvRec);
            var bigend = new BigEndianStream(tcpClient.GetStream());
            bigend.Write(startByte);
            bigend.WriteByte(0x01);
            bigend.Write((short)ProtocolVersion);
            //bigend.WriteByte(0x00);
            //bigend.WriteByte(0x08);
            //TODO: Find out how TLVs work
            bigend.ReadByte(); //Start byte
            bigend.ReadByte(); //Channel byte
            Console.WriteLine("Got version "+bigend.ReadShort());
        }
    }
}
