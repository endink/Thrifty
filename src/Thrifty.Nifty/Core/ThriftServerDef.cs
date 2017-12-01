using DotNetty.Common.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.Nifty.Codecs;
using Thrifty.Nifty.Duplex;
using Thrifty.Nifty.Processors;
using Thrifty.Nifty.Ssl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Protocol;

namespace Thrifty.Nifty.Core
{
    public class ThriftServerDef
    {
        public const long DefaultMaxFrameSize = 64 * 1024 * 1024;
        private static int _globalId = 0;

        public ThriftServerDef(
                TProcessor processor,
                TProtocolFactory protocolFactory,
                String name = null,
                String host = null,
                int serverPort = 8080,
                long maxFrameSize = DefaultMaxFrameSize,
                int queuedResponseLimit = 16,
                int maxConnections = int.MaxValue,
                IThriftFrameCodecFactory thriftFrameCodecFactory = null,
                TimeSpan? clientIdleTimeout = null,
                TimeSpan? taskTimeout = null,
                TimeSpan? queueTimeout = null, 
                SslConfig sslConfig = null)
            : this(
                  NiftyProcessorAdapters.FactoryFromTProcessor(processor),
                  protocolFactory,
                 name,
                 host,
                 serverPort,
                 maxFrameSize,
                 queuedResponseLimit,
                 maxConnections,
                 thriftFrameCodecFactory,
                 clientIdleTimeout,
                 taskTimeout,
                 queueTimeout,
                 sslConfig)
        {

        }

        public ThriftServerDef(
            INiftyProcessor processor,
            TProtocolFactory protocolFactory,
                String name = null,
                String host = null,
                int serverPort = 8080,
                long maxFrameSize = DefaultMaxFrameSize,
                int queuedResponseLimit = 16,
                int maxConnections = int.MaxValue,
                IThriftFrameCodecFactory thriftFrameCodecFactory = null,
                TimeSpan? clientIdleTimeout = null,
                TimeSpan? taskTimeout = null,
                TimeSpan? queueTimeout = null,
                SslConfig sslConfig = null)
            : this((processor == null ? null : new DelegateNiftyProcessorFactory(t => processor)),
                  protocolFactory,
                 name,
                 host,
                 serverPort,
                 maxFrameSize,
                 queuedResponseLimit,
                 maxConnections,
                 thriftFrameCodecFactory,
                 clientIdleTimeout,
                 taskTimeout,
                 queueTimeout,
                 sslConfig)
        {

        }

        public ThriftServerDef(
               INiftyProcessorFactory processorFactory,
               TProtocolFactory protocolFactory,
               String name = null,
               String host = null,
               int serverPort = 5858,
               long maxFrameSize = DefaultMaxFrameSize,
               int queuedResponseLimit = 16,
               int maxConnections = int.MaxValue,
               IThriftFrameCodecFactory thriftFrameCodecFactory = null,
               TimeSpan? clientIdleTimeout = null,
               TimeSpan? taskTimeout = null,
               TimeSpan? queueTimeout = null,
               SslConfig sslConfig = null
               )
            :this(processorFactory, 
                 TDuplexProtocolFactory.FromSingleFactory(protocolFactory),
                 name,
                 host,
                 serverPort,
                 maxFrameSize, 
                 queuedResponseLimit,
                 maxConnections,
                 thriftFrameCodecFactory,
                 clientIdleTimeout,
                 taskTimeout,
                 queueTimeout,
                 sslConfig)
        {

        }

        public ThriftServerDef(
                INiftyProcessorFactory processorFactory,
                TDuplexProtocolFactory protocolFactory,
                String name = null,
                String host = null,
                int serverPort = 5858,
                long maxFrameSize = DefaultMaxFrameSize,
                int queuedResponseLimit = 16,
                int maxConnections = int.MaxValue,
                IThriftFrameCodecFactory thriftFrameCodecFactory = null,
                TimeSpan? clientIdleTimeout = null,
                TimeSpan? taskTimeout = null,
                TimeSpan? queueTimeout = null,
                SslConfig sslConfig = null
                )
        {
            Guard.ArgumentNotNull(protocolFactory, nameof(protocolFactory));

            this.Name = String.IsNullOrWhiteSpace(name) ? ($"nifty-{Interlocked.Increment(ref _globalId)}") : name;
            this.Host = host;
            this.ServerPort = serverPort;
            this.MaxFrameSize = maxFrameSize;
            this.MaxConnections = maxConnections;
            this.QueuedResponseLimit = queuedResponseLimit;
            this.ProcessorFactory = processorFactory;
            this.DuplexProtocolFactory = protocolFactory;
            this.ClientIdleTimeout = clientIdleTimeout ?? TimeSpan.Zero;
            this.TaskTimeout = taskTimeout ?? TimeSpan.Zero;
            this.QueueTimeout = queueTimeout ?? TimeSpan.Zero;
            this.ThriftFrameCodecFactory = thriftFrameCodecFactory ?? new DefaultThriftFrameCodecFactory();
            //this.Executor = executor ?? new DotNetty.Common.Concurrency.SingleThreadEventExecutor;
            //this.SecurityFactory = securityFactory;
            this.SslConfig = sslConfig;
        }

        public String Host { get; }

        public int ServerPort { get; }
        public long MaxFrameSize { get; }
        public int MaxConnections { get; }
        public int QueuedResponseLimit { get; }
        public INiftyProcessorFactory ProcessorFactory { get; }
        public TDuplexProtocolFactory DuplexProtocolFactory { get; }

        public TimeSpan ClientIdleTimeout { get; set; }
        public TimeSpan TaskTimeout { get; set; }
        public TimeSpan QueueTimeout { get; set; }

        public IThriftFrameCodecFactory ThriftFrameCodecFactory { get; }
        public String Name { get; }
        //public INiftySecurityFactory SecurityFactory { get; }
        public SslConfig SslConfig { get; private set; }

    }
}
