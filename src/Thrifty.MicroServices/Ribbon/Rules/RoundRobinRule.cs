using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class RoundRobinRule : IRule
    {

        private volatile int _timer = 0;

        public Server Choose(ILoadBalancer loadBalancer)
        {
            var count = 0;
            while (count++ < 10)
            {
                var servers = loadBalancer.ReachableServers();
                var serversCount = servers?.Count ?? 0;
                if (serversCount == 0) return null;
                var server = servers[GetModulo(serversCount)];
                if (server == null)
                {
                    Thread.Sleep(5 * 1000);
                    continue;
                }
                if (server.IsAlive && server.ReadyToServe) return server;
            }
            return null;
        }


        private int GetModulo(int modulo)
        {
            while (true)
            {
                Interlocked.Add(ref _timer, 1);
                var current = _timer;
                var next = (current + 1) % modulo;
                //var value = Interlocked.CompareExchange(ref _timer, next, current);
                //if (value == current)
                return next;
            }
        }
    }
}
