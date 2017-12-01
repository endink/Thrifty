using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class VoidThriftCodec : IThriftCodec<Object>
    {
        public ThriftType Type { get { return ThriftType.Void; } }

        public object Read(TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));
            return null;
        }

        public object ReadObject(TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));
            return null;
        }

        public void Write(object value, TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));
        }

        public void WriteObject(object value, TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));
        }
    }
}
