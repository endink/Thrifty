using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Thrifty.Nifty.Client;
using Thrifty.Codecs;
using System.Collections.Concurrent;
using System.Text;
using static Thrifty.Services.ThriftClientConfig;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift;
using Microsoft.Extensions.Logging.Abstractions;

namespace Thrifty.Services
{
    public partial class ThriftClientManager : IDisposable
    {
        public const String DefaultClientName = "default";
        private bool _disposed;

        private readonly ThriftCodecManager codecManager;
        private NiftyClient niftyClient;

        private static readonly ConcurrentDictionary<TypeAndName, ThriftClientMetadata> clientMetadataCache =
            new ConcurrentDictionary<TypeAndName, ThriftClientMetadata>();
        private readonly IEnumerable<ThriftClientEventHandler> globalEventHandlers;
        private ILoggerFactory _loggerFactory;
        private ILogger _logger;
        private IThriftClientFactory _thriftClientFactory;

        public ThriftClientManager(IThriftClientFactory clientFactory = null, ILoggerFactory loggerFactory = null)
            : this(new ThriftCodecManager(), clientFactory, loggerFactory)
        {
        }

        public ThriftClientManager(ThriftCodecManager codecManager, IThriftClientFactory clientFactory = null, ILoggerFactory loggerFactory = null)
            : this(codecManager, new NiftyClient(), Enumerable.Empty<ThriftClientEventHandler>(), clientFactory, loggerFactory)
        {

        }

        public ThriftClientManager(ThriftCodecManager codecManager, NiftyClient niftyClient, IEnumerable<ThriftClientEventHandler> globalEventHandlers, IThriftClientFactory clientFactory = null, ILoggerFactory loggerFactory = null)
        {
            Guard.ArgumentNotNull(codecManager, nameof(codecManager));
            Guard.ArgumentNotNull(niftyClient, nameof(niftyClient));
            this.niftyClient = niftyClient;
            this.codecManager = codecManager;
            this.globalEventHandlers = globalEventHandlers ?? Enumerable.Empty<ThriftClientEventHandler>();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ThriftClientManager>() ?? (ILogger)NullLogger.Instance;
            _thriftClientFactory = clientFactory ?? new DynamicProxyClientFactory();
        }

        public object CreateClient(INiftyClientChannel channel, Type clientType, String name, IEnumerable<ThriftClientEventHandler> eventHandlers)
        {
            this.ThrowIfDisposed();
            Guard.ArgumentNotNull(channel, nameof(channel));
            Guard.ArgumentNotNull(clientType, nameof(clientType));
            Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
       
            if (!channel.NettyChannel.Active || channel.HasError)
            {
                throw new ThriftyTransportException($"fault to connect swifty server : {channel.NettyChannel.RemoteAddress}", ThriftyTransportException.ExceptionType.NotOpen);
            }

            eventHandlers = eventHandlers ?? Enumerable.Empty<ThriftClientEventHandler>();

            ThriftClientMetadata clientMetadata = clientMetadataCache.GetOrAdd(new TypeAndName(clientType, name), this.Load);

            String clientDescription = $"{clientMetadata.Name} {channel.ToString()}";

            var allHandlers = globalEventHandlers.Concat(eventHandlers).ToArray();
            return _thriftClientFactory.CreateClient(channel, clientType, clientMetadata, allHandlers, clientDescription);
        }

        public EndPoint DefaultSocksProxy { get; }

        private ThriftClientMetadata Load(TypeAndName typeAndName)
        {
            return new ThriftClientMetadata(typeAndName.Type, typeAndName.Name, codecManager);
        }

        public Task<TClient> CreateClientAsync<TClient>(
            String host,
            int port,
            ThriftClientConfig config = null,
            String clientName = null,
            ClientSslConfig sslConfig = null,
            IEnumerable<ThriftClientEventHandler> eventHandlers = null)
            where TClient : class
        {
            return this.CreateClientAsync(host, port, typeof(TClient), config, clientName, sslConfig, eventHandlers)
                .ContinueWith(t => t.Result as TClient);
        }

        public Task<TClient> CreateClientAsync<TClient, TChannel>(
             INiftyClientConnector<TChannel> connector,
            ThriftClientConfig config = null,
            String clientName = null,
            ClientSslConfig sslConfig = null,
            IEnumerable<ThriftClientEventHandler> eventHandlers = null)
            where TClient : class
            where TChannel : INiftyClientChannel
        {
            return this.CreateClientAsync(connector, typeof(TClient), config, clientName, sslConfig, eventHandlers)
                .ContinueWith(t => t.Result as TClient);
        }

        public Task<Object> CreateClientAsync(
            String host,
            int port,
            Type clientType,
            ThriftClientConfig config = null,
            String clientName = null,
            ClientSslConfig sslConfig = null,
            IEnumerable<ThriftClientEventHandler> eventHandlers = null)
        {
            return this.CreateClientAsync(
                new FramedClientConnector(host, port, _loggerFactory),
                clientType, config, clientName, sslConfig, eventHandlers);
        }


        public Task<Object> CreateClientAsync<TChannel>(
            INiftyClientConnector<TChannel> connector,
            Type clientType,
            ThriftClientConfig config = null,
            String clientName = null,
            ClientSslConfig sslConfig = null,
            IEnumerable<ThriftClientEventHandler> eventHandlers = null)
            where TChannel : INiftyClientChannel
        {
            config = config ?? new ThriftClientConfig();
            return this.CreateClientAsync(connector,
                clientType,
                config.ConnectTimeout,
                config.ReceiveTimeout,
                config.ReceiveTimeout,
                config.WriteTimeout,
                config.MaxFrameSize,
                clientName,
                sslConfig,
                eventHandlers,
                null);
        }

        public Task<Object> CreateClientAsync<TChannel>(
                    INiftyClientConnector<TChannel> connector,
                    Type clientType,
                    TimeSpan? connectTimeout,
                    TimeSpan? receiveTimeout,
                    TimeSpan? readTimeout,
                    TimeSpan? writeTimeout,
                    int maxFrameSize,
                    string clientName,
                    ClientSslConfig sslConfig,
                    IEnumerable<ThriftClientEventHandler> eventHandlers,
                    EndPoint socksProxy)
                    where TChannel : INiftyClientChannel
        {
            this.ThrowIfDisposed();
            Guard.ArgumentNotNull(connector, nameof(connector));
            Guard.ArgumentNotNull(clientType, nameof(clientType));

            eventHandlers = eventHandlers ?? Enumerable.Empty<ThriftClientEventHandler>();

            var connectFuture = this.CreateChannelAsync(
                connector,
                connectTimeout,
                receiveTimeout,
                readTimeout,
                writeTimeout,
                maxFrameSize,
                sslConfig,
                socksProxy);

            return connectFuture.ContinueWith(t =>
            {
                String name = String.IsNullOrWhiteSpace(clientName) ? DefaultClientName : clientName;
                INiftyClientChannel channel = null;
                try
                {
                    channel = t.Result;
                    return this.CreateClient(channel, clientType, name, eventHandlers);
                }
                catch (AggregateException ex)
                {
                    _logger.LogError(0, ex, $"create clinet channel fault.");
                    // The channel was created successfully, but client creation failed so the
                    // channel must be closed now
                    channel?.CloseAsync();
                    throw;
                }
            });
        }

        public Task<TChannel> CreateChannelAsync<TChannel>(INiftyClientConnector<TChannel> connector, ClientSslConfig sslConfig = null)
            where TChannel : INiftyClientChannel
        {
            return CreateChannelAsync(connector,
                                 DEFAULT_CONNECT_TIMEOUT,
                                 DEFAULT_RECEIVE_TIMEOUT,
                                 DEFAULT_READ_TIMEOUT,
                                 DEFAULT_WRITE_TIMEOUT,
                                 DEFAULT_MAX_FRAME_SIZE,
                                 sslConfig,
                                 this.DefaultSocksProxy);
        }

        public Task<TChannel> CreateChannelAsync<TChannel>(
            INiftyClientConnector<TChannel> connector,
            TimeSpan? connectTimeout,
            TimeSpan? receiveTimeout,
            TimeSpan? readTimeout,
            TimeSpan? writeTimeout,
            int maxFrameSize,
            ClientSslConfig sslConfig = null,
            EndPoint socksProxy = null)
            where TChannel : INiftyClientChannel
        {
            this.ThrowIfDisposed();
            Guard.ArgumentNotNull(connector, nameof(connector));
            var connectFuture = niftyClient.ConnectAsync(
                    connector,
                    connectTimeout,
                    receiveTimeout,
                    readTimeout,
                    writeTimeout,
                    maxFrameSize,
                    sslConfig,
                    socksProxy);

            return connectFuture;
        }

        public ThriftClientMetadata GetClientMetadata(Type clientType, String name = DefaultClientName)
        {
            this.ThrowIfDisposed();
            Guard.ArgumentNotNull(clientType, nameof(clientType));

            ThriftClientMetadata metadata = null;
            clientMetadataCache.TryGetValue(new TypeAndName(clientType, name), out metadata);
            return metadata;
        }

        ~ThriftClientManager()
        {
            this.Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task CloseAsync(TimeSpan? timeout = null)
        {
            return this.niftyClient?.CloseAsync(timeout);
        }

        internal static ThriftyRuntimeException WrapTException(Exception e)
        {
            //noinspection InstanceofCatchParameter
            if (e is TApplicationException)
            {
                return new ThriftyApplicationException(e.Message, (TApplicationException)e);
            }
            //noinspection InstanceofCatchParameter
            if (e is TProtocolException)
            {
                return new ThriftyProtocolException(e.Message, (TProtocolException)e);
            }
            //noinspection InstanceofCatchParameter
            if (e is TTransportException tex)
            {
                return new ThriftyTransportException(e.Message, tex, tex.Type.ToThriftyError());
            }
            return new ThriftyRuntimeException(e.Message, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    niftyClient?.Dispose();
                    niftyClient = null;
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private class TypeAndName : IEquatable<TypeAndName>
        {
            private readonly int _hash;
            [DebuggerHidden]
            public TypeAndName(Type type, String name)
            {
                Guard.ArgumentNotNull(type, nameof(type));
                Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
                this.Type = type;
                this.Name = name;
                var result = Type.GetHashCode();
                _hash = 31 * result + Name.GetHashCode();
            }
            public Type Type { get; }
            public string Name { get; }
            public override string ToString()
            {
                return new StringBuilder()
                    .Append("TypeAndName")
                    .Append("{type=").Append(Type)
                    .Append(", name='").Append(Name)
                    .Append('}').ToString();
            }
            public override bool Equals(object o)
            {
                if (ReferenceEquals(null, o)) return false;
                if (ReferenceEquals(this, o)) return true;
                if (o.GetType() != this.GetType()) return false;
                return Equals((TypeAndName)o);
            }

            [DebuggerHidden]
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _hash;
                    hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public bool Equals(TypeAndName other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return _hash == other._hash && Equals(Type, other.Type) && string.Equals(Name, other.Name);
            }
        }
    }
}
