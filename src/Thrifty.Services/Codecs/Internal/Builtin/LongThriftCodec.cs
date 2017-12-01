using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class LongThriftCodec : AbstractThriftCodec<long>
    {
        public override ThriftType Type { get { return ThriftType.I64; } }

        protected override long OnRead(TProtocolReader reader)
        {
            return reader.ReadI64();
        }

        protected override void OnWrite(long value, TProtocolWriter writer)
        {
            writer.WriteI64(value);
        }
    }
}
