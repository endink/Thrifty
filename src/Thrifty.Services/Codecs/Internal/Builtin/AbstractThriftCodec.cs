using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal.Builtin
{
    public abstract class AbstractThriftCodec<T> : IThriftCodec<T>
    {
        public abstract ThriftType Type { get; }

        public T Read(TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));
            return this.OnRead(new TProtocolReader(protocol));
        }

        public object ReadObject(TProtocol protocol)
        {
            return this.Read(protocol);
        }

        public void Write(T value, TProtocol protocol)
        {
            Guard.ArgumentNotNull(value, nameof(value));
            Guard.ArgumentNotNull(protocol, nameof(protocol));
            this.OnWrite(value, new TProtocolWriter(protocol));
        }

        public void WriteObject(object value, TProtocol protocol)
        {
            this.Write((T)value, protocol);
        }

        protected abstract T OnRead(TProtocolReader reader);
        protected abstract void OnWrite(T value, TProtocolWriter writer);
    }
}
