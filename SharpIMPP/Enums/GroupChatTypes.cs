using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    class GroupChatTypes
    {
        enum TFamily
        {
            GROUP_CHATS = 0x0007
        };

        enum TType
        {
            SET = 0x0001,
            GET = 0x0002,
            MEMBER_ADD = 0x0003,
            MEMBER_REMOVE = 0x0004,
            MESSAGE_SEND = 0x0005
        };

        enum TErrorcode
        {
            MEMBER_NOT_CONTACT = 0x8001,
            MEMBER_ALREADY_EXISTS = 0x8002
        };

        enum TTupleType
        {
            ERRORCODE = 0x0000,
            FROM = 0x0001,
            NAME = 0x0002,
            MEMBER = 0x0003,
            INITIAL = 0x0004,
            MESSAGE = 0x0005,
            TIMESTAMP = 0x0006,
            GROUP_CHAT_TUPLE = 0x0007
        };

    }
}
