using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Nifty.Ssl;

namespace Thrifty.Nifty.Core
{
    public class NettyServerConfig
    {
        public NettyServerConfig(
                                 int bossThreadCount,
                                 int workerThreadCount,
                                  int? ioThreadCount = null,
                                 ITimer timer = null,
                                 IDictionary<ChannelOption, Object> bootstrapOptions = null,
                                 ILoggerFactory loggerFactory = null)
            :this(timer,
                 new MultithreadEventLoopGroup(bossThreadCount),
                 new MultithreadEventLoopGroup(workerThreadCount),
                 ioThreadCount,
                 bootstrapOptions,
                 loggerFactory)
        {
           
        }

        public NettyServerConfig(
                                 ITimer timer = null,
                                 IEventLoopGroup bossGroup = null,
                                 IEventLoopGroup workerGroup = null,
                                 int? ioThreadCount = null,
                                 IDictionary<ChannelOption, Object> bootstrapOptions = null,
                                 ILoggerFactory loggerFactory = null)
        {
            this.BootstrapOptions = bootstrapOptions ?? new Dictionary<ChannelOption, Object>();
            this.Timer = timer ?? new HashedWheelTimer(TimeSpan.FromMilliseconds(100), 512, -1);
            this.BossGroup = bossGroup ?? new MultithreadEventLoopGroup(1);
            this.WorkerGroup = workerGroup ?? new MultithreadEventLoopGroup();
            this.LoggerFactory = loggerFactory;
            this.IOThreadCount = ioThreadCount;
        }

        public int? IOThreadCount { get; }
        
        public ILoggerFactory LoggerFactory { get; }

        public ITimer Timer { get; }

        public IEventLoopGroup BossGroup { get; }

        public IEventLoopGroup WorkerGroup { get; }

        public IDictionary<ChannelOption, Object> BootstrapOptions { get;}
    }
}
