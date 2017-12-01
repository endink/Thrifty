using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class DoubleArrayThriftCodec : AbstractThriftCodec<double[]>
    {
        public override ThriftType Type { get { return ThriftType.Array(ThriftType.Double); } }
        
        protected override double[] OnRead(TProtocolReader reader)
        {
            return reader.ReadDoubleArray();
        }

        protected override void OnWrite(double[] value, TProtocolWriter writer)
        {
            writer.WriteDoubleArray(value);
        }
    }
}
