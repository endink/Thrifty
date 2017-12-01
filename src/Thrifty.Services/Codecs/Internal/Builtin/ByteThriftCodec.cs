using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class ByteThriftCodec : AbstractThriftCodec<byte>
    {
        public override ThriftType Type { get { return ThriftType.Byte; } }

        protected override byte OnRead(TProtocolReader reader)
        {
            return reader.ReadByte();
        }

        protected override void OnWrite(byte value, TProtocolWriter writer)
        {
            writer.WriteByte(value);
        }
    }
}
