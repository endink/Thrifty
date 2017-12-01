using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class BooleanThriftCodec : AbstractThriftCodec<Boolean>
    {
        public override ThriftType Type
        {
            get { return ThriftType.Bool; }
        }

        protected override bool OnRead(TProtocolReader reader)
        {
            return reader.ReadBool();
        }

        protected override void OnWrite(bool value, TProtocolWriter writer)
        {
            writer.WriteBool(value);
        }
    }

}
