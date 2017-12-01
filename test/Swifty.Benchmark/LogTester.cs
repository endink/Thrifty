using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Thrifty.MicroServices.Client;
using Thrifty.MicroServices.Commons;
using Thrifty.MicroServices.Server;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Thrifty.Benchmark
{
    public abstract class LogTester
    {
        private ThriftyBootstrap _server;
        private ThriftyClient _client;
        private ISimpleCase _proxy;

        protected abstract int Port { get; }

        protected virtual String EurekaAddress { get; }

        protected virtual bool EnableConnectionPool { get; } = true;

        [GlobalSetup]
        public void OpenServer()
        {
            LoggerFactory loggerFac = new LoggerFactory();
#if DEBUG
            loggerFac.AddConsole(LogLevel.None);
#endif

            ThriftyServerOptions svrOptions = new ThriftyServerOptions
            {
                BindingAddress = "127.0.0.1",
                Port = this.Port
            };
            svrOptions.Eureka.EurekaServerServiceUrls = this.EurekaAddress;

            ThriftyClientOptions cltOptions = new ThriftyClientOptions()
            {
                ConnectionPoolEnabled = this.EnableConnectionPool
            };
            cltOptions.Eureka.EurekaServerServiceUrls = this.EurekaAddress;


            _server = new ThriftyBootstrap(
                  new DelegateServiceLocator((r, t) => new LogCase()),
                  svrOptions,
                  new InstanceDescription("test-case1", "test-case1"), loggerFac);
            _server.AddService<ISimpleCase>();

            _server.StartAsync().GetAwaiter().GetResult();
            _client = new ThriftyClient(cltOptions);
            _proxy = _client.Create<ISimpleCase>("127.0.0.1:6666");
        }

        [GlobalCleanup]
        public void CloseServer()
        {
            _client?.Dispose();
            _server?.ShutdownAsync().GetAwaiter().GetResult();
        }

        public virtual void RunLog() => RunTestCase(c => c.Log(new List<LogEntry>
        {
                new LogEntry { Category = "c1", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c2", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c3", Message = Guid.NewGuid().ToString() }
        }));
        
        public virtual void RunGetMessages() => RunTestCase(c => c.GetMessages());

        private void RunTestCase(Action<ISimpleCase> action)
        {
            action.Invoke(_proxy);
        }

    }
}
