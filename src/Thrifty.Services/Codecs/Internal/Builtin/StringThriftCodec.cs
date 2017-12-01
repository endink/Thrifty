using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class StringThriftCodec : AbstractThriftCodec<String>
    {
        public override ThriftType Type { get { return ThriftType.String; } }

        protected override string OnRead(TProtocolReader reader)
        {
            return reader.ReadString();
        }

        protected override void OnWrite(string value, TProtocolWriter writer)
        {
            writer.WriteString(value);
        }
    }
}
