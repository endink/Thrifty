using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.Nifty.Core;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DotNetty.Handlers.Tls;
using Thrifty.Nifty.Ssl;

namespace Thrifty.Nifty.Client
{
    public class NiftyClient : IDisposable
    {
        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan DefaultReceiveTimeout = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan DefaultSendTimeout = TimeSpan.FromSeconds(2);

        private const int DefaultMaxFrameSize = 16777216;
        private IChannelGroup _allChannel = null;
        private ITimer _timer;
        private bool _disposed;
        private ILogger _logger;
        private bool _closed;

        public NiftyClient() : this(new NettyClientConfig())
        {

        }

        public NiftyClient(NettyClientConfig nettyClientConfig, ILoggerFactory loggerFactorty = null)
        {
            Guard.ArgumentNotNull(nettyClientConfig, nameof(nettyClientConfig));
            this.NettyClientConfig = nettyClientConfig;

            this._timer = nettyClientConfig.Timer;
            this.WorkerExecutor = nettyClientConfig.WorkerExecutor;
            this.DefaultSocksProxyAddress = nettyClientConfig.DefaultSocksProxyAddress;
            this._allChannel = new DefaultChannelGroup(null);
            _logger = loggerFactorty?.CreateLogger<NiftyClient>() ?? (ILogger)NullLogger.Instance;
        }


        private NettyClientConfig NettyClientConfig { get; }
        private IEventLoopGroup WorkerExecutor { get; }
        //private NioClientSocketChannelFactory channelFactory;
        private EndPoint DefaultSocksProxyAddress { get; }
        //private IChannelGroup allChannels = new DefaultChannelGroup();

        public Task<T> ConnectAsync<T>(
            INiftyClientConnector<T> clientChannelConnector, ClientSslConfig sslConfig)
            where T : INiftyClientChannel
        {
            return this.ConnectAsync(clientChannelConnector,
                                DefaultConnectTimeout,
                                DefaultReceiveTimeout,
                                DefaultReadTimeout,
                                DefaultSendTimeout,
                                DefaultMaxFrameSize,
                                sslConfig,
                                this.DefaultSocksProxyAddress);
        }

        public Task<T> ConnectAsync<T>(
        INiftyClientConnector<T> clientChannelConnector,
        TimeSpan? connectTimeout,
        TimeSpan? receiveTimeout,
        TimeSpan? readTimeout,
        TimeSpan? sendTimeout,
        ClientSslConfig sslConfig,
        int maxFrameSize)
            where T : INiftyClientChannel
        {
            return ConnectAsync(clientChannelConnector,
                                connectTimeout,
                                receiveTimeout,
                                readTimeout,
                                sendTimeout,
                                maxFrameSize,
                                sslConfig,
                                this.DefaultSocksProxyAddress);
        }

        //暂时不支持代理，不暴露这个方法。
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<TNiftyClientChannelTransport> ConnectAsync<TChannel, TClient>(
                    INiftyClientConnector<TChannel> clientChannelConnector,
                    TimeSpan? connectTimeout,
                    TimeSpan? receiveTimeout,
                    TimeSpan? readTimeout,
                    TimeSpan? sendTimeout,
                    int maxFrameSize,
                    ClientSslConfig sslConfig,
                    EndPoint socksProxyAddress)
                    where TChannel : INiftyClientChannel
        {
            return ConnectAsync(
                            clientChannelConnector,
                            connectTimeout,
                            receiveTimeout,
                            readTimeout,
                            sendTimeout,
                            maxFrameSize,
                            sslConfig,
                            socksProxyAddress)
                            .ContinueWith(t =>
                            {
                                try
                                {
                                    return new TNiftyClientChannelTransport(typeof(TClient), t.GetAwaiter().GetResult());
                                }
                                catch (Exception e)
                                {
                                    throw new ThriftyTransportException($"Failed to establish client connection.{Environment.NewLine}{e.Message}", e, ThriftyTransportException.ExceptionType.NotOpen);
                                }
                            });



        }

        public Task<TNiftyClientChannelTransport> ConnectAsync<T, TClient>(
            INiftyClientConnector<T> clientChannelConnector,
            TimeSpan? connectTimeout,
            TimeSpan? receiveTimeout,
            TimeSpan? readTimeout,
            TimeSpan? sendTimeout,
            int maxFrameSize,
            ClientSslConfig sslConfig)
            where T : INiftyClientChannel
        {
            return ConnectAsync<T, TClient>(
                    clientChannelConnector,
                    connectTimeout,
                    receiveTimeout,
                    readTimeout,
                    sendTimeout,
                    maxFrameSize,
                    sslConfig,
                    (EndPoint)null);
        }

        //暂时不支持代理，不暴露这个方法。
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> ConnectAsync<T>(
                INiftyClientConnector<T> clientChannelConnector,
                TimeSpan? connectTimeout,
                TimeSpan? receiveTimeout,
                TimeSpan? readTimeout,
                TimeSpan? sendTimeout,
                int maxFrameSize,
                ClientSslConfig sslConfig,
                EndPoint socksProxyAddress)
        where T : INiftyClientChannel
        {
            this.ThrowIfDisposed();
            Guard.ArgumentNotNull(clientChannelConnector, nameof(clientChannelConnector));

            Bootstrap bootstrap = new Bootstrap();
            bootstrap.Group(this.WorkerExecutor)
                            .Channel<TcpSocketChannel>()
                           .Option(ChannelOption.TcpNodelay, true);

            if (connectTimeout != null)
            {
                bootstrap.Option(ChannelOption.ConnectTimeout, connectTimeout.Value);
            }
            bootstrap.Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
            {
                clientChannelConnector.ConfigureChannelPipeline(channel.Pipeline, maxFrameSize, this.NettyClientConfig, sslConfig);
            }));
            Task<IChannel> connectTask = clientChannelConnector.ConnectAsync(bootstrap);
            return connectTask.ContinueWith(t =>
            {
                if (t.Exception == null && t.Result != null && t.Result.Open)
                {
                    _allChannel.Add(t.GetAwaiter().GetResult());
                }
                if (t.Exception != null)
                {
                    _logger.LogError(nameof(EventId), t.Exception, "Failed to establish client connection.");
                }
                return CreateNiftyClientChannel(clientChannelConnector, receiveTimeout, readTimeout, sendTimeout, t);
            });
        }

        private T CreateNiftyClientChannel<T>(INiftyClientConnector<T> clientChannelConnector,
            TimeSpan? receiveTimeout,
            TimeSpan? readTimeout,
            TimeSpan? sendTimeout,
            Task<IChannel> future)
            where T : INiftyClientChannel
        {
            this.ThrowIfDisposed();
            try
            {
                if (future.Status == TaskStatus.RanToCompletion)
                {
                    IChannel nettyChannel = future.GetAwaiter().GetResult();
                    var channel = clientChannelConnector.NewThriftClientChannel(nettyChannel,
                                                                              this.NettyClientConfig);
                    channel.ReceiveTimeout = receiveTimeout;
                    channel.ReadTimeout = readTimeout;
                    channel.SendTimeout = sendTimeout;
                    return channel;
                }
                else if (future.IsCanceled)
                {
                    throw new ThriftyTransportException($"Unable to cancel client channel connection ( server: {clientChannelConnector.ServerAddress} ).", ThriftyTransportException.ExceptionType.NotOpen);
                }
                else if (future.Exception != null)
                {
                    throw new ThriftyTransportException($"Unable to open client channel connection ( server: {clientChannelConnector.ServerAddress} ).", future.Exception, ThriftyTransportException.ExceptionType.NotOpen);
                }
                else
                {
                    throw new ThriftyTransportException($"Unable to open client channel connection ( server: {clientChannelConnector.ServerAddress} ) . connection task status is '{future.Status}' .", ThriftyTransportException.ExceptionType.NotOpen);
                }
            }
            catch (AggregateException t)
            {
                throw new ThriftyTransportException($"Failed to establish client connection ( server: {clientChannelConnector.ServerAddress} ).", t, ThriftyTransportException.ExceptionType.Unknown);
            }
        }

        // 代理模式：
        //private Bootstrap createClientBootstrap(EndPoint socksProxyAddress)
        //{
        //    if (socksProxyAddress != null)
        //    {
        //        return new Socks4ClientBootstrap(channelFactory, toInetAddress(socksProxyAddress));
        //    }
        //    else
        //    {
        //        return new ClientBootstrap(channelFactory);
        //    }
        //}

        public async Task CloseAsync(TimeSpan? timeToClose = null)
        {
            this.ThrowIfDisposed();
            if (_closed)
            {
                _closed = true;
                // Stop the timer thread first, so no timeouts can fire during the rest of the
                // shutdown process
                await this._timer.StopAsync();

                await ShutdownUtil.ShutdownChannelAsync(
                                                    null,
                                                    this.WorkerExecutor,
                                                    this._allChannel, timeToClose ?? TimeSpan.FromSeconds(15));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        ~NiftyClient()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    try
                    {
                        this.CloseAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        ex.ThrowIfNecessary();
                    }
                    this._timer = null;
                }
            }
            _disposed = true;
        }
    }


}
