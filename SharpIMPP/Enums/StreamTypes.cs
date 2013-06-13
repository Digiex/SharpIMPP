using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpIMPP.Enums
{
    public class StreamTypes
    {
        public const byte FEATURE_NONE = 0x00;
        public const byte FEATURE_TLS = 0x01;
        public const byte FEATURE_COMPRESSION = 0x02;

        public const byte MECHANISM_PASSWORD = 0x01;

        enum TFamily
        {
            STREAM = 0x0001
        };

        enum TType
        {
            FEATURES_SET = 0x0001,
            AUTHENTICATE = 0x0002,
            PING = 0x0003
        };

        enum TErrorcode
        {
            FEATURE_INVALID = 0x8001,
            MECHANISM_INVALID = 0x8002,
            AUTHENTICATION_INVALID = 0x8003
        };

        enum TTupleType
        {
            ERRORCODE = 0x0000,
            FEATURES = 0x0001,
            MECHANISM = 0x0002,
            NAME = 0x0003,
            TIMESTAMP = 0x0004
        };

    }
}
