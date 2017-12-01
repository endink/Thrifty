using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftParameterInjection : IThriftInjection
    {
        public ThriftParameterInjection(
                short id,
                String name,
                int parameterIndex,
                Type csharpType)
        {
            if (parameterIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parameterIndex),
                    $"{nameof(ThriftParameterInjection)} 构造函数参数 {nameof(parameterIndex)} 必须大于等于 0 。");
            }
            Guard.ArgumentNotNull(csharpType, nameof(csharpType));
            Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
            this.CSharpType = csharpType;
            this.Name = name;
            this.Id = id;
            this.ParameterIndex = parameterIndex;
        }



        public short Id { get; }

        public string Name { get; }

        public FieldKind FieldKind { get; } = FieldKind.ThriftField;

        public int ParameterIndex { get; }

        public Type CSharpType { get; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftParameterInjection");
            sb.Append("{FieldId=").Append(this.Id);
            sb.Append(", Name=").Append(this.Name);
            sb.Append(", Index=").Append(this.ParameterIndex);
            sb.Append(", CSharpType=").Append(this.CSharpType);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
