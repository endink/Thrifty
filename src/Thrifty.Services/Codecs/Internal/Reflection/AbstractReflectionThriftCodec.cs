using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal.Reflection
{
    public abstract class AbstractReflectionThriftCodec<T> : IThriftCodec<T>
    {
        protected ThriftStructMetadata _metadata;
        protected SortedDictionary<short, IThriftCodec> _fields;

        protected AbstractReflectionThriftCodec(ThriftCodecManager manager, ThriftStructMetadata metadata)
        {
            Guard.ArgumentNotNull(manager, nameof(manager));
            Guard.ArgumentNotNull(metadata, nameof(metadata));

            this._metadata = metadata;
            _fields = new SortedDictionary<short, IThriftCodec>();

            foreach (ThriftFieldMetadata fieldMetadata in metadata.GetFields(FieldKind.ThriftField))
            {
                _fields.Add(fieldMetadata.Id, manager.GetCodec(fieldMetadata.ThriftType));
            }
        }

        public ThriftType Type { get { return ThriftType.Struct(this._metadata); } }

        protected Object GetFieldValue(Object instance, ThriftFieldMetadata field)
        {
            IThriftExtraction extraction;
            if (field.TryGetExtraction(out extraction))
            {
                if (extraction is ThriftFieldExtractor)
                {
                    ThriftFieldExtractor thriftFieldExtractor = (ThriftFieldExtractor)extraction;
                    return thriftFieldExtractor.Field.GetValue(instance);
                }
                else if (extraction is ThriftMethodExtractor)
                {
                    ThriftMethodExtractor thriftMethodExtractor = (ThriftMethodExtractor)extraction;
                    return thriftMethodExtractor.Method.Invoke(instance, null);
                }
                throw new ThriftyException($"Unsupported field extractor type {extraction.GetType().FullName}.");
            }
            throw new ThriftyException($"No extraction present for {field}.");
        }

        public abstract T Read(TProtocol protocol);

        public abstract void Write(T value, TProtocol protocol);

        public object ReadObject(TProtocol protocol)
        {
            return this.Read(protocol);
        }

        public void WriteObject(object value, TProtocol protocol)
        {
            if (value == null || !value.GetType().Equals(typeof(T)))
            {
                throw new ThriftyException($"IThriftCodec need a type {typeof(T).FullName} value to write.");
            }
            this.Write((T)value, protocol);
        }
    }
}
