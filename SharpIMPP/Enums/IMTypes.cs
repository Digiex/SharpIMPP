using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    class IMTypes
    {
        /* IMPP capability blocks.
 */

        public const byte CAPABILITY_IM = 0x0001;
        public const byte CAPABILITY_TYPING = 0x0002;

        enum TFamily
        {
            IM = 0x0004
        };

        enum TType
        {
            OFFLINE_MESSAGES_GET = 0x0001,
            OFFLINE_MESSAGES_DELETE = 0x0002,
            MESSAGE_SEND = 0x0003
        };

        enum TErrorcode
        {
            USERNAME_BLOCKED = 0x8001,
            USERNAME_NOT_CONTACT = 0x8002,
            INVALID_CAPABILITY = 0x8003
        };

        enum TTupleType
        {
            ERRORCODE = 0x0000,
            FROM = 0x0001,
            TO = 0x0002,
            CAPABILITY = 0x0003,
            MESSAGE_ID = 0x0004,
            MESSAGE_SIZE = 0x0005,
            MESSAGE_CHUNK = 0x0006,
            CREATED_AT = 0x0007,
            TIMESTAMP = 0x0008,
            OFFLINE_MESSAGE = 0x0009
        };


    }
}
