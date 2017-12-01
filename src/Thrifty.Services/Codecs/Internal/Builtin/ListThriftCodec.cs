using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class ListThriftCodec<T> : AbstractThriftCodec<IEnumerable<T>>
    {
        private IThriftCodec<T> elementCodec;
        private ThriftType type;
        private bool _isArray;

        public ListThriftCodec(ThriftType type, IThriftCodec<T> elementCodec)
        {
            Guard.ArgumentNotNull(type, nameof(type));
            Guard.ArgumentNotNull(elementCodec, nameof(elementCodec));

            this.type = type;
            this.elementCodec = elementCodec;
            this._isArray = type.CSharpType.IsArray;
        }


        public override ThriftType Type
        {
            get { return this.type; }
        }

        protected override IEnumerable<T> OnRead(TProtocolReader reader)
        {
            var list = reader.ReadList(elementCodec);
            return _isArray ? list.ToArray() : list;
        }

        protected override void OnWrite(IEnumerable<T> value, TProtocolWriter writer)
        {
            writer.WriteList(elementCodec, value.ToList());
        }
    }
}
