using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;

namespace Thrifty.Codecs.Metadata
{
    public class MethodExtractor : Extractor
    {

        public MethodExtractor(Type thriftStructType, MethodInfo method, ThriftFieldAttribute annotation, FieldKind fieldKind)
                : base(annotation, fieldKind)
        {
            this.CSharpType = thriftStructType;
            this.Method = method;
        }

        public MethodInfo Method { get; }

        public override Type CSharpType { get; }



        public override String ExtractName()
        {
            return this.Method.Name;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("MethodExtractor");
            sb.Append("{method=").Append(Method.Name);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
