using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property)]
    public class ThriftOrderAttribute : Attribute
    {
        public ThriftOrderAttribute(int order)
        {
            this.Order = order;
        }

        public int Order { get; }
    }
}
