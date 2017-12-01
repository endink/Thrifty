using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class IntThriftCodec : AbstractThriftCodec<int>
    {
        public override ThriftType Type { get { return ThriftType.I32; } }

        protected override int OnRead(TProtocolReader reader)
        {
            return reader.ReadI32();
        }

        protected override void OnWrite(int value, TProtocolWriter writer)
        {
            writer.WriteI32(value);
        }
    }
}
