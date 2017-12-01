using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal
{
    /// <summary>
    /// Implementations of this interface are expected to be thread safe.
    /// </summary>
    public interface IThriftCodecFactory
    {
        IThriftCodec GenerateThriftTypeCodec(ThriftCodecManager codecManager, ThriftStructMetadata metadata);
    }
}
