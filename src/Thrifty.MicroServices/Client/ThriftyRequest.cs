using System;
using System.Reflection;
using Thrifty.MicroServices.Ribbon.Client;

namespace Thrifty.MicroServices.Client
{
    public class ThriftyRequest : IClientRequest
    {
        public ThriftyRequest(bool retriable, int retriesNextServer, int retriesSameServer,
            int readTimeout, int writeTimeout, int reveiveTimeout, int connectTimeout, object[] args, MethodInfo method)
        {
            Retriable = retriable;
            RetriesNextServer = retriesNextServer;
            RetriesSameServer = retriesSameServer;
            ReadTimeout = readTimeout;
            WriteTimeout = writeTimeout;
            ReveiveTimeout = reveiveTimeout;
            ConnectTimeout = connectTimeout;
            Args = args;
            Method = method;
        }
        public Uri Uri { get; set; }
        public bool Retriable { get; }
        public ChannelKey ChannelKey { get; set; }
        public object Stub { get; set; }
        public int RetriesNextServer { get; }
        public int RetriesSameServer { get; }
        public int WriteTimeout { get; }
        public int ReadTimeout { get; }
        public int ConnectTimeout { get; }
        public int ReveiveTimeout { get; }

        public object[] Args { get; }
        public MethodInfo Method { get; }
    }
}
