using Chraft.Net;
using SharpIMPP.DNS;
using SharpIMPP.Enums;
using SharpIMPP.Net.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
#if WINDOWS
using System.Net.Security;
using System.Net.Sockets;
#endif
using System.Text;
using System.Threading;
#if NETFX_CORE
using Windows.System.Threading;
using SharpIMPP.WinRT.Net;
#endif

namespace SharpIMPP
{
    //IMPP Protocol documentation: https://www.trillian.im/impp/
    public class IMPPClient
    {
        private uint _SeqNum = 0;
        private uint SeqNum
        {
            get
            {
                _SeqNum++;
                return _SeqNum;
            }
        }
#if WINDOWS
        Thread ConnectionThread;
#endif
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

        private BigEndianStream stream;

        private string username;

        public string DeviceName { get; private set; }

        public IMPPClient()
        {

        }

        public void Connect(string UserName, string UserDomain, string Password)
        {
#if WINDOWS
            ConnectionThread = new Thread(() => connectInThread(UserName, UserDomain, Password));
            ConnectionThread.Start();
#elif NETFX_CORE
            ThreadPool.RunAsync((source) => { connectInThread(UserName, UserDomain, Password); });
#endif
        }

        private void connectInThread(string UserName, string UserDomain, string Password, string IP = null)
        {
            username = UserName;
            var srvRec = DnsSRV.GetSRVRecords("_impp._tcp." + UserDomain).First();
            TcpClient tcpClient;
            if (IP == null)
            {
                tcpClient = new TcpClient(srvRec.NameTarget, srvRec.Port);
            }
            else
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(new IPEndPoint(IPAddress.Parse(IP), srvRec.Port));
            }
            WriteDebugLine("Connected to " + srvRec);
            tcpClient.GetStream().ReadTimeout = Timeout.Infinite;
            stream = new BigEndianStream(tcpClient.GetStream());
            var vp = new VersionPacket();
            vp.Write(stream);
            vp.Read(stream);
            WriteDebugLine("Got version " + vp.ReadProtocolVersion);
            stream.Flush();

            TLVPacket tp = new TLVPacket();
            tp.MessageType = (ushort)StreamTypes.TType.FEATURES_SET;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] { new TLV() { TLVType = (ushort)StreamTypes.TTupleType.FEATURES, Value = new byte[] { 0x00, 0x01 } } };
            tp.Write(stream);


            stream.Flush();
            tp.Read(stream);
            stream.Flush();
            WriteDebugLine("Features set: " + tp);
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
                WriteDebugLine("Warning! Expected SSL to be used!");
            }
            else
            {
                stream.Flush();
                //stream.Close();
                SslStream ss = new SslStream(stream);
                ss.AuthenticateAsClient(srvRec.NameTarget);
                stream = new BigEndianStream(ss);
            }

            tp = new TLVPacket();
            tp.MessageType = (ushort)StreamTypes.TType.AUTHENTICATE;
            tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] {
                new TLV() { TLVType = 0x0002, Value = new byte[]{ 0x00, 0x01 } } ,
                new StringTLV() { TLVType = 0x0003, Value = UserName } ,
                new StringTLV() { TLVType = 0x4001, Value = Password } ,
            };
            Password = null; //We don't need this anymore!
            tp.Write(stream);

            tp.Read(stream);
            WriteDebugLine("Authenticate: " + tp);

            tp = new TLVPacket();
            tp.MessageType = (ushort)DeviceTypes.TType.BIND;
            tp.MessageFamily = (ushort)DeviceTypes.TFamily.DEVICE;
            tp.Flags = Globals.MF_REQUEST;
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
                new TLV() { TLVType = 0x000b, Value = new byte[] { 0x00, 0x01 } } , //Status
                new TLV() { TLVType = 0x0010, Value = new byte[] { 0x01 } } , //IS_STATUS_AUTOMATIC
                new TLV() { TLVType = 0x000d, Value = new byte[] { 0x00, 0x01 } } , //Capabilities
                new StringTLV() { TLVType = 0x0007, Value = "SharpIMPP/"+Environment.OSVersion.Platform.ToString()+" 1.0.0.1" } , //Description
            };
            tp.Write(stream);


            tp.Read(stream);
            WriteDebugLine("Device bind: " + tp);
            bool reconnect = false;
            foreach (TLV t in tp.Block)
            {
                if (t.TLVType == 0)
                {
                    if (t.Value == new byte[] { 80, 04 })
                    {
                        reconnect = true;
                    }
                    else
                    {
                        throw new Exception("Error");
                    }
                }
                else if (t.TLVType == (ushort)DeviceTypes.TTupleType.DEVICE_NAME)
                {
                    this.DeviceName = Encoding.UTF8.GetString(t.Value);
                }
                else if (t.TLVType == (ushort)DeviceTypes.TTupleType.SERVER)
                {
                    IP = Encoding.UTF8.GetString(t.Value);
                }
            }
            if (reconnect && IP != null)
            {
                tcpClient.GetStream().Dispose();
                connectInThread(UserName, UserDomain, Password, IP);
                return;
            }

            tp = new TLVPacket();
            tp.MessageType = (ushort)ListTypes.TType.GET;
            tp.MessageFamily = (ushort)ListTypes.TFamily.LISTS;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] { };
            tp.Write(stream);


            tp.Read(stream);
            WriteDebugLine("Lists get: " + tp);
            if (ListReceived != null)
            {
                List<ContactListItem> contacts = new List<ContactListItem>();
                foreach (TLV t in tp.Block)
                {
                    contacts.Add(new ContactListItem() { ContactType = (ListTypes.TTupleType)t.TLVType, ContactName = Encoding.UTF8.GetString(t.Value) });
                }
                RaiseEventOnUIThread(this.ListReceived, new object[] { this, new ListEventArgs() { ContactList = contacts } });

            }


            tp = new TLVPacket();
            tp.MessageType = (ushort)GroupChatTypes.TType.GET;
            tp.MessageFamily = (ushort)GroupChatTypes.TFamily.GROUP_CHATS;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] { };
            tp.Write(stream);


            tp.Read(stream);
            WriteDebugLine("Group chats: " + tp);

            tp = new TLVPacket();
            tp.MessageType = (ushort)IMTypes.TType.OFFLINE_MESSAGES_GET;
            tp.MessageFamily = (ushort)IMTypes.TFamily.IM;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Write(stream);


            tp.Read(stream);
            WriteDebugLine("Offline IMs: " + tp);
            byte[] timestamp = new byte[0];
            foreach (TLV t in tp.Block)
            {
                if (t.TLVType == (ushort)IMTypes.TTupleType.TIMESTAMP)
                {
                    timestamp = t.Value;
                }
                else if (t.TLVType == (ushort)IMTypes.TTupleType.OFFLINE_MESSAGE)
                {
                    WriteDebugLine("Offline message: " + Encoding.UTF8.GetString(t.Value));
                }
            }

            tp = new TLVPacket();
            tp.MessageType = (ushort)IMTypes.TType.OFFLINE_MESSAGES_DELETE;
            tp.MessageFamily = (ushort)IMTypes.TFamily.IM;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] {
                new TLV() { TLVType = (ushort)IMTypes.TTupleType.TIMESTAMP, Value = timestamp}
            };
            tp.Write(stream);


            tp.Read(stream);
            WriteDebugLine("Deleted offline IMs: " + tp);


            tp = new TLVPacket();
            tp.MessageType = (ushort)PresenceTypes.TType.GET;
            tp.MessageFamily = (ushort)PresenceTypes.TFamily.PRESENCE;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[0];
            tp.Write(stream);


            //tp.Read(stream);
            //WriteDebugLine("Presence set: " + tp);


            Thread pingThread = new Thread(new ThreadStart(doPing));
            pingThread.Start();
            while (stream.CanRead)
            {
                tp = new TLVPacket();
                WriteDebugLine("Start byte: " + stream.ReadByte());
                WriteDebugLine("Channel byte: " + stream.ReadByte());
                tp.Read(stream, false);
                if (tp.MessageType == (ushort)PresenceTypes.TType.GET && tp.MessageFamily == (ushort)PresenceTypes.TFamily.PRESENCE)
                {
                    String str = "User:";
                    foreach (TLV t in tp.Block)
                    {
                        if (t.TLVType == (ushort)PresenceTypes.TTupleType.NICKNAME)
                        {
                            str += " Nick: " + Encoding.UTF8.GetString(t.Value);
                        }
                        else if (t.TLVType == 16385)
                        {
                            str += " Email: " + Encoding.UTF8.GetString(t.Value);
                        }
                        else if (t.TLVType == (ushort)PresenceTypes.TTupleType.STATUS)
                        {
                            str += " Status: " + t.Value.Last();
                        }
                        else if (t.TLVType == (ushort)PresenceTypes.TTupleType.STATUS_MESSAGE)
                        {
                            str += " Message: " + Encoding.UTF8.GetString(t.Value);
                        }
                    }
                    WriteDebugLine(str);
                }
                else if (tp.MessageFamily == (ushort)StreamTypes.TFamily.STREAM && tp.MessageType == (ushort)StreamTypes.TType.PING)
                {
                    WriteDebugLine("Pong");
                }
                else
                {
                    WriteDebugLine(tp);
                }
            }

            //Just some debug reads to check if we missed something
            //WriteDebugLine(stream.ReadByte());
            //WriteDebugLine(stream.ReadByte());
            //WriteDebugLine(stream.ReadByte());
            //WriteDebugLine(stream.ReadByte());
            //WriteDebugLine(stream.ReadByte());
        }
        private void doPing()
        {
            while (stream.CanWrite)
            {
                TLVPacket tp = new TLVPacket();
                tp.MessageType = (ushort)StreamTypes.TType.PING;
                tp.MessageFamily = (ushort)StreamTypes.TFamily.STREAM;
                tp.Flags = Globals.MF_REQUEST;
                tp.SequenceNumber = SeqNum;
                tp.BlockSize = 0;
                tp.Write(stream);

                WriteDebugLine("Ping");
                Thread.Sleep(120000);
            }
        }
        public void Disconnect()
        {
            TLVPacket tp = new TLVPacket();
            tp.MessageType = (ushort)DeviceTypes.TType.UNBIND;
            tp.MessageFamily = (ushort)DeviceTypes.TFamily.DEVICE;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] {
                new StringTLV() { TLVType = 0x0008, Value = this.DeviceName } , //Client name
            };
            tp.Write(stream);

            stream.Flush();
            stream.Dispose();
        }

        public void SendChat(string user, string message)
        {
            TLVPacket tp = new TLVPacket();
            tp.MessageType = (ushort)IMTypes.TType.MESSAGE_SEND;
            tp.MessageFamily = (ushort)IMTypes.TFamily.IM;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] {
                new StringTLV() { TLVType = 0x0001, Value = user } , //To
                new StringTLV() { TLVType = 0x0002, Value =  username } , //From
                new TLV() { TLVType = 0x0003, Value = new byte[] { 0x00, 0x01 } } , //CAPABILITY
                new TLV() { TLVType = 0x0004, Value = new byte[] { 0x00, 0x00, 0x00, 0x00 } } , //MESSAGE_ID
                new TLV() { TLVType = 0x0005, Value = BitConverter.GetBytes(message.Length).Reverse().ToArray() } , //MESSAGE_SIZE
                new StringTLV() { TLVType = 0x0006, Value =  message } , //MESSAGE_CHUNK
                new TLV() { TLVType = 0x0007, Value =  BitConverter.GetBytes(DateTime.UtcNow.Ticks).Reverse().ToArray() } , //CREATED_AT
            };
            tp.Write(stream);

            stream.Flush();
        }

        private void RaiseEventOnUIThread(Delegate theEvent, object[] args)
        {
            foreach (Delegate d in theEvent.GetInvocationList())
            {
                ISynchronizeInvoke syncer = d.Target as ISynchronizeInvoke;
                if (syncer == null)
                {
                    d.DynamicInvoke(args);
                }
                else
                {
                    syncer.BeginInvoke(d, args);  // cleanup omitted
                }
            }
        }
        public static void WriteDebugLine(object o)
        {
#if WINDOWS
            Console.WriteLine(o);
#endif
#if DEBUG
            System.Diagnostics.Debug.WriteLine(o);
#endif
        }
    }
}
