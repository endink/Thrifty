using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ParameterInjection : Injection
    {
        private String extractedName;
        private Type thriftStructType;

        internal ParameterInjection(Type thriftStructType, int parameterIndex, ThriftFieldAttribute annotation, String extractedName, Type parameterType)
                : base(annotation, FieldKind.ThriftField)
        {
            Guard.ArgumentNotNull(parameterType, nameof(parameterType));
            this.CSharpType = parameterType;
            this.thriftStructType = thriftStructType;

            this.ParameterIndex = parameterIndex;
            this.extractedName = extractedName;
            if (typeof(void).Equals(parameterType))
            {
                throw new ArgumentException($"pararmeter type not allow void");
            }

            Guard.ArgumentCondition(!String.IsNullOrWhiteSpace(this.Name) || !String.IsNullOrWhiteSpace(extractedName), "Parameter must have an explicit name or an extractedName");
        }

        public int ParameterIndex { get; }

        public override String ExtractName()
        {
            return extractedName;
        }

        public override Type CSharpType { get; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ParameterInjection");
            sb.Append("{parameterIndex=").Append(this.ParameterIndex);
            sb.Append(", extractedName='").Append(extractedName).Append('\'');
            sb.Append(", parameterCSharpType=").Append(this.CSharpType);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
