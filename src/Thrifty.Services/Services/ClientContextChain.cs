using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public class ClientContextChain
    {
        private readonly String _methodName;
        private readonly IEnumerable<KeyValuePair<ThriftClientEventHandler, Object>> _contexts;

        internal ClientContextChain(IEnumerable<ThriftClientEventHandler> handlers, String methodName, IClientRequestContext requestContext)
        {
            handlers = handlers ?? Enumerable.Empty<ThriftClientEventHandler>();
            this._methodName = methodName;
            this._contexts = handlers.ToDictionary(h => h, h=>h.GetContext(methodName, requestContext)).ToArray();
        }

        public void PreWrite(Object[] args)
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.PreWrite(entry.Value, _methodName, args);
            }
        }

        public void PostWrite(Object[] args)
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.PostWrite(entry.Value, _methodName, args);
            }
        }

        public void PreRead()
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.PreRead(entry.Value, _methodName);
            }
        }

        public void PreReadException(Exception t)
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.PostReadException(entry.Value, _methodName, t);
            }
        }

        public void PostRead(Object result)
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.PostRead(entry.Value, _methodName, result);
            }
        }

        public void PostReadException(Exception t)
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.PostReadException(entry.Value, _methodName, t);
            }
        }

        public void Done()
        {
            foreach (var entry in this._contexts)
            {
                entry.Key.Done(entry.Value, _methodName);
            }
        }
    }
}
