using Thrifty.Codecs.Internal.Builtin;
using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class DecimalThriftCodec : AbstractThriftCodec<Decimal>
    {
        public override ThriftType Type { get { return ThriftType.Decimal; } }

        protected override Decimal OnRead(TProtocolReader reader)
        {
            string v = reader.ReadString();
            Decimal dc = default(Decimal);
            Decimal.TryParse(v, out dc);
            return dc;
        }

        protected override void OnWrite(Decimal value, TProtocolWriter writer)
        {
            writer.WriteString(value.ToString());
        }
    }
}
