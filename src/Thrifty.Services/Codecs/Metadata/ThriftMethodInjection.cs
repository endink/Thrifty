using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftMethodInjection
    {
        public ThriftMethodInjection(MethodInfo method, params ThriftParameterInjection[] parameters)
            :this(method, (IEnumerable<ThriftParameterInjection>)parameters)
        {
        }

        public ThriftMethodInjection(MethodInfo method, IEnumerable<ThriftParameterInjection> parameters)
        {
            Guard.ArgumentNotNull(method, nameof(method));
            
            this.Method = method;
            this.Parameters = parameters;
        }

        public MethodInfo Method { get; }
        public IEnumerable<ThriftParameterInjection> Parameters { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.Method.Name);
            sb.Append('(');
            sb.Append(String.Join(", ", this.Parameters.Select(p => p.CSharpType.Name)));
            sb.Append(')');
            return sb.ToString();
        }
    }
}
