using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftMethodExtractor : IThriftExtraction
    {
        public ThriftMethodExtractor(
                short fieldId, String fieldName, FieldKind fieldKind, MethodInfo method, Type fieldType)
        {
            Guard.ArgumentNullOrWhiteSpaceString(fieldName, nameof(fieldName));
            Guard.ArgumentNotNull(method, nameof(method));
            Guard.ArgumentNotNull(fieldType, nameof(fieldType));

            //    this.name = checkNotNull(fieldName, "name is null");
            //this.method = checkNotNull(method, "method is null");
            //this.fieldKind = checkNotNull(fieldKind, "fieldKind is null");
            this.Name = fieldName;
            this.Method = method;
            this.FieldKind = fieldKind;

            this.Type = fieldType;
            //不支持 Union
            //switch (fieldKind)
            //{
            //    case FieldKind.ThriftField:
            //        // Nothing to check
            //        break;
            //    case FieldKind.ThriftUnionId:
            //        checkArgument(fieldId == Short.MIN_VALUE, "fieldId must be Short.MIN_VALUE for thrift_union_id");
            //        break;
            //}

            this.Id = fieldId;
        }

        public MethodInfo Method { get; }

        public Type Type { get; }

        public bool IsGeneric
        {
            get { return this.Method.ReturnType.GetTypeInfo().IsGenericType; }
        }

        public short Id { get; }

        public string Name { get; }

        public FieldKind FieldKind { get; }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftMethodExtractor");
            sb.Append("{id=").Append(this.Id);
            sb.Append(", name='").Append(this.Name).Append('\'');
            sb.Append(", fieldKind=").Append(this.FieldKind);
            sb.Append(", method=").Append(this.Method);
            sb.Append(", type=").Append(this.Type.FullName);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
