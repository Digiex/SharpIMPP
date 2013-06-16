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
        public uint SeqNum = 0;
        public IMPPClient()
        {

        }

        public void Connect(string UserName, string UserDomain, string Password)
        {
            var srvRec = DnsSRV.GetSRVRecords("_impp._tcp." + UserDomain).First();
            TcpClient tcpClient = new TcpClient(srvRec.NameTarget, srvRec.Port);
            //TcpClient tcpClient = new TcpClient();
            //tcpClient.Connect(new IPEndPoint(IPAddress.Parse("74.201.34.10"), srvRec.Port));
            Console.WriteLine("Connected to " + srvRec);
            var bigend = new BigEndianStream(tcpClient.GetStream());

            var vp = new VersionPacket();
            vp.Write(bigend);
            vp.Read(bigend);
            Console.WriteLine("Got version " + vp.ReadProtocolVersion);
            bigend.Flush();

            TLVPacket tp = new TLVPacket();
            tp.MessageType = (ushort)StreamTypes.TType.FEATURES_SET;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLVPacket.TLV[] { new TLVPacket.TLV() { TLVType = 1, Value = new byte[] { 0x00, 0x01 } } };
            tp.Write(bigend);
            SeqNum++;

            bigend.Flush();
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
            else
            {
                bigend.Flush();
                //bigend.Close();
                SslStream ss = new SslStream(bigend);
                ss.AuthenticateAsClient(srvRec.NameTarget);
                bigend = new BigEndianStream(ss);
            }

            tp.MessageType = (ushort)StreamTypes.TType.AUTHENTICATE;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLVPacket.TLV[] {
                new TLVPacket.TLV() { TLVType = 0x0002, Value = new byte[]{ 0x00, 0x01 } } ,
                new TLVPacket.TLV() { TLVType = 0x0003, Value = ASCIIEncoding.UTF8.GetBytes(UserName) } ,
                new TLVPacket.TLV() { TLVType = 0x4001, Value = ASCIIEncoding.UTF8.GetBytes(Password) } ,
            };
            Password = null; //We don't need this anymore!
            tp.Write(bigend);
            SeqNum++;

            tp.Read(bigend);
            Console.WriteLine(tp);

            tp.MessageType = (ushort)DeviceTypes.TType.BIND;
            tp.MessageFamily = (ushort)DeviceTypes.TFamily.DEVICE;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            string os = "Windows";
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    os = "Mac OS X";
                    break;
                case PlatformID.Unix:
                    os = "Unix";
                    break;
            }
            tp.Block = new TLVPacket.TLV[] {
                new TLVPacket.TLV() { TLVType = 0x0001, Value = ASCIIEncoding.UTF8.GetBytes("SharpIMPP") } , //Client name
                new TLVPacket.TLV() { TLVType = 0x0002, Value = ASCIIEncoding.UTF8.GetBytes(os) } , //OS Name
                new TLVPacket.TLV() { TLVType = 0x0004, Value = ASCIIEncoding.UTF8.GetBytes(Environment.Is64BitOperatingSystem ? "amd64" : "i386") } , //Processor architecture
                new TLVPacket.TLV() { TLVType = 0x0005, Value = new byte[] { 0x00, 0x01 } } , //Client version
                new TLVPacket.TLV() { TLVType = 0x0006, Value = new byte[] { 0x00, 0x01 } } , //Build Number
                new TLVPacket.TLV() { TLVType = 0x0008, Value = ASCIIEncoding.UTF8.GetBytes(Environment.MachineName) } , //Machine name
                new TLVPacket.TLV() { TLVType = 0x000b, Value = new byte[] { 0x00, 0x01 } } , //Unknown, 00 01 in docs
                new TLVPacket.TLV() { TLVType = 0x0010, Value = new byte[] { 0x00 } } , //Unknown, 00 in docs
                new TLVPacket.TLV() { TLVType = 0x000d, Value = new byte[] { 0x00 , 0x01 , 0x42 , 0x04 , 0x00 , 0x02 , 0x42 , 0x09 , 0x42 ,
                                                                             0x03 , 0x42 , 0x06 , 0x42 , 0x05 , 0x42 , 0x07 , 0x42 , 0x08 } } , //Unknown, 00 01 42 04 00 02 42 09 42 03 42 06 42 05 42 07 42 08 in docs
                new TLVPacket.TLV() { TLVType = 0x0007, Value = ASCIIEncoding.UTF8.GetBytes("SharpIMPP/"+Environment.OSVersion.Platform.ToString()+" 1.0.0.1") } , //Full version string
            };
            tp.Write(bigend);
            SeqNum++;

            tp.Read(bigend);
            Console.WriteLine(tp);


            tp.MessageType = (ushort)ListTypes.TType.GET;
            tp.MessageFamily = (ushort)ListTypes.TFamily.LISTS;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLVPacket.TLV[] { };
            tp.Write(bigend);
            SeqNum++;

            tp.Read(bigend);
            Console.WriteLine(tp);
            Console.WriteLine("Your friends:");
            ushort lasttype = 0;
            foreach (TLVPacket.TLV t in tp.Block)
            {
                if (lasttype != t.TLVType)
                {
                    Console.WriteLine();
                    Console.Write(((ListTypes.TTupleType)t.TLVType).ToString() + ": ");
                    lasttype = t.TLVType;
                }
                var v = t.Value.Reverse();
                Console.Write(ASCIIEncoding.UTF8.GetString(v.ToArray()) + ", ");
            }

            //Just some debug reads to check if we missed something
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
            Console.WriteLine(bigend.ReadByte());
        }
    }
}
