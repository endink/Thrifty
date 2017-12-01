using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty
{
    //暂时不支持异常处理
    [AttributeUsage(AttributeTargets.Method, AllowMultiple =true, Inherited = true)]
    internal class ThriftExceptionAttribute : Attribute
    {
        public ThriftExceptionAttribute(short id, String name, Type exceptionType)
        {
            Guard.ArgumentNotNull(exceptionType, nameof(exceptionType));
            if (!typeof(Exception).GetTypeInfo().IsAssignableFrom(exceptionType))
            {
                throw new ArgumentException($"{nameof(ThriftExceptionAttribute)} 构造函数参数 {nameof(exceptionType)} 必须是一个 {nameof(Exception)} 类型。", nameof(exceptionType));
            }

            this.Id = id;
            this.Name = name;
            this.ExceptionType = exceptionType;
        }
        public Type ExceptionType { get; }

        public short Id { get; }
        public String Name { get; } = "";
    }
}
