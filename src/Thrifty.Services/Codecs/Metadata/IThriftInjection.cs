using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    /// <summary>
    /// ThriftInjection contains information an injection point for a single thrift field.
    /// Implementation of this interface are expected to be thread safe.
    /// </summary>
    public interface IThriftInjection
    {
        short Id { get; }

        String Name { get; }

        FieldKind FieldKind { get; }
    }
}
