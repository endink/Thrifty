using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    /// <summary>
    /// 标识一个 Thrift 结构（对应 IDL 中的 struct）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false, Inherited =false)]
    public sealed class ThriftStructAttribute : Attribute
    {
        public ThriftStructAttribute(String name = null)
        {
            this.Name = name;
        }

        /// <summary>
        /// 获取 Thrift 结构的唯一标识符号。
        /// </summary>
        public String Name { get; }
    }
}
