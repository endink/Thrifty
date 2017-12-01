using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public class ThriftClient
    {
        private readonly ThriftClientManager _clientManager;
        
        private readonly IEnumerable<ThriftClientEventHandler> _eventHandlers;

        public ThriftClient(ThriftClientManager clientManager, Type clientType)
            : this(clientManager, clientType, new ThriftClientConfig(), ThriftClientManager.DefaultClientName)
        {
            
        }

        public ThriftClient(
            ThriftClientManager clientManager,
            Type clientType,
            ThriftClientConfig clientConfig,
            String clientName,
            IEnumerable<ThriftClientEventHandler> eventHandlers = null):
            this(clientManager,
                    clientType,
                    clientName,
                    clientConfig.ConnectTimeout,
                    clientConfig.ReceiveTimeout,
                    clientConfig.ReadTimeout,
                    clientConfig.WriteTimeout,
                    clientConfig.SocksProxy,
                    clientConfig.MaxFrameSize,
                    clientConfig.SslConfig,
                    eventHandlers ?? Enumerable.Empty<ThriftClientEventHandler>())
        {
            
        }

        public ThriftClient(
            ThriftClientManager clientManager,
            Type clientType,
            String clientName,
            TimeSpan connectTimeout,
            TimeSpan receiveTimeout,
            TimeSpan readTimeout,
            TimeSpan writeTimeout,
            EndPoint socksProxy,
            int maxFrameSize,
            ClientSslConfig sslConfig,
            IEnumerable<ThriftClientEventHandler> eventHandlers)
        {
            Guard.ArgumentNotNull(clientManager, nameof(clientManager));
            Guard.ArgumentNotNull(clientType, nameof(clientType));
            Guard.ArgumentNotNull(clientName, nameof(clientName));
            Guard.ArgumentNotNull(connectTimeout, nameof(connectTimeout));
            Guard.ArgumentNotNull(receiveTimeout, nameof(receiveTimeout));
            Guard.ArgumentNotNull(readTimeout, nameof(readTimeout));
            Guard.ArgumentNotNull(writeTimeout, nameof(writeTimeout));
            Guard.ArgumentCondition(maxFrameSize >= 0, "maxFrameSize cannot be negative");
            Guard.ArgumentNotNull(eventHandlers, nameof(clientManager));          

            this._clientManager = clientManager;
            this.ClientType = clientType;
            this.ClientName = clientName;
            this.ConnectTimeout = connectTimeout;
            this.ReceiveTimeout = receiveTimeout;
            this.ReadTimeout = readTimeout;
            this.WriteTimeout = writeTimeout;
            this.SocksProxy = socksProxy;
            this.MaxFrameSize = maxFrameSize;
            this.SslConfig = sslConfig;
            this._eventHandlers = eventHandlers;
        }

        public Type ClientType { get; }

        public String ClientName { get; }

        public TimeSpan ConnectTimeout { get; }
        public TimeSpan ReceiveTimeout { get; }
        public TimeSpan ReadTimeout { get; }
        public TimeSpan WriteTimeout { get; }

        public EndPoint SocksProxy { get; }
        public int MaxFrameSize { get; }

        public ClientSslConfig SslConfig { get; }
        
        /// <summary>
        /// Asynchronously connect to a service to create a new client.
        /// </summary>
        /// <typeparam name="TChannel"></typeparam>
        /// <param name="connector">Connector used to establish the new connection</param>
        /// <returns>Future that will be set to the client once the connection is established</returns>
        public Task<Object> OpenAsync<TChannel>(INiftyClientConnector<TChannel> connector)
            where TChannel : INiftyClientChannel
        {
            return _clientManager.CreateClientAsync(
                    connector,
                    this.ClientType,
                    ConnectTimeout,
                    ReceiveTimeout,
                    ReadTimeout,
                    WriteTimeout,
                    MaxFrameSize,
                    ClientName,
                    this.SslConfig,
                    this._eventHandlers,
                    GetSocksProxyOrDefault());
        }

        private EndPoint GetSocksProxyOrDefault()
        {
            return (this.SocksProxy != null) ? SocksProxy : _clientManager.DefaultSocksProxy;
        }
    }
}
