#if NETFX_CORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SharpIMPP.WinRT.Net
{
    public class IPAddress  // assumes IPv4 currently
    {
        public string IP_String;
        public IPAddress() { }
        public IPAddress(string IP) { IP_String = IP; }
        public static IPAddress Broadcast { get { return new IPAddress("255.255.255.255"); } }
        public static IPAddress Parse(string IP) { return new IPAddress(IP); }
        public static bool TryParse(string V, out IPAddress Addr)
        {
            try
            {
                Addr = IPAddress.Parse(V);
                return true;
            }
            catch { Addr = null; return false; }
        }
        public HostName GetHostNameObject() { return new HostName(IP_String); }
        public byte[] GetAddressBytes()
        {
            string[] Fields = IP_String.Split('.');
            byte[] temp = new byte[4];
            for (int i = 0; i < temp.Length; i++) temp[i] = byte.Parse(Fields[i]);
            return temp;
        }
    }

    public class IPEndPoint
    {
        public IPAddress Address;
        public int Port;
        public IPEndPoint() { }
        public IPEndPoint(IPAddress Addr, int PortNum)
        {
            Address = Addr; Port = PortNum;
        }
    }

    public class NetworkStream
    {
        private DataReader Reader;
        private DataWriter Writer;

        public void Set(StreamSocket HostClient)
        {
            Reader = new DataReader(HostClient.InputStream);
            Reader.InputStreamOptions = InputStreamOptions.Partial;
            Writer = new DataWriter(HostClient.OutputStream);
        }

        public int Write(byte[] Buffer, int Offset, int Len)
        {
            if (Offset != 0 || Len != Buffer.Length) throw new ArgumentException("Can only write whole byte array");
            Writer.WriteBytes(Buffer);
            Task Tk = Writer.StoreAsync().AsTask();
            Tk.Wait();
            return Buffer.Length;
        }

        public int Read(byte[] Buffer, int Offset, int Len)
        {
            if (Offset != 0 || Len != Buffer.Length) throw new ArgumentException("Can only read whole byte array");
            Task<uint> Tk = Reader.LoadAsync((uint)Len).AsTask<uint>();
            Tk.Wait();
            uint Count = Tk.Result;
            for (int i = 0; i < Count; i++)
            {
                Buffer[i] = Reader.ReadByte();
            }
            return (int)Count;
        }

        public bool DataAvailable
        {
            get
            {
                return true; // Read() will still work if no data; could we do read ahead 1 byte to determine?
            }
        }
    }


    public class TcpClient
    {
        private StreamSocket sock;

        public TcpClient()
        {

        }

        public TcpClient(string hostname, int port)
        {

            IPEndPoint ipe = new IPEndPoint(new IPAddress(""));
            this.Connect(ipe);
        }

        public void Connect(IPEndPoint EndPt)
        {
            try
            {
                sock = new StreamSocket();
                HostName Hst = EndPt.Address.GetHostNameObject();
                Task Tsk = sock.ConnectAsync(Hst, EndPt.Port.ToString()).AsTask();
                Tsk.Wait();
            }
            catch (Exception ex) { MetroHelpers.UnpeelAggregate(ex); }
        }

        public void Close()
        {
            sock.Dispose();
            sock = null;
        }

        public NetworkStream GetStream()
        {
            var N = new NetworkStream();
            N.Set(sock);
            return N;
        }
    }

    public static class MetroHelpers
    {
        public static void UnpeelAggregate(Exception Ex)
        {
            AggregateException Ag_Ex = Ex as AggregateException;
            if (Ag_Ex == null) throw Ex;
            if (Ag_Ex.InnerExceptions.Count > 0)
            {
                if (Ag_Ex.InnerExceptions.Count == 1) throw Ag_Ex.InnerExceptions[0];
                StringBuilder Str = new StringBuilder();
                foreach (Exception X in Ag_Ex.InnerExceptions)
                {
                    Str.AppendLine(X.Message);
                }
                throw new Exception(Str.ToString(), Ag_Ex);
            }
            else throw Ag_Ex;
        }
    }
} // END NAMESPACE
#endif