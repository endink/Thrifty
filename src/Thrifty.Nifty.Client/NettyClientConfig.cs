using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Thrifty.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Thrifty.Nifty.Ssl;

namespace Thrifty.Nifty.Client
{
    public class NettyClientConfig
    {
        private readonly IDictionary<IConstant, Object> _bootstrapOptions;

        public NettyClientConfig(IDictionary<IConstant, Object> bootstrapOptions = null,
                                 ITimer timer = null,
                                 IEventLoopGroup workerExecutor = null)
        {
            this._bootstrapOptions = bootstrapOptions ?? new Dictionary<IConstant, Object>();
            //this.DefaultSocksProxyAddress = defaultSocksProxyAddress;
            this.Timer = timer ?? new HashedWheelTimer();
            //this.BossExecutor = bossExecutor ?? new MultithreadEventLoopGroup(1);
            this.WorkerExecutor = workerExecutor ?? new MultithreadEventLoopGroup();
            //this.sslClientConfiguration = sslClientConfiguration;
        }

        //public IEventExecutorGroup BossExecutor { get; }

        /// <summary>
        /// 获取 Sock代理，当前版本无效，总是返回 null.
        /// </summary>
        public IPEndPoint DefaultSocksProxyAddress { get; }

        public ITimer Timer { get; }

        public IEventLoopGroup WorkerExecutor { get; }
    }
}
