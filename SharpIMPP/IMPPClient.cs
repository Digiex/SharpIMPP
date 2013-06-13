using Chraft.Net;
using SharpIMPP.DNS;
using SharpIMPP.Enums;
using SharpIMPP.Net.Packets;
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
            bigend.Write(ProtocolVersion);
            //bigend.WriteByte(0x00);
            //bigend.WriteByte(0x08);
            bigend.Flush();
            //TODO: Find out how TLVs work
            Console.WriteLine("Start byte: "+bigend.ReadByte()); //Start byte
            Console.WriteLine("Channel byte: " + bigend.ReadByte()); //Channel byte
            Console.WriteLine("Got version " + bigend.ReadUShort());
            bigend.Flush();

            bigend.Write(startByte);
            bigend.WriteByte(0x02); //Channel byte
            TLVPacket tp = new TLVPacket();
            tp.MessageType = (ushort)StreamTypes.TType.FEATURES_SET;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = 1;
            tp.Block = new TLVPacket.TLVBlock() { Is32 = false, TLVType = (ushort)StreamTypes.TType.FEATURES_SET };
            tp.Block.Value = new byte[] { 0x00, 0x03 };
            tp.Block.Length16 = (ushort)tp.Block.Value.Length;
            tp.BlockSize = tp.Block.GetSize();
            tp.Write(bigend);

            bigend.Flush();
            Console.WriteLine("Start byte: " + bigend.ReadByte()); //Start byte
            Console.WriteLine("Channel byte: " + bigend.ReadByte()); //Channel byte
            tp.Read(bigend);
            bigend.Flush();
            Console.WriteLine(tp);

            //Just some debug reads to check if we missed something
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
        }
    }
}
