using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    class ListTypes
    {
        public const int MAX_OBJECTS = 1000;

        public enum TFamily
        {
            LISTS = 0x0003
        };

        public enum TType
        {
            GET = 0x0001,
            CONTACT_ADD = 0x0002,
            CONTACT_REMOVE = 0x0003,
            CONTACT_AUTH_REQUEST = 0x0004,
            CONTACT_APPROVE = 0x0005,
            CONTACT_APPROVED = 0x0006,
            CONTACT_DENY = 0x0007,
            ALLOW_ADD = 0x0008,
            ALLOW_REMOVE = 0x0009,
            BLOCK_ADD = 0x000a,
            BLOCK_REMOVE = 0x000b
        };

        public enum TErrorcode
        {
            LIST_LIMIT_EXCEEDED = 0x8001,
            ADDRESS_EXISTS = 0x8002,
            ADDRESS_DOES_NOT_EXIST = 0x8003,
            ADDRESS_CONFLICT = 0x8004,
            ADDRESS_INVALID = 0x8005
        };

        public enum TTupleType
        {
            ERRORCODE = 0x0000,
            FROM = 0x0001,
            TO = 0x0002,
            CONTACT_ADDRESS = 0x0003,
            PENDING_ADDRESS = 0x0004,
            ALLOW_ADDRESS = 0x0005,
            BLOCK_ADDRESS = 0x0006,
            AVATAR_SHA1 = 0x0007,
            NICKNAME = 0x0008
        };

    }
}
