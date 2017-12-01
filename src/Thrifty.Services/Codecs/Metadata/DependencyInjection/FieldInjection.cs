using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class FieldInjection : Injection
    {
        public FieldInjection(PropertyInfo field, ThriftFieldAttribute annotation, FieldKind fieldKind)
             : base(annotation, fieldKind)
        {
            this.Field = field;
            this.CSharpType = field.PropertyType;
        }

        public override Type CSharpType { get; }

        public PropertyInfo Field { get; }

        public override string ExtractName()
        {
            return this.Field.Name;
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("FieldInjection");
            sb.Append("{field=").Append(this.Field.Name);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
