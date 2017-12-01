using Thrifty.Codecs.Internal;
using Thrifty.Codecs.Internal.Builtin;
using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class GuidThriftCodec : AbstractThriftCodec<Guid>
    {
        public override ThriftType Type { get { return ThriftType.Guid; } }

        protected override Guid OnRead(TProtocolReader reader)
        {
            string v = reader.ReadString();
            Guid id = Guid.Empty;
            Guid.TryParse(v, out id);
            return id;
        }
        
        protected override void OnWrite(Guid value, TProtocolWriter writer)
        {
            writer.WriteString(value.ToString());
        }
    }
}
