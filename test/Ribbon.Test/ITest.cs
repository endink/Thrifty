using System;
using System.Collections.Generic;
using Thrifty.MicroServices.Ribbon;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrifty.Tests.Services.MicroServices;

namespace Ribbon.Test
{
    internal class ServerHit
    {
        public Server Server { get; set; }
        public int HitCount { get; set; }
    }

    internal interface ITest
    {
        string Name { get; }
        ServerHit[] Run(int count, IList<Server> servers);
    }

    internal abstract class BaseTest : ITest
    {
        protected readonly string _name;
        protected readonly ILoggerFactory _factory;
        protected BaseTest(string name)
        {
            _factory = new LoggerFactory();
            _name = name;
        }

        public string Name => _name;

        protected abstract Server Choose();

        protected abstract void AddServers(IList<Server> servers);
        public ServerHit[] Run(int count, IList<Server> servers)
        {
            if (count < 100 * 100) count = 100 * 100;
            const int maxCount = 10;
            var taskCount = count / maxCount;
            AddServers(servers);
            var dictionary = new ConcurrentDictionary<Server, int>();
            Parallel.For(0, maxCount, _ =>
            {
                for (var i = 0; i < taskCount; i++)
                {
                    var server = Choose();
                    dictionary.AddOrUpdate(server, s => 0, (s, c) => c + 1);
                }
            });
            return (from x in dictionary select new ServerHit { HitCount = x.Value, Server = x.Key }).ToArray();
        }
    }

    internal abstract class Test : BaseTest, ITest
    {
        private readonly ILoadBalancer _loadBalancer;
        protected Test(string name, IRule rule) : base(name)
        {
            _loadBalancer = new BaseLoadBalancer(1 * 1000, name, rule, new EasyHttpPing(_factory), new SerialPingStrategy(), _factory);
        }

        protected override Server Choose() => _loadBalancer.Choose();
        protected override void AddServers(IList<Server> servers) => _loadBalancer.AddServers(servers);
    }
}
