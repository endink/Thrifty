using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public abstract class Injection : FieldMetadata
    {
        protected Injection(ThriftFieldAttribute annotation, FieldKind fieldKind)
            :base(annotation, fieldKind)
        {
        }
    }
}
