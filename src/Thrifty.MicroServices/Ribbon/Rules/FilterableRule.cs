using System;
using System.Linq;
using System.Threading;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class FilterableRule : IRule
    {
        private static readonly Random Random = new Random();
        private readonly IFilter _filter;

        public FilterableRule(IFilter filter)
        {
            if (filter == null) throw new NullReferenceException(nameof(filter));
            _filter = filter;
        }
        public Server Choose(ILoadBalancer loadBalancer)
        {
            while (true)
            {
                var servers = _filter.Filtration(loadBalancer.ReachableServers()).ToArray();
                var serversCount = servers?.Length ?? 0;
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
