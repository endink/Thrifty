using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    /// <summary>
    /// An interface to either a resolved <see cref="ThriftType"/> or the information to compute one.
    /// Used when computing struct/union metadata, as a placeholder for field types that might not be directly resolvable yet (in cases of recursive types).
    /// </summary>
    public interface IThriftTypeReference
    {
        Type CSharpType { get; }

        ThriftProtocolType ProtocolType { get; }

        bool Recursive { get; }

        ThriftType Get();
    }
}
