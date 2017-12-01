using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;

namespace Thrifty.Nifty.Core
{
    /// <summary>
    /// 支持多个 Thrift 服务的 Netty 启动器。
    /// 单个服务考虑使用 <see cref="NettyServerTransport.StartAsync()"/> 方法启动。
    /// </summary>
    public sealed class NiftyBootstrap
    {
        private readonly NettyServerConfig _nettyServerConfig;
        private readonly Dictionary<ThriftServerDef, NettyServerTransport> _transports;
        private IEventLoopGroup _bossExecutor;
        private IEventLoopGroup _workerExecutor;

        public NiftyBootstrap(
                IEnumerable<ThriftServerDef> thriftServerDefs,
                NettyServerConfig nettyServerConfig)
        {
            Guard.ArgumentNotNull(thriftServerDefs, nameof(nettyServerConfig));
            Guard.ArgumentNotNull(nettyServerConfig, nameof(nettyServerConfig));
            _transports = new Dictionary<ThriftServerDef, NettyServerTransport>();
            this._nettyServerConfig = nettyServerConfig;
            foreach (ThriftServerDef thriftServerDef in thriftServerDefs)
            {
                _transports.Add(thriftServerDef, new NettyServerTransport(thriftServerDef,
                        nettyServerConfig));
            }
        }

        public void Start()
        {
            _bossExecutor = _nettyServerConfig.BossGroup;
            _workerExecutor = _nettyServerConfig.WorkerGroup;
            ServerBootstrap serverBootstrap = new ServerBootstrap();
            serverBootstrap.Option(ChannelOption.TcpNodelay, true);
            serverBootstrap.Group(_bossExecutor, _workerExecutor);
            
            List<Task> tasks = new List<Task>();
            foreach (NettyServerTransport transport in _transports.Values)
            {
                var t = transport.StartAsync(serverBootstrap); 
                tasks.Add(t);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }
        }

        public void Stop(TimeSpan? timeout)
        {
            List<Task> tasks = new List<Task>();
            foreach (NettyServerTransport transport in _transports.Values)
            {
                 var t = transport.StopAsync();
                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
            ShutdownUtil.ShutdownChannelAsync(_bossExecutor, _workerExecutor, null, timeout ?? TimeSpan.FromSeconds(15));
        }

        public IEnumerable<KeyValuePair<ThriftServerDef, int>> BoundPorts()
        {
            Dictionary<ThriftServerDef, int> builder = new Dictionary<ThriftServerDef, int>();
            foreach (var entry in _transports)
            {
                builder.Add(entry.Key, entry.Value.Port);
            }
            return builder;
        }

        //public Map<ThriftServerDef, NiftyMetrics> getNiftyMetrics()
        //{
        //    ImmutableMap.Builder<ThriftServerDef, NiftyMetrics> builder = new ImmutableMap.Builder<>();
        //    for (Map.Entry<ThriftServerDef, NettyServerTransport> entry : transports.entrySet())
        //    {
        //        builder.put(entry.getKey(), entry.getValue().getMetrics());
        //    }
        //    return builder.build();
        //}
    }
}
