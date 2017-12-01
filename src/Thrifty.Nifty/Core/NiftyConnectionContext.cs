using Thrifty.Nifty.Ssl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public class NiftyConnectionContext : IConnectionContext
    {
        //private SslSession _sslSession;
        private ConcurrentDictionary<String, Object> _attributes = new ConcurrentDictionary<String, Object>();

        public EndPoint RemoteAddress { get; set; }

        public SslSession SslSession { get; set; }

        public IEnumerable<KeyValuePair<string, object>> Attributes
        {
            get { return _attributes; }
        }

        public object GetAttribute(string attributeName)
        {
            Guard.ArgumentNullOrWhiteSpaceString(attributeName, nameof(attributeName));
            Object val = null;
            this._attributes.TryGetValue(attributeName, out val);
            return val;
        }

        public object SetAttribute(string attributeName, object value)
        {
            Guard.ArgumentNullOrWhiteSpaceString(attributeName, nameof(attributeName));
            this._attributes[attributeName] = value;
            return value;
        }

        public object RemoveAttribute(string attributeName)
        {
            Guard.ArgumentNullOrWhiteSpaceString(attributeName, nameof(attributeName));
            Object old = null;
            this._attributes.TryRemove(attributeName, out old);
            return old;
        }
    }
}
