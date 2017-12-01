using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftConstructorInjection
    {
        public ThriftConstructorInjection(ConstructorInfo constructor, params ThriftParameterInjection[] parameters)
            :this(constructor, (IEnumerable<ThriftParameterInjection>)parameters)
        {
            
        }

        public ThriftConstructorInjection(ConstructorInfo constructor, IEnumerable<ThriftParameterInjection> parameters)
        {
            Guard.ArgumentNotNull(constructor, nameof(constructor));

            this.Constructor = constructor;
            this.Parameters = parameters ?? Enumerable.Empty<ThriftParameterInjection>();
        }

        public ConstructorInfo Constructor { get; }

        public IEnumerable<ThriftParameterInjection> Parameters { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.Constructor.Name);
            sb.Append('(');
            sb.Append(String.Join(", ", Parameters.Select(p => p.CSharpType.Name)));
            sb.Append(')');
            return sb.ToString();
        }
    }
}
