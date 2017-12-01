using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    /// <summary>
    /// ThriftExtraction contains information an extraction point for a single thrift field.
    /// Implementations of this interface are expected to be thread safe.
    /// </summary>
    public interface IThriftExtraction
    {
        short Id { get; }

        String Name { get; }

        FieldKind FieldKind { get; }
    }
}
