using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    /// <summary>
    /// 标识一个 Thrift 服务。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple =false, Inherited =false)]
    public sealed class ThriftServiceAttribute : Attribute
    {
        public ThriftServiceAttribute(String name = null)
        {
            this.Name = name;
        }

        public String Name { get; }
    }
}
