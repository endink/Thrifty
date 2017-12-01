using Thrifty.Nifty.Client;
using Thrifty.Nifty.Core;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Tests.Services
{
    internal class ScopedServer : IDisposable
    {
        private NettyServerTransport server;

        public ScopedServer(TProtocolFactory protocolFactory, object service)
        {
            ThriftServiceProcessor processor = new ThriftServiceProcessor(services: service);

            ThriftServerDef def = new ThriftServerDef(processor, protocolFactory, serverPort: 0);

            server = new NettyServerTransport(def, new NettyServerConfig(1, 1));
            server.StartAsync().GetAwaiter().GetResult();
        }

        public int Port
        {
            get
            {
                IPEndPoint address = (IPEndPoint)server.ServerChannel.LocalAddress;
                return address.Port;
            }
        }

        public void Dispose()
        {
            try
            {
                server.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public T CreateScribeClient<T>(
                           ThriftClientManager manager, ScopedServer server, TProtocolFactory protocolFactory)
            where T : class
        {
            ThriftClientConfig config = new ThriftClientConfig();
            config.ConnectTimeout = TimeSpan.FromSeconds(600);
            config.ReceiveTimeout = TimeSpan.FromSeconds(600);
            config.ReadTimeout = TimeSpan.FromSeconds(600);
            config.WriteTimeout = TimeSpan.FromSeconds(600);

            var thriftClient = new ThriftClient(manager, typeof(T), config, typeof(T).Name);
            return thriftClient.OpenAsync(
                new FramedClientConnector("localhost", server.Port, protocolFactory))
                .GetAwaiter().GetResult() as T;
        }
    }
}
