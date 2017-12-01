using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftStruct]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class StructWithConstructor
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        [ThriftField(1)]
        public String StringProperty { get; }

        [ThriftField(2)]
        public int IntProperty { get; set; }

        [ThriftConstructor]
        public StructWithConstructor([ThriftField(1)]String a, [ThriftField(2)]int b)
        {
            this.StringProperty = a;
            this.IntProperty = b;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            foreach (var property in this.GetType().GetTypeInfo().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
            {
                object v1 = property.GetValue(this);
                object v2 = property.GetValue(obj);
                if (!Object.Equals(v1, v2))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
