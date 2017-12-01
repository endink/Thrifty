using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    /// <summary>
    /// 标识一个 Thrift 服务中的方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ThriftMethodAttribute : Attribute
    {
        public ThriftMethodAttribute(String name = null)
        {
            this.Name = name;
        }

        public String Name { get; }

        /// <summary>
        /// 获取或设置一个值，指示服务方法是否是单向（one-way）的。
        /// </summary>
        public bool OneWay { get; set; }
    }
}
