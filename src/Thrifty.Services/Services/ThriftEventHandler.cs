using Thrifty.Codecs;
using Thrifty.Nifty.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public abstract class ThriftEventHandler
    {
        public virtual Object GetContext(String methodName, IRequestContext requestContext)
        {
            return null;
        }

        public virtual void PreRead(Object context, String methodName) { }
        public virtual void PostRead(Object context, String methodName, Object[] args) { }
        public virtual void PreWrite(Object context, String methodName, Object result) { }
        public virtual void PreWriteException(Object context, String methodName, Exception t) { }
        public virtual void PostWrite(Object context, String methodName, Object result) { }
        public virtual void PostWriteException(Object context, String methodName, Exception t) { }
        public virtual void DeclaredUserException(Object o, String methodName, Exception t, IThriftCodec exceptionCodec){ }
        public virtual void UndeclaredUserException(Object o, String methodName, Exception t) { }
        public virtual void Done(Object context, String methodName) { }
    }
}
