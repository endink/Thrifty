using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Thrifty.Nifty.Duplex;
using Thrift.Protocol;
using Thrifty.Nifty.Codecs;
using Thrifty.Nifty.Processors;
using Thrifty.Nifty.Core;
using Thrifty.Threading;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using DotNetty.Common.Utilities;
using Thrifty.Nifty.Ssl;

namespace Thrifty.Services
{
    public class ThriftServer : IDisposable
    {
        private static readonly ImmutableDictionary<String, TDuplexProtocolFactory> DefaultProtocolFactories;
        private static readonly ImmutableDictionary<String, IThriftFrameCodecFactory> DefaultFrameCodecFactories;
        private NettyServerTransport _transport;
        private readonly ManualResetEvent _eventLocked;
        private ILogger _logger = null;

        static ThriftServer()
        {
            var factoryBuilder = ImmutableDictionary.CreateBuilder<String, TDuplexProtocolFactory>();
            factoryBuilder.Add("binary", TDuplexProtocolFactory.FromSingleFactory(new TBinaryProtocol.Factory()));
            factoryBuilder.Add("compact", TDuplexProtocolFactory.FromSingleFactory(new TCompactProtocol.Factory()));
            DefaultProtocolFactories = factoryBuilder.ToImmutableDictionary();

            var codecBuilder = ImmutableDictionary.CreateBuilder<String, IThriftFrameCodecFactory>();
            codecBuilder.Add("buffered", new DefaultThriftFrameCodecFactory());
            codecBuilder.Add("framed", new DefaultThriftFrameCodecFactory());
            DefaultFrameCodecFactories = codecBuilder.ToImmutableDictionary();
        }

        public ThriftServer(NettyServerConfig nettyServerConfig, ThriftServerDef thriftServerDef, ILoggerFactory loggerFactory = null)
        {
            Guard.ArgumentNotNull(nettyServerConfig, nameof(nettyServerConfig));
            Guard.ArgumentNotNull(thriftServerDef, nameof(thriftServerDef));

            _eventLocked = new ManualResetEvent(true);
            _logger = loggerFactory?.CreateLogger<ThriftServer>() ?? (ILogger)NullLogger.Instance;

            _transport = new NettyServerTransport(thriftServerDef, nettyServerConfig);
        }

        public ThriftServer(
            INiftyProcessor processor,
            int port = 0,
            SslConfig sslConfig = null,
            ILoggerFactory loggerFactory = null)
            : this(processor,
                new ThriftServerConfig() { Port = Math.Max(0, port) },
                DefaultFrameCodecFactories,
                DefaultProtocolFactories,
                sslConfig,
                loggerFactory)
        {

        }

        public ThriftServer(
            INiftyProcessor processor,
            ThriftServerConfig serverConfig,
            SslConfig sslConfig = null,
            ILoggerFactory loggerFactory = null)
            : this(processor,
                serverConfig,
                DefaultFrameCodecFactories,
                DefaultProtocolFactories,
                sslConfig,
                loggerFactory)
        {

        }

        public ThriftServer(
            INiftyProcessor processor,
            ThriftServerConfig serverConfig,
            IDictionary<String, IThriftFrameCodecFactory> availableFrameCodecFactories,
            IDictionary<String, TDuplexProtocolFactory> availableProtocolFactories,
            SslConfig sslConfig = null,
            ILoggerFactory loggerFactory = null
        )
        {
            Guard.ArgumentNotNull(serverConfig, nameof(serverConfig));
            Guard.ArgumentNotNull(availableFrameCodecFactories, nameof(availableFrameCodecFactories));
            Guard.ArgumentNotNull(availableProtocolFactories, nameof(availableProtocolFactories));
            Guard.ArgumentCondition(availableFrameCodecFactories.ContainsKey(serverConfig.TransportName), $"No available server transport named {serverConfig.TransportName}");
            Guard.ArgumentCondition(availableProtocolFactories.ContainsKey(serverConfig.ProtocolName), $"No available server protocol named {serverConfig.ProtocolName}");

            _eventLocked = new ManualResetEvent(true);
            _logger = loggerFactory?.CreateLogger<ThriftServer>() ?? (ILogger)NullLogger.Instance;
            ThriftServerDef def = new ThriftServerDef(
                new DelegateNiftyProcessorFactory(t => processor),
                availableProtocolFactories[serverConfig.ProtocolName],
                "thrift",
                serverConfig.BindingAddress,
                serverConfig.Port,
                serverConfig.MaxFrameSizeBytes,
                serverConfig.MaxQueuedResponsesPerConnection,
                serverConfig.ConnectionLimit ?? int.MaxValue,
                availableFrameCodecFactories[serverConfig.TransportName],
                serverConfig.IdleConnectionTimeout,
                serverConfig.TaskExpirationTimeout != TimeSpan.Zero ? serverConfig.TaskExpirationTimeout : (TimeSpan?)null,
                serverConfig.QueueTimeout != TimeSpan.Zero ? serverConfig.QueueTimeout : (TimeSpan?)null,
                sslConfig
            );

            NettyServerConfig config = new NettyServerConfig(new HashedWheelTimer(),
                new MultithreadEventLoopGroup(serverConfig.AcceptorThreadCount),
                new MultithreadEventLoopGroup(serverConfig.WorkerThreadCount), serverConfig.IOThreadCount,//20170515陈慎远临时添加
                null,
                loggerFactory);
            //NettyServerConfig config = new NettyServerConfig(timer:new HashedWheelTimer(),
            //    bossGroup: new MultithreadEventLoopGroup(serverConfig.AcceptorThreadCount),
            //    ioThreadCount:1);


            _transport = new NettyServerTransport(def, config);
        }

        public State ServerState { get; private set; }

        public int Port
        {
            get { return _transport.Port; }
        }

        public Task StartAsync()
        {
            lock (_eventLocked)
            {
                _eventLocked.WaitOne();
                _eventLocked.Reset();
            }
            Guard.ArgumentCondition(this.ServerState != State.Closed, "Thrift server is closed");
            if (this.ServerState == State.NotStarted)
            {
                return this._transport.StartAsync()
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            _logger.LogError(default(EventId),
                                t.Exception,
                                $"Thrift Server startup failed !");
                        }
                        else
                        {
                            _logger.LogInformation("Thrift Server was started.");
                            this.ServerState = State.Running;
                        }
                        _eventLocked.Set();
                    });
            }
            else
            {
                _logger.LogWarning($"Repeated invocation of the {nameof(ThriftServer)}.{nameof(ThriftServer.StartAsync)} method.");
                _eventLocked.Set();
                return Task.FromResult(0);
            }
        }

        public Task CloseAsync(TimeSpan? timeout = null)
        {
            lock (_eventLocked)
            {
                _eventLocked.WaitOne();
                _eventLocked.Reset();
            }
            if (this.ServerState == State.Closed)
            {
                _eventLocked.Set();
                return Task.FromResult(0);
            }

            if (this.ServerState == State.Running)
            {
                return _transport.StopAsync(timeout).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogError(default(EventId),
                            t.Exception,
                            $"Thrift Server stop failed.");
                    }
                    else
                    {
                        _logger.LogInformation("Thrift Server was stopped.");
                        this.ServerState = State.Closed;
                    }
                    _eventLocked.Set();
                });
            }
            else
            {
                // Executors are created in the constructor, so we should shut them down here even if the
                // server was never actually started
                _logger.LogWarning($"Thrift server was never actually started ( {nameof(ThriftServer)}.{nameof(ThriftServer.CloseAsync)} method ).");
                this.ServerState = State.Closed;
                _eventLocked.Set();
                return Task.FromResult(0);
            }
        }

        public void Dispose()
        {
            if (this.ServerState != State.Closed)
            {
                this.CloseAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            }
        }

        public enum State
        {
            NotStarted,
            Running,
            Closed,
        }
    }
}
