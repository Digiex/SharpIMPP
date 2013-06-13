using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    class AvatarTypes
    {
        public const int MAX_AVATAR_SIZE = 65535;

        enum TFamily
        {
            AVATAR = 0x0006
        };

        enum TType
        {
            SET = 0x0001,
            GET = 0x0002,
            UPLOAD = 0x0003
        };

        enum TErrorcode
        {
            AVATAR_NOT_FOUND = 0x8001
        };

        enum TTupleType
        {
            ERRORCODE = 0x0000,
            FROM = 0x0001,
            TO = 0x0002,
            AVATAR_SHA1 = 0x0003,
            DATA = 0x0004
        };

    }
}
