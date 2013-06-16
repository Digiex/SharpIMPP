﻿using Chraft.Net;
using SharpIMPP.DNS;
using SharpIMPP.Enums;
using SharpIMPP.Net.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SharpIMPP
{
    //IMPP Protocol documentation: https://www.trillian.im/impp/
    public class IMPPClient
    {
        public uint SeqNum = 0;
        Thread ConnectionThread;
        #region Events
        public event ListEvent ListReceived;
        public delegate void ListEvent(object sender, ListEventArgs e);
        public class ListEventArgs : EventArgs
        {
            public List<ContactListItem> ContactList { get; set; }
        }
        public class ContactListItem
        {
            public ListTypes.TTupleType ContactType { get; set; }
            public string ContactName { get; set; }
        }
        #endregion
        public IMPPClient()
        {

        }

        public void Connect(string UserName, string UserDomain, string Password)
        {
            ConnectionThread = new Thread(() => connectInThread(UserName, UserDomain, Password));
            ConnectionThread.Start();
        }

        private void connectInThread(string UserName, string UserDomain, string Password)
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
            tp.Block = new TLV[] { new TLV() { TLVType = 1, Value = new byte[] { 0x00, 0x01 } } };
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
            tp.Block = new TLV[] {
                new TLV() { TLVType = 0x0002, Value = new byte[]{ 0x00, 0x01 } } ,
                new StringTLV() { TLVType = 0x0003, Value = UserName } ,
                new StringTLV() { TLVType = 0x4001, Value = Password } ,
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
            tp.Block = new TLV[] {
                new StringTLV() { TLVType = 0x0001, Value = "SharpIMPP" } , //Client name
                new StringTLV() { TLVType = 0x0002, Value = os } , //OS Name
                new StringTLV() { TLVType = 0x0004, Value = Environment.Is64BitOperatingSystem ? "amd64" : "i386" } , //Processor architecture
                new TLV() { TLVType = 0x0005, Value = new byte[] { 0x00, 0x01 } } , //Client version
                new TLV() { TLVType = 0x0006, Value = new byte[] { 0x00, 0x01 } } , //Build Number
                new StringTLV() { TLVType = 0x0008, Value = Environment.MachineName } , //Machine name
                new TLV() { TLVType = 0x000b, Value = new byte[] { 0x00, 0x01 } } , //Unknown, 00 01 in docs
                new TLV() { TLVType = 0x0010, Value = new byte[] { 0x00 } } , //Unknown, 00 in docs
                new TLV() { TLVType = 0x000d, Value = new byte[] { 0x00 , 0x01 , 0x42 , 0x04 , 0x00 , 0x02 , 0x42 , 0x09 , 0x42 ,
                                                                             0x03 , 0x42 , 0x06 , 0x42 , 0x05 , 0x42 , 0x07 , 0x42 , 0x08 } } , //Unknown, 00 01 42 04 00 02 42 09 42 03 42 06 42 05 42 07 42 08 in docs
                new StringTLV() { TLVType = 0x0007, Value = "SharpIMPP/"+Environment.OSVersion.Platform.ToString()+" 1.0.0.1" } , //Full version string
            };
            tp.Write(bigend);
            SeqNum++;

            tp.Read(bigend);
            Console.WriteLine(tp);


            tp.MessageType = (ushort)ListTypes.TType.GET;
            tp.MessageFamily = (ushort)ListTypes.TFamily.LISTS;
            tp.Flags = MessageFlags.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] { };
            tp.Write(bigend);
            SeqNum++;

            tp.Read(bigend);
            Console.WriteLine(tp);
            if (ListReceived != null)
            {
                List<ContactListItem> contacts = new List<ContactListItem>();
                foreach (TLV t in tp.Block)
                {
                    contacts.Add(new ContactListItem() { ContactType = (ListTypes.TTupleType)t.TLVType, ContactName = Encoding.UTF8.GetString(t.Value) });
                }
                ListReceived(this, new ListEventArgs() { ContactList = contacts });

            }

            //Just some debug reads to check if we missed something
            //Console.WriteLine(bigend.ReadByte());
            //Console.WriteLine(bigend.ReadByte());
            //Console.WriteLine(bigend.ReadByte());
            //Console.WriteLine(bigend.ReadByte());
            //Console.WriteLine(bigend.ReadByte());
        }
    }
}
