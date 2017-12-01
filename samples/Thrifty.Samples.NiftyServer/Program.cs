using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka;
using Thrifty.MicroServices.Commons;
using Thrifty.MicroServices.Server;
using Thrifty.Nifty.Ssl;
using Thrifty.Samples.Thrifty;
using Thrifty.Services;
using System;
using System.Reflection;

namespace Thrifty.Samples
{
    public class Program
    {
        private const string PublicAddress = "10.66.10.166";
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            StartThriftyServer(args?.Length == 1 ? int.Parse(args[0]) : 9999, ""/*,"http://10.66.4.68:8761/eureka"*/);
            Console.Read();
        }

        private static void StartThriftyServer(int port, string eurekaServer)
        {
            var factory = new LoggerFactory();
            factory.AddConsole(LogLevel.Debug);
            var serverConfig = new ThriftyServerOptions
            {
                QueueTimeout = TimeSpan.FromMinutes(1),
                TaskExpirationTimeout = TimeSpan.FromMinutes(1)
            };


            var bootStrap = new ThriftyBootstrap(new object[] { new ScribeTest() },
                serverConfig, new InstanceDescription("SampleApp", "TestApp122", PublicAddress), factory);

            bootStrap
                .SslConfig(new SslConfig
                {
                    CertFile = "server.pfx",
                    CertPassword = "1qaz@WSX",
                    CertFileProvider = new EmbeddedFileProvider(typeof(Program).GetTypeInfo().Assembly)
                })
               .AddService(typeof(IScribe), "0.0.1")
               .EurekaConfig(!String.IsNullOrWhiteSpace(eurekaServer), new EurekaClientConfig { EurekaServerServiceUrls = eurekaServer })
               .Bind(PublicAddress, port)
               .StartAsync();
        }

        private static void StartServerByAnnotaions()
        {
            var factory = new LoggerFactory();
            factory.AddConsole(LogLevel.Debug);
            var processor = new ThriftServiceProcessor(factory, new Thrifty.ScribeTest());
            var config = new ThriftServerConfig()
            {
                BindingAddress = "0.0.0.0",
                Port = 9999,
                IdleConnectionTimeout = TimeSpan.FromMinutes(10),
                QueueTimeout = TimeSpan.FromMinutes(10),
                TaskExpirationTimeout = TimeSpan.FromMinutes(10)
            };
            var server = new ThriftServer(processor, config, loggerFactory: factory);
            server.StartAsync();
        }

        private static void StartThriftyServer(int port)
        {
            var factory = new LoggerFactory();
            factory.AddConsole(LogLevel.Debug);
            var serverConfig = new ThriftyServerOptions();

            var bootStrap = new ThriftyBootstrap(new DelegateServiceLocator(
                (ctx, x) => new Thrifty.ScribeTest()), serverConfig, new InstanceDescription("SampleApp",
                "TestApp122", PublicAddress), factory);

            var future = bootStrap
                .AddService(typeof(Thrifty.IScribe), "0.0.1")
                .EurekaConfig(true, new EurekaClientConfig { EurekaServerServiceUrls = "http://10.66.4.68:8761/eureka" })
                .Bind("localhost", port)
                .StartAsync();
        }

        //private static void StartServerFromIdl()
        //{
        //    var serverDefs = new List<ThriftServerDef>();
        //    var p = new Scribe.Processor(new ScribeTest());
        //    var fac = new TBinaryProtocol.Factory();
        //    var def = new ThriftServerDef(p, fac, serverPort: 45678);

        //    serverDefs.Add(def);

        //    var loggerFactory = new LoggerFactory();
        //    loggerFactory.AddConsole(LogLevel.Debug);
        //    var nettyConfig = new NettyServerConfig(loggerFactory: loggerFactory);

        //    var boot = new NiftyBootstrap(serverDefs, nettyConfig);

        //    boot.Start();

        //    Console.WriteLine($"已在 {def.ServerPort} 端口启动了Thrift服务。");
        //}

        //private class ScribeTest : Scribe.Iface
        //{

        //    public List<LogEntry> getMessages()
        //    {
        //        return new List<LogEntry>
        //        {
        //            new LogEntry { Message = "message A", Category = "C1" },
        //            new LogEntry { Message = "message B", Category = "C2" }
        //        };
        //    }

        //    public ResultCode Log(List<LogEntry> messages)
        //    {
        //        return ResultCode.OK;
        //    }
        //}
    }
}
