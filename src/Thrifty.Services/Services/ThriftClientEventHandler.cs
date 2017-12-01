using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public abstract class ThriftClientEventHandler
    {
        public virtual Object GetContext(String methodName, IClientRequestContext requestContext)
        {
            return null;
        }

        public virtual void PreWrite(Object context, String methodName, Object[] args) { }
        public virtual void PostWrite(Object context, String methodName, Object[] args) { }
        public virtual void PreRead(Object context, String methodName) { }
        public virtual void PreReadException(Object context, String methodName, Exception t) { }
        public virtual void PostRead(Object context, String methodName, Object result) { }
        public virtual void PostReadException(Object context, String methodName, Exception t) { }
        public virtual void Done(Object context, String methodName) { }
    }
}
