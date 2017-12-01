using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class FloatThriftCodec : AbstractThriftCodec<float>
    {
        public override ThriftType Type { get { return ThriftType.Float; } }

        protected override float OnRead(TProtocolReader reader)
        {
            return (float)reader.ReadDouble();
        }

        protected override void OnWrite(float value, TProtocolWriter writer)
        {
            writer.WriteDouble(value);
        }
    }
}
