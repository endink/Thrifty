using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class BooleanArrayThriftCodec : AbstractThriftCodec<bool[]>
    {
        public override ThriftType Type
        {
            get { return ThriftType.Array(ThriftType.Bool); }
        }

        protected override bool[] OnRead(TProtocolReader reader)
        {
            return reader.ReadBoolArray();
        }

        protected override void OnWrite(bool[] value, TProtocolWriter writer)
        {
            writer.WriteBoolArray(value);
        }
    }
}
