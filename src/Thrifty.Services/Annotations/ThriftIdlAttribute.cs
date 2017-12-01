using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    //暂时不支持 Idl 反向生成
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, 
        AllowMultiple = true, Inherited = false)]
    internal class ThriftIdlAttribute : Attribute
    {
        public String Key { get; set; } = "";
        public String Value { get; set; } = "";
    }
}
