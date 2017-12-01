using System;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class WeightedResponseTimeRule : IRule
    {
        private static readonly Random Random = new Random();
        private readonly IServerWeightAccumulater _accumulater;
        private RoundRobinRule _ribonRule;
        private RoundRobinRule RibonRule => _ribonRule ?? (_ribonRule = new RoundRobinRule());

        public WeightedResponseTimeRule(IServerWeightAccumulater accumulater)
        {
            if (accumulater == null) throw new ArgumentNullException(nameof(accumulater));
            _accumulater = accumulater;
        }

        public Server Choose(ILoadBalancer loadBalancer)
        {
            Server server = null;
            while (true)
            {
                var currentWeights = _accumulater.AccumulatedWeights;
                var servers = loadBalancer.ReachableServers();
                var serversCount = servers?.Count ?? 0;
                if (serversCount == 0) return null;
                var length = currentWeights?.Length ?? 0;
                var maxTotalWeight = length == 0 ? 0 : currentWeights[length - 1];
                if (maxTotalWeight < 0.001)
                {
                    server = RibonRule.Choose(loadBalancer);
                    if (server != null) return server;
                }
                else
                {
                    var randomWeight = Random.NextDouble() * maxTotalWeight;
                    for (var i = 0; i < length; i++)
                    {
                        var weight = currentWeights[i];
                        if (!(weight > randomWeight)) continue;
                        server = servers[i];
                        break;
                    }
                }
                if (server == null)
                {
                    Thread.Sleep(5 * 1000);
                    continue;
                }
                if (server.IsAlive) return server;
                server = null;
            }
        }
    }
}
