using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class ShortArrayThriftCodec : AbstractThriftCodec<short[]>
    {
        public override ThriftType Type { get { return ThriftType.Array(ThriftType.I16); } }

        protected override short[] OnRead(TProtocolReader reader)
        {
            return reader.ReadI16Array();
        }

        protected override void OnWrite(short[] value, TProtocolWriter writer)
        {
            writer.WriteI16Array(value);
        }
    }
}
