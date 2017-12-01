using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class IntArrayThriftCodec : AbstractThriftCodec<int[]>
    {
        public override ThriftType Type { get { return ThriftType.Array(ThriftType.I32); } }

        protected override int[] OnRead(TProtocolReader reader)
        {
            return reader.ReadI32Array();
        }

        protected override void OnWrite(int[] value, TProtocolWriter writer)
        {
            writer.WriteI32Array(value);
        }
    }
}
