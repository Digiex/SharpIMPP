using Chraft.Net;
using SharpIMPP.DNS;
using SharpIMPP.Enums;
using SharpIMPP.Net.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace SharpIMPP
{
    //IMPP Protocol documentation: https://www.trillian.im/impp/
    public class IMPPClient
    {
        const ushort ProtocolVersion = 8;
        const byte startByte = 0x6f;
        public uint SeqNum = 0;
        public IMPPClient()
        {

        }

        public void Connect(string UserName, string UserDomain, string Password)
        {
            var srvRec = DnsSRV.GetSRVRecords("_impp._tcp." + UserDomain).First();
            //TcpClient tcpClient = new TcpClient(srvRec.NameTarget, srvRec.Port);
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(new IPEndPoint(IPAddress.Parse("74.201.34.10"), srvRec.Port));
            Console.WriteLine("Connected to " + srvRec);
            var bigend = new BigEndianStream(tcpClient.GetStream());
            bigend.Write(startByte);
            bigend.WriteByte(0x01);
            bigend.Write(ProtocolVersion);
            //bigend.WriteByte(0x00);
            //bigend.WriteByte(0x08);
            bigend.Flush();
            //TODO: Find out how TLVs work
            Console.WriteLine("Start byte: " + bigend.ReadByte()); //Start byte
            Console.WriteLine("Channel byte: " + bigend.ReadByte()); //Channel byte
            Console.WriteLine("Got version " + bigend.ReadUShort());
            bigend.Flush();

            bigend.Write(startByte);
            bigend.WriteByte(0x02); //Channel byte
            TLVPacket tp = new TLVPacket();
            tp.MessageType = (ushort)StreamTypes.TType.FEATURES_SET;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLVPacket.TLV[] { new TLVPacket.TLV() { TLVType = 1, Value = new byte[] { 0x00, 0x01 } } };
            tp.Write(bigend);
            SeqNum++;

            bigend.Flush();
            Console.WriteLine("Start byte: " + bigend.ReadByte()); //Start byte
            Console.WriteLine("Channel byte: " + bigend.ReadByte()); //Channel byte
            tp.Read(bigend);
            bigend.Flush();
            Console.WriteLine(tp);
            bool UseSSL = false;
            foreach (byte b in tp.Block.First().Value)
            {
                if (b == (byte)0x01)
                {
                    UseSSL = true;
                }
            }
            if (!UseSSL)
            {
                Console.WriteLine("Warning! Expected SSL to be used!");
            }
            bigend.Flush();
            //bigend.Close();
            SslStream ss = new SslStream(bigend);
            ss.AuthenticateAsClient(srvRec.NameTarget);
            bigend = new BigEndianStream(ss);

            bigend.Write(startByte);
            bigend.WriteByte(0x02); //Channel byte
            tp.MessageType = (ushort)StreamTypes.TType.AUTHENTICATE;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            var userBytes = ASCIIEncoding.UTF8.GetBytes(UserName);
            var passBytes = ASCIIEncoding.UTF8.GetBytes(Password);
            Password = null; //We don't need this anymore!
            tp.Block = new TLVPacket.TLV[] {
                new TLVPacket.TLV() { TLVType = 0x0002, Value = new byte[]{ 0x00, 0x01 } } ,
                new TLVPacket.TLV() { TLVType = 0x0003, Value = userBytes } ,
                new TLVPacket.TLV() { TLVType = 0x4001, Value = passBytes } ,
            };
            passBytes = null; //We don't need this anymore!
            tp.Write(bigend);
            SeqNum++;

            Console.WriteLine("Start byte: " + bigend.ReadByte()); //Start byte
            Console.WriteLine("Channel byte: " + bigend.ReadByte()); //Channel byte
            tp.Read(bigend);
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
