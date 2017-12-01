using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftEnumMetadata
    {
        private Delegate _castDelegate = null;

        public ThriftEnumMetadata(Type enumType)
        {
            Guard.ArgumentNotNull(enumType, nameof(enumType));
            
            this.EnumType = enumType;
            if (!this.EnumType.GetTypeInfo().IsEnum)
            {
                throw new ArgumentException($"constructor of {typeof(ThriftEnumMetadata)} parameter need a enum value.");
            }
            var underlyingType = Enum.GetUnderlyingType(enumType);
            
            this.HasExplicitThriftValue = Enum.GetValues(enumType).Cast<Object>().Select(v=>Convert.ToInt64(v)).Any(v=>v<0 || v > int.MaxValue);
            
            if (this.HasExplicitThriftValue)
            {
                var p = Expression.Parameter(enumType);
                var convert = Expression.Convert(p, underlyingType);
                if (!underlyingType.Equals(typeof(int)))
                {
                    convert = Expression.Convert(convert, typeof(int));
                }
               _castDelegate = Expression.Lambda(convert, p).Compile();
            }
        }

        public int CastToNumber(object enumValue)
        {
            if (!this.HasExplicitThriftValue)
            {
                throw new ArgumentException($"Enum {this.EnumType.FullName} has not explicit ThriftValue.");
            }
            return (int)_castDelegate.DynamicInvoke(enumValue);
        }

        public Type EnumType { get; }
        
        public bool HasExplicitThriftValue { get; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftEnumMetadata");
            sb.Append("{enumClass=").Append(this.EnumType.FullName);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
