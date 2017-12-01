using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class MapThriftCodec<K, V> : AbstractThriftCodec<IDictionary<K, V>>
    {
        private readonly ThriftType _thriftType;
        private readonly IThriftCodec<K> _keyCodec;
        private readonly IThriftCodec<V> _valueCodec;

        public MapThriftCodec(ThriftType type, IThriftCodec<K> keyCodec, IThriftCodec<V> valueCodec)
        {
            Guard.ArgumentNotNull(type, nameof(type));
            Guard.ArgumentNotNull(keyCodec, nameof(keyCodec));
            Guard.ArgumentNotNull(valueCodec, nameof(valueCodec));

            this._thriftType = type;
            this._keyCodec = keyCodec;
            this._valueCodec = valueCodec;
        }

        public override ThriftType Type { get { return this._thriftType; } }

        protected override IDictionary<K, V> OnRead(TProtocolReader reader)
        {
            return reader.ReadMap(_keyCodec, _valueCodec);
        }

        protected override void OnWrite(IDictionary<K, V> value, TProtocolWriter writer)
        {
            writer.WriteMap(_keyCodec, _valueCodec, value);
        }
    }
}
