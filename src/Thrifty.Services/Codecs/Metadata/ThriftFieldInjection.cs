using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftFieldInjection : IThriftInjection
    {

        public ThriftFieldInjection(short id, String name, PropertyInfo field, FieldKind fieldKind)
        {
            Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
            Guard.ArgumentNotNull(field, nameof(field));

            this.Name = name;
            this.Field = field;
            this.FieldKind = fieldKind;

            //不再支持 Union
            //switch (this.FieldKind)
            //{
            //    case FieldKind.ThriftField:
            //        // Nothing to check
            //        break;
            //    case FieldKind.ThriftUnionId:
            //        checkArgument(id == Short.MIN_VALUE, "fieldId must be Short.MIN_VALUE for thrift_union_id");
            //        break;
            //}

            this.Id = id;
        }

        public FieldKind FieldKind { get; }

        public short Id { get; }

        public string Name { get; }

        public PropertyInfo Field { get; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftFieldInjection");
            sb.Append("{fieldId=").Append(this.Id);
            sb.Append(", name=").Append(this.Name);
            sb.Append(", fieldKind=").Append(this.FieldKind);
            sb.Append(", field=").Append(this.Field.DeclaringType.FullName).Append(".").Append(this.Field.Name);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
