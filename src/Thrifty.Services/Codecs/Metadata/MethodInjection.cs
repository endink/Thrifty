using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class MethodInjection
    {
        private IEnumerable<ParameterInjection> parameters;

        public MethodInjection(MethodInfo method, IEnumerable<ParameterInjection> parameters)
        {
            this.Method = method;
            this.parameters = parameters;
        }

        public MethodInfo Method { get; }

        public IEnumerable<ParameterInjection> getParameters()
        {
            return parameters;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("MethodInjection");
            sb.Append("{method=").Append(String.Format($"{Method.DeclaringType}.{Method.Name}"));
            sb.Append(", parameters=").Append(String.Join(", ", parameters));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
