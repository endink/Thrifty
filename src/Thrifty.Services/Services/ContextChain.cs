using Thrifty.Codecs;
using Thrifty.Nifty.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public class ContextChain
    {
        private readonly String methodName;
        private readonly IDictionary<ThriftEventHandler, Object> contexts;

        internal ContextChain(IEnumerable<ThriftEventHandler> handlers, String methodName, IRequestContext requestContext)
        {
            handlers = handlers ?? Enumerable.Empty<ThriftEventHandler>();
            this.methodName = methodName;
            this.contexts = handlers.ToDictionary(h => h, h => h.GetContext(methodName, requestContext));
        }

        public void PreRead()
        {
            foreach (var entry in contexts)
            {
                entry.Key.PreRead(entry.Value, methodName);
            }
        }

        public void PostRead(Object[] args)
        {
            foreach (var entry in contexts)
            {
                entry.Key.PostRead(entry.Value, methodName, args);
            }
        }

        public void PreWrite(Object result)
        {
            foreach (var entry in contexts)
            {
                entry.Key.PreWrite(entry.Value, methodName, result);
            }
        }

        public void PreWriteException(Exception t)
        {
            foreach (var entry in contexts)
            {
                entry.Key.PreWriteException(entry.Value, methodName, t);
            }
        }

        public void PostWrite(Object result)
        {
            foreach (var entry in contexts)
            {
                entry.Key.PostWrite(entry.Value, methodName, result);
            }
        }

        public void PostWriteException(Exception t)
        {
            foreach (var entry in contexts)
            {
                entry.Key.PostWriteException(entry.Value, methodName, t);
            }
        }

        public void DeclaredUserException(Exception t, IThriftCodec exceptionCodec)
        {
            foreach (var entry in contexts)
            {
                entry.Key.DeclaredUserException(entry.Value, methodName, t, exceptionCodec);
            }
        }

        public void UndeclaredUserException(Exception t)
        {
            foreach (var entry in contexts)
            {
                entry.Key.UndeclaredUserException(entry.Value, methodName, t);
            }
        }

        public void Done()
        {
            foreach (var entry in contexts)
            {
                entry.Key.Done(entry.Value, methodName);
            }
        }
    }
}
