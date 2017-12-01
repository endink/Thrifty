using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;

namespace Thrifty.Codecs.Internal.Reflection
{
    public class ReflectionThriftCodecFactory : IThriftCodecFactory
    {
        public IThriftCodec GenerateThriftTypeCodec(ThriftCodecManager codecManager, ThriftStructMetadata metadata)
        {
            switch (metadata.MetadataType)
            {
                case MetadataType.Struct:
                    var type = metadata.StructType;
                    var codecType = typeof(ReflectionThriftStructCodec<>).MakeGenericType(type);
                    return (IThriftCodec)Activator.CreateInstance(codecType, codecManager, metadata);
                case MetadataType.Union:
                default:
                    throw new ThriftyException($"encountered type {metadata.MetadataType}");
            }
        }
    }
}
