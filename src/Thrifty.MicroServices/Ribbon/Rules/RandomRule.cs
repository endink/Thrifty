using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class RandomRule : IRule
    {
        private static readonly Random Random = new Random();

        public Server Choose(ILoadBalancer loadBalancer)
        {
            while (true)
            {
                var servers = loadBalancer.ReachableServers();
                var serversCount = servers?.Count ?? 0;
                if (serversCount == 0) return null;
                var server = servers[Random.Next(0, serversCount)];
                if (server == null)
                {
                    Thread.Sleep(5 * 1000);
                    continue;
                }
                if (server.IsAlive && server.ReadyToServe) return server;
            }
        }
    }
}
