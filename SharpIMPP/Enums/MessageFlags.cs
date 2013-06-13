using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    public class MessageFlags
    {
        /* Message flags.
         */

        public const byte MF_REQUEST = 0x0000;
        public const byte MF_RESPONSE = 0x0001;
        public const byte MF_INDICATION = 0x0002;
        public const byte MF_ERROR = 0x0004;
        public const byte MF_EXTENSION = 0x0008;

        /* Global error codes.
         */

        enum TErrorcode
        {
            SUCCESS = 0x0000,
            SERVICE_UNAVAILABLE = 0x0001,
            INVALID_CONNECTION = 0x0002,
            INVALID_STATE = 0x0003,
            INVALID_TLV_FAMILY = 0x0004,
            INVALID_TLV_LENGTH = 0x0005,
            INVALID_TLV_VALUE = 0x0006
        };

    }
}
