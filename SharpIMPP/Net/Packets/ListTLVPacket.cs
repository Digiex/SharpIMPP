using SharpIMPP.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Net.Packets
{
    class ListTLVPacket : TLVPacket
    {
        new public ListTypes.TFamily MessageFamily
        {
            get
            {
                return (ListTypes.TFamily)base.MessageFamily;
            }
            set
            {
                base.MessageFamily = (ushort)value;
            }
        }
        new public ListTypes.TType MessageType
        {
            get
            {
                return (ListTypes.TType)base.MessageType;
            }
            set
            {
                base.MessageType = (ushort)value;
            }
        }
        public ListTLVPacket()
        {
            this.MessageFamily = ListTypes.TFamily.LISTS;
        }
    }
}
