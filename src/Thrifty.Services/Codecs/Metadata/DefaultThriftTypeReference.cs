using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class DefaultThriftTypeReference : IThriftTypeReference
    {
        private ThriftType thriftType;

        public DefaultThriftTypeReference(ThriftType thriftType)
        {
            this.thriftType = thriftType;
        }

        public Type CSharpType
        {
            get { return this.thriftType.CSharpType; }
        }

        public ThriftProtocolType ProtocolType
        {
            get { return this.thriftType.ProtocolType; }
        }

        public bool Recursive
        {
            get { return false; }
        }

        public ThriftType Get()
        {
            return this.thriftType;
        }

        public override int GetHashCode()
        {
            return thriftType.GetHashCode();
        }
        

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj == null || !obj.GetType().Equals(typeof(DefaultThriftTypeReference)))
            {
                return false;
            }

            DefaultThriftTypeReference that = (DefaultThriftTypeReference)obj;

            return this.thriftType.Equals(that.thriftType);
        }
    }
}
