using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ConstructorInjection
    {
        public ConstructorInjection(ConstructorInfo constructor, IEnumerable<ParameterInjection> parameters)
        {
            this.Constructor = constructor;
            this.Parameters = parameters ?? Enumerable.Empty<ParameterInjection>();
        }

        public ConstructorInjection(ConstructorInfo constructor, params ParameterInjection[] parameters)
            :this(constructor, (IEnumerable<ParameterInjection>)parameters)
        {
        }

        public ConstructorInfo Constructor { get; }

        public IEnumerable<ParameterInjection> Parameters { get; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ConstructorInjection");
            sb.Append("{constructor=").Append($"{this.Constructor.Name}({String.Join(", ", this.Constructor.GetParameters().Select(p => p.ParameterType.Name).ToArray())})");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
