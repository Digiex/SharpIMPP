using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    public class PresenceTypes
    {
        public enum TFamily
        {
            PRESENCE = 0x0005
        };

        public enum TType
        {
            SET = 0x0001,
            GET = 0x0002,
            UPDATE = 0x0003
        };

        public enum TTupleType
        {
            ERRORCODE = 0x0000,
            FROM = 0x0001,
            TO = 0x0002,
            STATUS = 0x0003,
            STATUS_MESSAGE = 0x0004,
            IS_STATUS_AUTOMATIC = 0x0005,
            AVATAR_SHA1 = 0x0006,
            NICKNAME = 0x0007,
            CAPABILITIES = 0x0008
        };

    }
}
