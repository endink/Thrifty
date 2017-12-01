using DotNetty.Common.Concurrency;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty;
using Thrifty.Nifty.Codecs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Handlers.Tls;
using Thrifty.Nifty.Ssl;
using Thrift.Protocol;

namespace Thrifty.Nifty.Core
{
    public partial class NettyServerTransport
    {
        private readonly int _requestedPort;
        private int _actualPort;
        private readonly Action<IChannelPipeline> _pipelineSetup;
        private const int NoWriterIdleTimeout = 0;
        private const int NoAllIdleTimeout = 0;
        private IEventLoopGroup _bossExecutor;
        private IEventLoopGroup _ioWorkerExecutor;
        private ServerBootstrap _bootstrap;
        private IChannel _serverChannel;
        private readonly ThriftServerDef _def;
        private readonly NettyServerConfig _nettyServerConfig;
        private ILogger _logger;
        //private readonly ChannelStatistics channelStatistics;

        //private AtomicReference<SslServerConfiguration> sslConfiguration = new AtomicReference<>();

        public NettyServerTransport(ThriftServerDef def) :
                this(def, new NettyServerConfig())
        {

        }

        public NettyServerTransport(
                ThriftServerDef def,
                NettyServerConfig nettyServerConfig)
        {
            this._logger = nettyServerConfig.LoggerFactory?.CreateLogger(this.GetType()) ?? NullLogger.Instance;
            this._def = def;
            this._nettyServerConfig = nettyServerConfig;
            this._requestedPort = def.ServerPort;
            //this.allChannels = allChannels;


            //this.channelStatistics = new ChannelStatistics(allChannels);

            //this.sslConfiguration.set(this.def.getSslConfiguration());
            X509Certificate2 tlsCertificate = null;
            if (this._def.SslConfig!=null)
            {
                tlsCertificate = CertificateHelper.GetCertificate(this._def.SslConfig);
            }

            this._pipelineSetup = cp =>
            {
                TProtocolFactory inputProtocolFactory = def.DuplexProtocolFactory.GetInputProtocolFactory();
                if (tlsCertificate != null)
                {
                    cp.AddLast("tls", TlsHandler.Server(tlsCertificate));
                }
                //NiftySecurityHandlers securityHandlers = def.getSecurityFactory().getSecurityHandlers(def, nettyServerConfig);
                cp.AddLast("connectionContext", new ConnectionContextHandler());
                if (def.MaxConnections >0 && def.MaxConnections != int.MaxValue)
                {
                    // connectionLimiter must be instantiated exactly once (and thus outside the pipeline factory)
                    ConnectionLimiter connectionLimiter = new ConnectionLimiter(def.MaxConnections, nettyServerConfig.LoggerFactory);
                    cp.AddLast("connectionLimiter", connectionLimiter);
                }
                //cp.addLast(ChannelStatistics.NAME, channelStatistics);
                //cp.AddLast("encryptionHandler", securityHandlers.getEncryptionHandler());
                //cp.AddLast("ioDispatcher", new NiftyIODispatcher());

                //cp.AddLast("encoder", new DefaultThriftFrameEncoder(def.MaxFrameSize));
                cp.AddLast("thriftDecoder", def.ThriftFrameCodecFactory.Create(def.MaxFrameSize, inputProtocolFactory, _nettyServerConfig.LoggerFactory));
                if (def.ClientIdleTimeout > TimeSpan.Zero)
                {
                    // Add handlers to detect idle client connections and disconnect them
                    cp.AddLast("idleTimeoutHandler", new IdleStateHandler(
                                                                            (int)def.ClientIdleTimeout.TotalSeconds,
                                                                              NoWriterIdleTimeout,
                                                                              NoAllIdleTimeout));
                    cp.AddLast("idleDisconnectHandler", new IdleDisconnectHandler());
                }

                //cp.addLast("authHandler", securityHandlers.getAuthenticationHandler());
                cp.AddLast("dispatcher", new NiftyDispatcher(def, nettyServerConfig.Timer, nettyServerConfig.IOThreadCount, nettyServerConfig.LoggerFactory));
                //cp.AddLast("exceptionLogger", new NiftyExceptionLogger());
            };
        }

        

        public Task StartAsync()
        {
            _bossExecutor = _nettyServerConfig.BossGroup;
            _ioWorkerExecutor = _nettyServerConfig.WorkerGroup;
            this._bootstrap = new ServerBootstrap();
            //foreach (var option in nettyServerConfig.BootstrapOptions)
            //{
            //    this.bootstrap.Option(option, value);
            //}
            this._bootstrap.Group(_bossExecutor, _ioWorkerExecutor);
            return this.StartAsync(_bootstrap);
        }

        public async Task StartAsync(ServerBootstrap bootstrap)
        {
            bootstrap.Channel<TcpServerSocketChannel>();
            bootstrap
                .Handler(new LoggingHandler($"{this._def.Name}", DotNetty.Handlers.Logging.LogLevel.INFO))
                .ChildHandler(new ActionChannelInitializer<IChannel>(c =>
                {
                    this._pipelineSetup(c.Pipeline);
                }));
            IPAddress address = null;
            address = GetIPAddress();

            // bootstrap.Option(nettyServerConfig.BootstrapOptions);
            _serverChannel = await bootstrap.BindAsync(address, this._requestedPort);
            var actualSocket = _serverChannel.LocalAddress;

            IPEndPoint localPoint = _serverChannel.LocalAddress as IPEndPoint;
            _actualPort = localPoint?.Port ?? 0;
            if (_actualPort == 0)
            {
                DnsEndPoint dnsPoint = _serverChannel.LocalAddress as DnsEndPoint;
                _actualPort = dnsPoint?.Port ?? 0;
            }
            //Preconditions.checkState(actualPort != 0 && (actualPort == requestedPort || requestedPort == 0));
            _logger.LogInformation($"transport {_def.Name}:{_actualPort} was started.");
        }

        private IPAddress GetIPAddress()
        {
            if (String.IsNullOrWhiteSpace(_def.Host))
            {
                return IPAddress.Any;
            }
            IPAddress address;
            try
            {
                var host = Dns.GetHostAddressesAsync(_def.Host)
                    .GetAwaiter().GetResult().Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (!host.Any())
                {
                    throw new ThriftyException($"host name {host} is not a  invalid host name or ip address.");
                }
                address = host.FirstOrDefault();
            }
            catch (ArgumentException)
            {
                throw new ThriftyException($"host name {_def.Host} is not a  invalid host name or ip address.");
            }

            return address;
        }

        public Task StopAsync(TimeSpan? timeout = null)
        {
            if (_serverChannel != null && _serverChannel.Open)
            {
                _logger.LogInformation($"transport {_def.Name}:{this._actualPort} is shutting down");
                TimeSpan to = timeout.HasValue ? TimeSpan.FromSeconds(Math.Max(3, timeout.Value.Seconds)) : TimeSpan.FromSeconds(15);
                return _serverChannel.CloseAsync().ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        // first stop accepting
                        var t1 = _bossExecutor.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), to);
                        var t2 = _ioWorkerExecutor.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), to);
                        Task.WaitAll(t1, t2);
                    }
                });
            }
            return Task.FromResult(0);
        }

        public IChannel ServerChannel
        {
            get { return _serverChannel; }
        }

        public int Port
        {
            get
            {
                if (_actualPort != 0)
                {
                    return _actualPort;
                }
                else
                {
                    return _requestedPort; // may be 0 if server not yet started
                }
            }
        }
    }
}
