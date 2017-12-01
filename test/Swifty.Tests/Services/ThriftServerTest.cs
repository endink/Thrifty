#if DEBUG
using DotNetty.Transport.Channels;
using Thrifty.Codecs;
using Thrifty.Nifty.Core;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;
using Xunit;

namespace Thrifty.Tests.Services
{
    using Thrifty.MicroServices.Server;

    [Collection("ThriftServerTest")]
    public class ThriftServerTest
    {
        [Fact(DisplayName = "ThriftServer: 启停测试")]
        public void ShutdownTest()
        {
            ServerTester creator = new ServerTester();
            using (var svr = creator.Invoke().Server)
            {
                creator.Start();
                creator.Stop();
                creator.CheckExecutorsTerminated();
            }
        }

        [Fact(DisplayName = "ThriftServer: 未启动直接停止测试")]
        public void ShutdownWithNoStartup()
        {
            ServerTester creator = new ServerTester();
            using (var svr = creator.Invoke().Server)
            {
                creator.Stop();
                creator.CheckExecutorsUnstarted();
            }
        }

        private class ServerTester
        {
            private IEventLoopGroup bossExecutor;
            private IEventLoopGroup ioWorkerExecutor;

            public ThriftServer Server { get; private set; }

            public ServerTester Invoke()
            {
                ThriftServiceProcessor processor = new ThriftServiceProcessor(loggerFactory: null, services: new SimpleService());

                bossExecutor = new MultithreadEventLoopGroup(1);
                ioWorkerExecutor = new MultithreadEventLoopGroup(1);

                ThriftServerDef serverDef = new ThriftServerDef(processor, new TBinaryProtocol.Factory());

                NettyServerConfig serverConfig = new NettyServerConfig(bossGroup: bossExecutor, workerGroup: ioWorkerExecutor);

                Server = new ThriftServer(serverConfig, serverDef);
                return this;
            }

            public void CheckExecutorsUnstarted()
            {
                Assert.Equal(TaskStatus.WaitingForActivation, bossExecutor.TerminationCompletion.Status);
                Assert.Equal(TaskStatus.WaitingForActivation, ioWorkerExecutor.TerminationCompletion.Status);
            }

            public void CheckExecutorsTerminated()
            {
                Assert.Equal(TaskStatus.RanToCompletion, bossExecutor.TerminationCompletion.Status);
                Assert.Equal(TaskStatus.RanToCompletion, ioWorkerExecutor.TerminationCompletion.Status);
            }

            public void Start()
            {
                Server.StartAsync().GetAwaiter().GetResult();
            }

            public void Stop()
            {
                Server.CloseAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            }
        }
    } 
}
#endif