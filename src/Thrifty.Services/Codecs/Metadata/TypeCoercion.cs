using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class TypeCoercion
    {
        private Type _underlyingType;

        public TypeCoercion(ThriftType thriftType)
        {
            Guard.ArgumentNotNull(thriftType, nameof(thriftType));
            var typeInfo = thriftType.CSharpType.GetTypeInfo();
            if (!typeInfo.IsGenericType || !typeInfo.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                throw new ArgumentException($"only nullable type can be used by {nameof(TypeCoercion)}, actual thrift type is: {thriftType.CSharpType.FullName}");
            }
            _underlyingType = Nullable.GetUnderlyingType(thriftType.CSharpType);

            this.ThriftType = thriftType;

            var p = Expression.Parameter(thriftType.CSharpType);
            var methodName = nameof(Nullable<int>.GetValueOrDefault);
            var m = thriftType.CSharpType.GetMethod(methodName, new Type[0]);
            var call = Expression.Call(p, m);
            this.ToThrift = Expression.Lambda(call, p).Compile();
            
            var p1 = Expression.Parameter(_underlyingType, "v");
            var ctor = typeInfo.GetConstructor(new Type[] { _underlyingType });
            var newExp = Expression.New(ctor, p1);
            this.FromThrift = Expression.Lambda(newExp, p1).Compile();
        }

        public ThriftType ThriftType { get; }

        public Delegate ToThrift { get; }

        public Delegate FromThrift { get; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TypeCoercion");
            sb.Append("{ThriftType=").Append(this.ThriftType);
            sb.Append('}');
            return sb.ToString();
        }
    }
}
