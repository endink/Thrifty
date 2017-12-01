using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class ByteBufferThriftCodec : AbstractThriftCodec<byte[]>
    {
        public override ThriftType Type { get { return ThriftType.Binary; } }

        protected override byte[] OnRead(TProtocolReader reader)
        {
            return reader.ReadBinary();
        }

        protected override void OnWrite(byte[] value, TProtocolWriter writer)
        {
            writer.WriteBinary(value);
        }
    }
}
