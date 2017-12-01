using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class FieldExtractor : Extractor
    {
        public FieldExtractor(PropertyInfo field, ThriftFieldAttribute annotation, FieldKind fieldKind)
            : base(annotation, fieldKind)
        {
            Guard.ArgumentNotNull(field, nameof(field));

            this.CSharpType = field.PropertyType;
            this.Field = field;
        }

        public PropertyInfo Field { get; }

        public override Type CSharpType { get; }
        
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("FieldExtractor");
            sb.Append("{field=").Append(this.Field.Name);
            sb.Append('}');
            return sb.ToString();
        }

        public override string ExtractName()
        {
            return this.Field.Name;
        }
    }
}
