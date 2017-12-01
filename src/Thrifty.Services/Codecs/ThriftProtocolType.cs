using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs
{
    /// <summary>
    /// 表示一个 Thrift 协议类型的枚举。
    /// </summary>
    public enum ThriftProtocolType : byte
    {
        Unknown = 0,
        Bool = 2,
        Byte = 3,
        Double = 4,
        I16 = 6,
        I32 = 8,
        I64 = 10,
        String = 11,
        Struct = 12,
        Map = 13,
        Set = 14,
        List = 15,
        Enum = 16,
        Binary = 11 // same as STRING type
    }

    public static class ThriftProtocolTypeExtensions
    {
        public static TType ToTType(this ThriftProtocolType protocolType)
        {
            switch (protocolType)
            {
                case ThriftProtocolType.Bool:
                    return TType.Bool;
                case ThriftProtocolType.Byte:
                    return TType.Byte;
                    case ThriftProtocolType.Double:
                    return TType.Double;
                case ThriftProtocolType.I16:
                    return TType.I16;
                case ThriftProtocolType.I32:
                case ThriftProtocolType.Enum:
                    return TType.I32;
                case ThriftProtocolType.I64:
                    return TType.I64;
                case ThriftProtocolType.List:
                    return TType.List;
                case ThriftProtocolType.Map:
                    return TType.Map;
                case ThriftProtocolType.Set:
                    return TType.Set;
                case ThriftProtocolType.String:
                    return TType.String;
                case ThriftProtocolType.Struct:
                    return TType.Struct;
                //case ThriftProtocolType.Binary:
                //    return TType.String;
                case ThriftProtocolType.Unknown:
                default:
                    return TType.Stop;
            }
        }
    }
}
