using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class FloatArrayThriftCodec : AbstractThriftCodec<float[]>
    {
        public override ThriftType Type { get { return ThriftType.Array(ThriftType.Float); } }

        protected override float[] OnRead(TProtocolReader reader)
        {
            return reader.ReadDoubleArray().Select(d => (float)d).ToArray(); ;
        }

        protected override void OnWrite(float[] value, TProtocolWriter writer)
        {
            writer.WriteDoubleArray(value.Select(f=>(double)f).ToArray());
        }
    }
}
