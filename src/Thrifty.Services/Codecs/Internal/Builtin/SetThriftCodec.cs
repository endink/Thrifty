using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class SetThriftCodec<T> : AbstractThriftCodec<ISet<T>>
    {
        private readonly IThriftCodec<T> _elementCodec;
        private readonly ThriftType _type;

        public SetThriftCodec(ThriftType type, IThriftCodec<T> elementCodec)
        {
            Guard.ArgumentNotNull(type, nameof(type));
            Guard.ArgumentNotNull(elementCodec, nameof(elementCodec));

            this._type = type;
            this._elementCodec = elementCodec;
        }
        public override ThriftType Type { get { return _type; } }

        protected override ISet<T> OnRead(TProtocolReader reader)
        {
            return reader.ReadSet(_elementCodec);
        }

        protected override void OnWrite(ISet<T> value, TProtocolWriter writer)
        {
            writer.WriteSet(_elementCodec, value);
        }
    }
}
