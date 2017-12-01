using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thrift.Protocol;

namespace Thrifty.Nifty.Core
{
    public class NiftyRequestContext : IRequestContext
    {
        private ConcurrentDictionary<String, Object> _data;
        public TProtocol OutputProtocol { get; }

        public TProtocol InputProtocol { get; }

        public IConnectionContext ConnectionContext { get; }

        public IEnumerable<KeyValuePair<string, object>> ContextData => this._data;

        public TNiftyTransport NiftyTransport { get; }

        public Guid Id { get; }

        public NiftyRequestContext(IConnectionContext context, TProtocol inputProtocol, TProtocol outputProtocol, TNiftyTransport niftyTransport)
        {
            _data = new ConcurrentDictionary<String, Object>();
            this.ConnectionContext = context;
            this.InputProtocol = inputProtocol;
            this.OutputProtocol = outputProtocol;
            this.NiftyTransport = niftyTransport;
            this.Id = Guid.NewGuid();
        }

        public void SetContextData(string key, object val)
        {
            this._data[key] = val;
        }

        public object GetContextData(string key)
        {
            object val = null;
            this._data.TryGetValue(key, out val);
            return val;
        }

        public void ClearContextData(string key)
        {
            this._data.Clear();
        }


    }

}
