using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class BestAvailableRule : IRule
    {
        private static readonly Random Random = new Random();
        private readonly IServerStatusCollector _collector;
        private RoundRobinRule _ribonRule;
        private RoundRobinRule RibonRule => _ribonRule ?? (_ribonRule = new RoundRobinRule());

        public BestAvailableRule(IServerStatusCollector collector)
        {
            _collector = collector;
        }

        public Server Choose(ILoadBalancer loadBalancer)
        {
            while (true)
            {
                if (_collector == null) return RibonRule.Choose(loadBalancer);
                var minimalActiveRequestsCount = long.MaxValue;
                var now = DateTime.Now;
                Server chosen = null;
                var servers = loadBalancer.ReachableServers();
                if (servers == null)
                {
                    Thread.Sleep(5 * 1000);
                    continue;
                }
                var statuses = (from s in servers let status = _collector.ServerStatus(s) where !status.IsCircuitBreakerTripped(now) select status).ToArray();
                foreach (var status in statuses)
                {
                    var activeRequestsCount = status.ActiveRequestsCount;
                    if (activeRequestsCount > minimalActiveRequestsCount) continue;
                    minimalActiveRequestsCount = activeRequestsCount;
                }
                if (minimalActiveRequestsCount == long.MaxValue) return null;
                var count = statuses.Count(x => x.ActiveRequestsCount == minimalActiveRequestsCount);
                chosen = count > 1 ? statuses[Random.Next(0, statuses.Length)].Server : statuses[0].Server;
                return chosen;
            }
        }
    }
}
