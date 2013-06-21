#region C#raft License
// This file is part of C#raft. Copyright C#raft Team 
// 
// C#raft is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if WINDOWS
using System.Net.Sockets;
#elif NETFX_CORE
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading.Tasks;
#endif
using System.IO;
using System.IO.Compression;

namespace Chraft.Net
{
#if WINDOWS
    public class BigEndianStream : Stream
    {
        public Stream FileStream { get; private set; }
            public BigEndianStream(Stream stream)
        {
            FileStream = stream;
        }
        public override bool CanRead { get { return FileStream.CanRead; } }
        public override bool CanSeek { get { return FileStream.CanSeek; } }
        public override bool CanWrite { get { return FileStream.CanWrite; } }
        public override long Length { get { return FileStream.Length; } }
        public override long Position { get { return FileStream.Position; } set { FileStream.Position = value; } }

        public new byte ReadByte()
        {
            int b = FileStream.ReadByte();
            if (b >= byte.MinValue && b <= byte.MaxValue)
                return (byte)b;
            throw new EndOfStreamException();
        }

        public byte[] ReadBytes(int Count)
        {
            byte[] Input = new byte[Count];

            for (int i = Count - 1; i >= 0; i--)
            {
                Input[i] = ReadByte();
            }

            return (Input);
        }

        public byte[] ReadBytesReversed(int Count)
        {
            byte[] Input = new byte[Count];

            for (int i = 0; i < Count; i++)
            {
                Input[i] = ReadByte();
            }

            return (Input);
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadByte());
        }

        public short ReadShort()
        {
            return unchecked((short)((ReadByte() << 8) | ReadByte()));
        }

        public ushort ReadUShort()
        {
            return unchecked((ushort)((ReadByte() << 8) | ReadByte()));
        }

        public int ReadInt()
        {
            return unchecked((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public uint ReadUInt()
        {
            return unchecked((uint)((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte()));
        }

        public long ReadLong()
        {
            return unchecked((ReadByte() << 56) | (ReadByte() << 48) | (ReadByte() << 40) | (ReadByte() << 32)
                | (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public ulong ReadULong()
        {
            return unchecked((ulong)((ReadByte() << 56) | (ReadByte() << 48) | (ReadByte() << 40) | (ReadByte() << 32)
                | (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte()));
        }

        public unsafe float ReadFloat()
        {
            int i = ReadInt();
            return *(float*)&i;
        }

        public unsafe double ReadDouble()
        {
            byte[] r = new byte[8];
            for (int i = 7; i >= 0; i--)
            {
                r[i] = ReadByte();
            }
            return BitConverter.ToDouble(r, 0);
        }

        public string ReadString16(short maxLen)
        {
            int len = ReadShort();
            if (len > maxLen)
                throw new IOException("String field too long");
            byte[] b = new byte[len * 2];
            for (int i = 0; i < len * 2; i++)
                b[i] = (byte)ReadByte();
            return Encoding.BigEndianUnicode.GetString(b, 0, b.Length);
        }

        public string ReadString8(short maxLen)
        {
            int len = ReadShort();
            if (len > maxLen)
                throw new IOException("String field too long");
            byte[] b = new byte[len];
            for (int i = 0; i < len; i++)
                b[i] = (byte)ReadByte();
            return Encoding.UTF8.GetString(b, 0, b.Length);
        }

        public bool ReadBool()
        {
            return ReadByte() == 1;
        }

        public void Write(byte data)
        {
            FileStream.WriteByte(data);
        }

        public void Write(sbyte data)
        {
            Write(unchecked((byte)data));
        }

        public void Write(short data)
        {
            Write(unchecked((byte)(data >> 8)));
            Write(unchecked((byte)data));
        }

        public void Write(ushort data)
        {
            Write(unchecked((byte)(data >> 8)));
            Write(unchecked((byte)data));
        }

        public void Write(int data)
        {
            Write(unchecked((byte)(data >> 24)));
            Write(unchecked((byte)(data >> 16)));
            Write(unchecked((byte)(data >> 8)));
            Write(unchecked((byte)data));
        }

        public void Write(uint data)
        {
            Write(unchecked((byte)(data >> 24)));
            Write(unchecked((byte)(data >> 16)));
            Write(unchecked((byte)(data >> 8)));
            Write(unchecked((byte)data));
        }

        public void Write(long data)
        {
            Write(unchecked((byte)(data >> 56)));
            Write(unchecked((byte)(data >> 48)));
            Write(unchecked((byte)(data >> 40)));
            Write(unchecked((byte)(data >> 32)));
            Write(unchecked((byte)(data >> 24)));
            Write(unchecked((byte)(data >> 16)));
            Write(unchecked((byte)(data >> 8)));
            Write(unchecked((byte)data));
        }

        public void Write(ulong data)
        {
            Write(unchecked((byte)(data >> 56)));
            Write(unchecked((byte)(data >> 48)));
            Write(unchecked((byte)(data >> 40)));
            Write(unchecked((byte)(data >> 32)));
            Write(unchecked((byte)(data >> 24)));
            Write(unchecked((byte)(data >> 16)));
            Write(unchecked((byte)(data >> 8)));
            Write(unchecked((byte)data));
        }

        public unsafe void Write(float data)
        {
            Write(*(int*)&data);
        }

        public unsafe void Write(double data)
        {
            Write(*(long*)&data);
        }

        public void Write(string data)
        {
            byte[] b = Encoding.BigEndianUnicode.GetBytes(data);
            Write((short)data.Length);
            Write(b, 0, b.Length);
        }

        public void Write8(string data)
        {
            byte[] b = Encoding.UTF8.GetBytes(data);
            Write((short)b.Length);
            Write(b, 0, b.Length);
        }

        public void Write(bool data)
        {
            Write((byte)(data ? 1 : 0));
        }

        public override void Flush()
        {
            FileStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return FileStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return FileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            FileStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            FileStream.Write(buffer, offset, count);
        }

        public double ReadDoublePacked()
        {
            return (double)ReadInt() / 32.0;
        }

        public void WriteDoublePacked(double d)
        {
            Write((int)(d * 32.0));
        }
    }
#elif NETFX_CORE
    public class BigEndianStream
    {
        public DataReader Reader { get; private set; }
        public DataWriter Writer { get; private set; }
        public StreamSocket Socket { get; private set; }
        public BigEndianStream(StreamSocket HostClient)
        {
            Reader = new DataReader(HostClient.InputStream);
            Reader.InputStreamOptions = InputStreamOptions.Partial;
            Writer = new DataWriter(HostClient.OutputStream);
            Socket = HostClient;
            Reader.ByteOrder = ByteOrder.BigEndian;
            Writer.ByteOrder = ByteOrder.BigEndian;
        }


        public void WriteByte(byte b)
        {
            Writer.WriteByte(b);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(byte o)
        {
            Writer.WriteByte(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(ushort o)
        {
            Writer.WriteUInt16(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(uint o)
        {
            Writer.WriteUInt32(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(ulong o)
        {
            Writer.WriteUInt64(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(bool o)
        {
            Writer.WriteBoolean(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(double o)
        {
            Writer.WriteDouble(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(short o)
        {
            Writer.WriteInt16(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(int o)
        {
            Writer.WriteInt32(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public void Write(long o)
        {
            Writer.WriteInt64(o);
            Writer.StoreAsync().AsTask().Wait();
        }

        public byte ReadByte()
        {
            LoadBytes(1);
            return Reader.ReadByte();
        }

        public ushort ReadUShort()
        {
            LoadBytes(2);
            return Reader.ReadUInt16();
        }

        public uint ReadUInt()
        {
            LoadBytes(4);
            return Reader.ReadUInt32();
        }

        public ulong ReadULong()
        {
            LoadBytes(8);
            return Reader.ReadUInt64();
        }

        public short ReadShort()
        {
            LoadBytes(2);
            return Reader.ReadInt16();
        }

        public int ReadInt()
        {
            LoadBytes(4);
            return Reader.ReadInt32();
        }

        public long ReadLong()
        {
            LoadBytes(8);
            return Reader.ReadInt64();
        }
        private void LoadBytes(uint Count)
        {
            uint left = Count;
            while (left > 0)
            {
                Task<uint> tk = Reader.LoadAsync(left).AsTask<uint>();
                tk.Wait();
                left -= tk.Result;
            }
        }

        public byte[] ReadBytesReversed(uint Count)
        {
            byte[] val = new byte[Count];
            LoadBytes(Count);
            Reader.ReadBytes(val);
            return val;
        }


        public void Flush()
        {
            Socket.OutputStream.FlushAsync().AsTask().Wait();
        }
        public void Write(byte[] Buffer, int Offset, int Len)
        {
            if (Offset != 0 || Len != Buffer.Length) throw new ArgumentException("Can only write whole byte array");
            Writer.WriteBytes(Buffer);
            Writer.StoreAsync().AsTask().Wait();
            //return Buffer.Length;
        }

        public void Read(byte[] Buffer, int Offset, int Len)
        {
            if (Offset != 0 || Len != Buffer.Length) throw new ArgumentException("Can only read whole byte array");
            LoadBytes((uint)Len);
            for (int i = 0; i < Len; i++)
            {
                Buffer[i] = Reader.ReadByte();
            }
        }

        public bool CanRead { get { return Reader != null; } }

        internal void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            Socket.Dispose();
        }

        public bool CanWrite { get { return Writer != null; } }
    }
#endif
}