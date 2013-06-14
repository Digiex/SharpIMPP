using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    public class DeviceTypes
    {
        public enum TFamily
        {
            DEVICE = 0x0002
        };

        public enum TType
        {
            BIND = 0x0001,
            UPDATE = 0x0002,
            UNBIND = 0x0003
        };

        public enum TErrorcode
        {
            CLIENT_INVALID = 0x8001,
            DEVICE_COLLISION = 0x8002,
            TOO_MANY_DEVICES = 0x8003,
            DEVICE_BOUND_ELSEWHERE = 0x8004
        };

        public enum TTupleType
        {
            ERRORCODE = 0x0000,
            CLIENT_NAME = 0x0001,
            CLIENT_PLATFORM = 0x0002,
            CLIENT_MODEL = 0x0003,
            CLIENT_ARCH = 0x0004,
            CLIENT_VERSION = 0x0005,
            CLIENT_BUILD = 0x0006,
            CLIENT_DESCRIPTION = 0x0007,
            DEVICE_NAME = 0x0008,
            IP_ADDRESS = 0x0009,
            CONNECTED_AT = 0x000a,
            STATUS = 0x000b,
            STATUS_MESSAGE = 0x000c,
            CAPABILITIES = 0x000d,
            IS_IDLE = 0x000e,
            IS_MOBILE = 0x000f,
            IS_STATUS_AUTOMATIC = 0x0010,
            SERVER = 0x0012,
            DEVICE_TUPLE = 0x0013
        };

    }
}
