using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftFieldExtractor : IThriftExtraction
    {

        public ThriftFieldExtractor(
                short fieldId, String fieldName, FieldKind fieldKind, PropertyInfo field, Type fieldType)
        {
            Guard.ArgumentNotNull(fieldType, nameof(fieldType));
            Guard.ArgumentNullOrWhiteSpaceString(fieldName, nameof(fieldName));
            Guard.ArgumentNotNull(field, nameof(field));

            this.Name = fieldName;
            this.Field = field;
            this.FieldKind = fieldKind;
            this.Type = field.PropertyType;

            switch (fieldKind)
            {
                case FieldKind.ThriftField:
                    // nothing to check
                    break;
                case FieldKind.ThriftUnionId:
                    if (fieldId != short.MinValue)
                    {
                        new ArgumentOutOfRangeException(nameof(fieldId), "fieldId must be short.MinValue for thrift_union_id");
                    }
                    break;
            }
            this.Id = fieldId;
        }



        public PropertyInfo Field { get; }

        public Type Type { get; }

        public short Id { get; }

        public string Name { get; }

        public FieldKind FieldKind { get; }

        public bool IsGeneric
        {
            get { return Field.PropertyType.GetTypeInfo().IsGenericType; }
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftFieldExtractor");
            sb.Append("{id=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", fieldKind=").Append(this.FieldKind);
            sb.Append(", field=").Append(this.Field.DeclaringType.Name).Append(".").Append(this.Field.Name);
            sb.Append(", type=").Append(this.Type);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
