using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal.Coercion
{
    public class CoercionThriftCodec<T> : IThriftCodec<T>
    {
        private readonly IThriftCodec<T> _codec;
        private readonly TypeCoercion _typeCoercion;

        public CoercionThriftCodec(IThriftCodec<T> codec, TypeCoercion typeCoercion)
        {
            Guard.ArgumentNotNull(codec, nameof(codec));
            Guard.ArgumentNotNull(typeCoercion, nameof(typeCoercion));

            this._codec = codec;
            this._typeCoercion = typeCoercion;
            this.Type = typeCoercion.ThriftType;
        }
        public ThriftType Type { get; }

        public T Read(TProtocol protocol)
        {
            Object thriftValue = _codec.Read(protocol);
            T csharpValue = (T)_typeCoercion.FromThrift.DynamicInvoke(thriftValue);
            return csharpValue;
        }

        public object ReadObject(TProtocol protocol)
        {
            return this.Read(protocol);
        }

        public void Write(T value, TProtocol protocol)
        {
            Object thriftValue = _typeCoercion.ToThrift.DynamicInvoke(value);
            _codec.Write((T)thriftValue, protocol);
        }

        public void WriteObject(object value, TProtocol protocol)
        {
            Object thriftValue = _typeCoercion.ToThrift.DynamicInvoke(value);
            _codec.WriteObject(thriftValue, protocol);
        }
    }
}
