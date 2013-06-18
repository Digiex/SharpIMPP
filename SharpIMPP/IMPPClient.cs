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
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
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

        public event ContactStatusEvent ContactStatusChanged;
        public delegate void ContactStatusEvent(object sender, ContactStatusEventArgs e);
        public class ContactStatusEventArgs : EventArgs
        {
            public string Nick { get; set; }

            public byte Status { get; set; }

            public string StatusMessage { get; set; }

            internal TLVPacket Packet { get; set; }

            public string Username { get; set; }

            public byte[] AvatarSHA { get; set; }
        }

        public event ChatEvent ChatReceived;
        public delegate void ChatEvent(object sender, ChatEventArgs e);
        public class ChatEventArgs : EventArgs
        {
            public string From { get; set; }
            public string To { get; set; }
            public bool OfflineMessage { get; set; }
            public string Message { get; set; }

            internal TLVPacket Packet { get; set; }
        }

        public event TypingEvent ContactTyping;
        public delegate void TypingEvent(object sender, TypingEventArgs e);
        public class TypingEventArgs : EventArgs
        {
            public string From { get; set; }
            public bool IsTyping { get; set; }
            internal TLVPacket Packet { get; set; }
        }

        public event AvatarEvent AvatarReceived;
        public delegate void AvatarEvent(object sender, AvatarEventArgs e);
        public class AvatarEventArgs : EventArgs
        {
            public byte[] Data { get; set; }
            internal TLVPacket Packet { get; set; }

            public string AvatarSHA { get; set; }

            public string User { get; set; }
        }

        public event ConnectedEvent Connected;
        public delegate void ConnectedEvent(object sender, EventArgs e);
        public bool IsConnected { get; set; }
        #endregion

        private BigEndianStream stream;

        private string username;

        public string DeviceName { get; private set; }

        private List<string> contactsTyping = new List<string>();
        private List<string> avatarQueue = new List<string>();

        public IMPPClient()
        {

        }

        public void Connect(string UserName, string UserDomain, string Password)
        {
#if WINDOWS
            ConnectionThread = new Thread(() => connectInThread(UserName, UserDomain, Password));
            ConnectionThread.Start();
#elif NETFX_CORE
            Task.Run(() => connectInThread(UserName, UserDomain, Password));
#endif
        }

        private void connectInThread(string UserName, string UserDomain, string Password, string IP = null)
        {
            IsConnected = false;
            username = UserName;
#if WINDOWS
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
#elif NETFX_CORE
            //TODO: Fix this
            var srvRec = new DnsSRV.SRVRecord("impp.trillian.im", 0, 0, 3158);
            var sock = new StreamSocket();
            HostName host;
            if (IP == null)
            {
                host = new HostName(srvRec.NameTarget);
            }
            else
            {
                host = new HostName(IP);
            }
            sock.ConnectAsync(host, srvRec.Port.ToString()).AsTask().Wait();
            stream = new BigEndianStream(sock);
#endif

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
#if WINDOWS
                SslStream ss = new SslStream(stream);
                ss.AuthenticateAsClient(srvRec.NameTarget);
                stream = new BigEndianStream(ss);
#elif NETFX_CORE
                sock.UpgradeToSslAsync(SocketProtectionLevel.Ssl, new HostName(srvRec.NameTarget)).AsTask().Wait();
#endif
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
            tp.Write(stream);

            stream.Flush();

            tp.Read(stream);
            WriteDebugLine("Authenticate: " + tp);

            tp = new TLVPacket();
            tp.MessageType = (ushort)DeviceTypes.TType.BIND;
            tp.MessageFamily = (ushort)DeviceTypes.TFamily.DEVICE;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            string os = "Windows";
            string arch = "Unknown";
            string machine = "Unknown";
#if WINDOWS
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    os = "Mac OS X";
                    break;
                case PlatformID.Unix:
                    os = "Unix";
                    break;
            }
            arch = Environment.Is64BitOperatingSystem ? "amd64" : "i386";
            machine = Environment.MachineName;
#elif NETFX_CORE
            os = "WinRT";
            switch (System.CPU.NativeInfo.ProcessorArchitecture)
            {
                case ProcessorArchitecture.INTEL:
                    arch = "i386";
                    break;
                case ProcessorArchitecture.IA32_ON_WIN64:
                    arch = "amd64";
                    break;
                default:
                    arch = System.CPU.NativeInfo.ProcessorArchitecture.ToString().ToLower();
                    break;
            }
            var hostnames = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();
            foreach (var hostname in hostnames)
            {
                WriteDebugLine("Machine: " + hostname.RawName);
                if (!hostname.RawName.Contains(".") && !hostname.RawName.Contains(":"))
                {
                    machine = hostname.RawName;
                }
            }
#endif
            tp.Block = new TLV[] {
                new StringTLV() { TLVType = 0x0001, Value = "SharpIMPP" } , //Client name
                new StringTLV() { TLVType = 0x0002, Value = os } , //OS Name
                new StringTLV() { TLVType = 0x0004, Value = arch } , //Processor architecture
                new TLV() { TLVType = 0x0005, Value = new byte[] { 0x00, 0x01 } } , //Client version
                new TLV() { TLVType = 0x0006, Value = new byte[] { 0x00, 0x01 } } , //Build Number
                new StringTLV() { TLVType = 0x0008, Value = machine } , //Machine name
                new TLV() { TLVType = 0x000b, Value = new byte[] { 0x00, 0x01 } } , //Status
                new TLV() { TLVType = 0x0010, Value = new byte[] { 0x01 } } , //IS_STATUS_AUTOMATIC
                new TLV() { TLVType = 0x000d, Value = new byte[] { 0x00, 0x01, 0x00, 0x02 } } , //Capabilities
                new StringTLV() { TLVType = 0x0007, Value = "SharpIMPP/"+os+"-"+arch+" 1.0.0.1" } , //Description
            };
            tp.Write(stream);

            stream.Flush();

            tp.Read(stream);
            WriteDebugLine("Device bind: " + tp);
            bool reconnect = false;
            foreach (TLV t in tp.Block)
            {
                if (t.TLVType == 0)
                {
                    if (t.Value.Length == 2 && t.Value[0] == 128 && t.Value[1] == 4)
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
                    this.DeviceName = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                }
                else if (t.TLVType == (ushort)DeviceTypes.TTupleType.SERVER)
                {
                    IP = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                }
            }
            if (reconnect && IP != null)
            {
                WriteDebugLine("Reconnecting to " + IP);
#if WINDOWS
                tcpClient.GetStream().Dispose();
#elif NETFX_CORE
                sock.Dispose();
#endif
                connectInThread(UserName, UserDomain, Password, IP);
                return;
            }

            Password = null; //We don't need this anymore!

            tp = new TLVPacket();
            tp.MessageType = (ushort)ListTypes.TType.GET;
            tp.MessageFamily = (ushort)ListTypes.TFamily.LISTS;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[] { };
            tp.Write(stream);

            stream.Flush();

            tp.Read(stream);
            WriteDebugLine("Lists get: " + tp);
            if (ListReceived != null)
            {
                List<ContactListItem> contacts = new List<ContactListItem>();
                foreach (TLV t in tp.Block)
                {
                    contacts.Add(new ContactListItem() { ContactType = (ListTypes.TTupleType)t.TLVType, ContactName = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length) });
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

            stream.Flush();

            tp.Read(stream);
            WriteDebugLine("Group chats: " + tp);

            tp = new TLVPacket();
            tp.MessageType = (ushort)IMTypes.TType.OFFLINE_MESSAGES_GET;
            tp.MessageFamily = (ushort)IMTypes.TFamily.IM;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Write(stream);

            stream.Flush();

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
                    WriteDebugLine("Offline message: " + Encoding.UTF8.GetString(t.Value, 0, t.Value.Length));
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

            stream.Flush();

            tp.Read(stream);
            WriteDebugLine("Deleted offline IMs: " + tp);


            tp = new TLVPacket();
            tp.MessageType = (ushort)PresenceTypes.TType.GET;
            tp.MessageFamily = (ushort)PresenceTypes.TFamily.PRESENCE;
            tp.Flags = Globals.MF_REQUEST;
            tp.SequenceNumber = SeqNum;
            tp.Block = new TLV[0];
            tp.Write(stream);

            stream.Flush();

            //tp.Read(stream);
            //WriteDebugLine("Presence set: " + tp);

#if WINDOWS
            Thread pingThread = new Thread(new ThreadStart(doPing));
            pingThread.Start();
#elif NETFX_CORE
            //Task.Run(() => { doPing(); });
#endif

            IsConnected = true;
            if (Connected != null)
            {
                RaiseEventOnUIThread(this.Connected, new object[] { this, new EventArgs() });
            }
            while (stream.CanRead)
            {
                tp = new TLVPacket();
                WriteDebugLine("Start byte: " + stream.ReadByte());
                WriteDebugLine("Channel byte: " + stream.ReadByte());
                tp.Read(stream, false);
                bool error = false;
                foreach (TLV t in tp.Block)
                {
                    if (t.TLVType == 0)
                    {
                        error = true;
                    }
                }
                if (error)
                {
                    WriteDebugLine("Error: " + tp);
                }
                else
                    if (tp.MessageType == (ushort)PresenceTypes.TType.UPDATE && tp.MessageFamily == (ushort)PresenceTypes.TFamily.PRESENCE)
                    {
                        String str = "User:";
                        ContactStatusEventArgs csea = new ContactStatusEventArgs();
                        csea.Packet = tp;
                        foreach (TLV t in tp.Block)
                        {
                            if (t.TLVType == (ushort)PresenceTypes.TTupleType.NICKNAME)
                            {
                                csea.Nick = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                                str += " Nick: " + csea.Nick;
                            }
                            else if (t.TLVType == (ushort)PresenceTypes.TTupleType.FROM)
                            {
                                csea.Username = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                                str += " Username: " + csea.Username;
                            }
                            else if (t.TLVType == 16385)
                            {
                                str += " Email: " + Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                            }
                            else if (t.TLVType == (ushort)PresenceTypes.TTupleType.STATUS)
                            {
                                csea.Status = t.Value.Last();
                                str += " Status: " + csea.Status;
                            }
                            else if (t.TLVType == (ushort)PresenceTypes.TTupleType.STATUS_MESSAGE)
                            {
                                csea.StatusMessage = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                                str += " Message: " + csea.StatusMessage;
                            }
                            else if (t.TLVType == (ushort)PresenceTypes.TTupleType.AVATAR_SHA1)
                            {
                                csea.AvatarSHA = t.Value;
                            }
                        }
                        if (ContactStatusChanged != null)
                        {
                            RaiseEventOnUIThread(this.ContactStatusChanged, new object[] { this, csea });
                        }
                        WriteDebugLine(str);
                    }
                    else if (tp.MessageFamily == (ushort)StreamTypes.TFamily.STREAM && tp.MessageType == (ushort)StreamTypes.TType.PING)
                    {
                        WriteDebugLine("Pong");
                    }
                    else if (tp.MessageFamily == (ushort)IMTypes.TFamily.IM && tp.MessageType == (ushort)IMTypes.TType.MESSAGE_SEND)
                    {
                        string from = "";
                        string to = "";
                        string msg = "";
                        bool typingevent = false;
                        foreach (TLV t in tp.Block)
                        {
                            if (t.TLVType == (ushort)IMTypes.TTupleType.FROM)
                            {
                                from = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                            }
                            else if (t.TLVType == (ushort)IMTypes.TTupleType.TO)
                            {
                                to = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                            }
                            else if (t.TLVType == (ushort)IMTypes.TTupleType.MESSAGE_CHUNK)
                            {
                                msg = Encoding.UTF8.GetString(t.Value, 0, t.Value.Length);
                            }
                            else if (t.TLVType == (ushort)IMTypes.TTupleType.CAPABILITY && t.Value[1] == 2)
                            {
                                typingevent = true;
                            }
                        }
                        if (typingevent)
                        {
                            bool isTyping = true;
                            if (contactsTyping.Contains(from))
                            {
                                isTyping = false;
                                contactsTyping.Remove(from);
                            }
                            else
                            {
                                contactsTyping.Add(from);
                            }
                            WriteDebugLine(from + (isTyping ? " started" : " ended") + " typing");
                            if (ContactTyping != null)
                            {
                                RaiseEventOnUIThread(this.ContactTyping, new object[] { this, new TypingEventArgs() { From = from, Packet = tp, IsTyping = isTyping } });
                            }
                        }
                        else
                        {
                            WriteDebugLine("Chat message from " + from + " to " + to + ": " + msg);
                            if (contactsTyping.Contains(from))
                            {
                                contactsTyping.Remove(from);
                                if (ContactTyping != null)
                                {
                                    RaiseEventOnUIThread(this.ContactTyping, new object[] { this, new TypingEventArgs() { From = from, Packet = tp, IsTyping = false } });
                                }
                            }
                            if (ChatReceived != null)
                            {
                                RaiseEventOnUIThread(this.ChatReceived, new object[] { this, new ChatEventArgs() { From = from, To = to, OfflineMessage = false, Message = msg, Packet = tp } });
                            }
                        }
                    }
                    else if (tp.MessageFamily == (ushort)AvatarTypes.TFamily.AVATAR && tp.MessageType == (ushort)AvatarTypes.TType.GET)
                    {
                        WriteDebugLine("Got avatar");
                        if (AvatarReceived != null)
                        {
                            byte[] data = new byte[0];
                            string sha = "";
                            string user = "";
                            foreach (TLV t in tp.Block)
                            {
                                if (t.TLVType == (ushort)AvatarTypes.TTupleType.DATA)
                                {
                                    data = t.Value;
                                }
                            }
                            user = avatarQueue.First();
                            avatarQueue.Remove(user);
                            RaiseEventOnUIThread(this.AvatarReceived, new object[] { this, new AvatarEventArgs() { Packet = tp, Data = data, AvatarSHA = sha, User = user } });
                        }
                    }
                    else if (!error)
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

                stream.Flush();
                WriteDebugLine("Ping");
#if WINDOWS
                Thread.Sleep(120000);
#elif NETFX_CORE
                new System.Threading.ManualResetEvent(false).WaitOne(120000);
#endif
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
            if (!IsConnected)
            {
                throw new Exception("Not connected");
            }
            if (user != null && message != null)
            {
                TLVPacket tp = new TLVPacket();
                tp.MessageType = (ushort)IMTypes.TType.MESSAGE_SEND;
                tp.MessageFamily = (ushort)IMTypes.TFamily.IM;
                tp.Flags = Globals.MF_REQUEST;
                tp.SequenceNumber = SeqNum;
                tp.Block = new TLV[] {
                new StringTLV() { TLVType = (ushort)IMTypes.TTupleType.TO, Value = user } , //To
                new StringTLV() { TLVType = (ushort)IMTypes.TTupleType.FROM, Value =  this.username } , //From
                new TLV() { TLVType = (ushort)IMTypes.TTupleType.CAPABILITY, Value = new byte[] { 0x00, 0x01 } } , //CAPABILITY
                new TLV() { TLVType = (ushort)IMTypes.TTupleType.MESSAGE_ID, Value = BitConverter.GetBytes(tp.SequenceNumber).Reverse().ToArray() } , //MESSAGE_ID
                new TLV() { TLVType = (ushort)IMTypes.TTupleType.MESSAGE_SIZE, Value = BitConverter.GetBytes(message.Length).Reverse().ToArray() } , //MESSAGE_SIZE
                new StringTLV() { TLVType = 0x0006, Value =  message } , //MESSAGE_CHUNK
                //new TLV() { TLVType = 0x0007, Value =  BitConverter.GetBytes(DateTime.UtcNow.Ticks).Reverse().ToArray() } , //CREATED_AT
            };
                tp.Write(stream);

                stream.Flush();
            }
        }

        public void RequestAvatar(byte[] avatarsha, string username)
        {
            if (!IsConnected)
            {
                throw new Exception("Not connected");
            }
            if (avatarsha != null)
            {
                TLVPacket tp = new TLVPacket();
                tp.MessageType = (ushort)AvatarTypes.TType.GET;
                tp.MessageFamily = (ushort)AvatarTypes.TFamily.AVATAR;
                tp.Flags = Globals.MF_REQUEST;
                tp.SequenceNumber = SeqNum;
                tp.Block = new TLV[] {
                new TLV() { TLVType = (ushort)AvatarTypes.TTupleType.AVATAR_SHA1, Value = avatarsha } ,
                new StringTLV() { TLVType = (ushort)AvatarTypes.TTupleType.FROM, Value = this.username } ,
                new StringTLV() { TLVType = (ushort)AvatarTypes.TTupleType.TO, Value = username } ,
            };
                tp.Write(stream);

                stream.Flush();
                avatarQueue.Add(username);
            }
        }

        private void RaiseEventOnUIThread(Delegate theEvent, object[] args)
        {
            foreach (Delegate d in theEvent.GetInvocationList())
            {
#if WINDOWS
                ISynchronizeInvoke syncer = d.Target as ISynchronizeInvoke;
                if (syncer == null)
                {
                    d.DynamicInvoke(args);
                }
                else
                {
                    syncer.BeginInvoke(d, args);  // cleanup omitted
                }
#elif NETFX_CORE
                d.DynamicInvoke(args);
#endif
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
