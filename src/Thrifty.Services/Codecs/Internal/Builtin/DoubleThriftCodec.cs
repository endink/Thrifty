using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class DoubleThriftCodec : AbstractThriftCodec<double>
    {
        public override ThriftType Type { get { return ThriftType.Double; } }

        protected override double OnRead(TProtocolReader reader)
        {
            return reader.ReadDouble();
        }

        protected override void OnWrite(double value, TProtocolWriter writer)
        {
            writer.WriteDouble(value);
        }
    }
}
