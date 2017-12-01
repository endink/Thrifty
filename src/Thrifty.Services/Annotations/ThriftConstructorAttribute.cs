using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    /// <summary>
    /// 标识一个 Thrift 结构的序列化时所使用的构造函数。
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class ThriftConstructorAttribute : Attribute
    {

    }
}
