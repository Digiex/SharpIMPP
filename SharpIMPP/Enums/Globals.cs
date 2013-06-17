using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    public class Globals
    {
        /* Message flags.
         */

        public const ushort MF_REQUEST = 0x0000;
        public const ushort MF_RESPONSE = 0x0001;
        public const ushort MF_INDICATION = 0x0002;
        public const ushort MF_ERROR = 0x0004;
        public const ushort MF_EXTENSION = 0x0008;

        /* Global error codes.
         */

        public enum TErrorcode
        {
            SUCCESS = 0x0000,
            SERVICE_UNAVAILABLE = 0x0001,
            INVALID_CONNECTION = 0x0002,
            INVALID_STATE = 0x0003,
            INVALID_TLV_FAMILY = 0x0004,
            INVALID_TLV_LENGTH = 0x0005,
            INVALID_TLV_VALUE = 0x0006
        };

        /* User statuses.
        */

        public const ushort USER_STATUS_OFFLINE = 0;
        public const ushort USER_STATUS_ONLINE = 1;
        public const ushort USER_STATUS_AWAY = 2;

        /* DND doesn't work yet.
         */

        public const ushort USER_STATUS_DND = 3;
        public const ushort USER_STATUS_INVISIBLE = 4;

        /*
         * Mobile can't be set by clients; the server exports it automatically when a
         * mobile device is considered "most available".
         */

        public const ushort USER_STATUS_MOBILE = 5;


    }
}
