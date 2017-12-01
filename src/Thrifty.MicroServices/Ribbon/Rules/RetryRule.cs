using System;
using System.Threading;
namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class RetryRule : IRule
    {
        private readonly IRule _subRule;
        private static readonly int MaxRetryMilliseconds = 500;
        private static readonly int MaxRetryCount = 100;
        private static CancellationToken? _cancellationToken;
        public RetryRule(IRule subRule)
        {
            _subRule = subRule ?? new RoundRobinRule();
        }
        public RetryRule(IRule subRule, CancellationToken cancellationToken)
        {
            _subRule = subRule ?? new RoundRobinRule();
            _cancellationToken = cancellationToken;
        }
        public Server Choose(ILoadBalancer loadBalancer)
        {
            var until = DateTime.Now.AddMilliseconds(MaxRetryMilliseconds);
            Server server = null;
            while ((server == null || !server.IsAlive) && DateTime.Now < until)
            {
                if (_cancellationToken.HasValue)
                {
                    while (!_cancellationToken.Value.IsCancellationRequested)
                    {
                        server = _subRule.Choose(loadBalancer);
                        if ((server == null || !server.IsAlive) && DateTime.Now < until)
                        {
                            Thread.Sleep(5 * 1000);
                            continue;
                        }
                        break;
                    }
                }
                else
                {
                    var count = 0;
                    while (count++ < MaxRetryCount)
                    {
                        server = _subRule.Choose(loadBalancer);
                        if ((server == null || !server.IsAlive) && DateTime.Now < until)
                        {
                            Thread.Sleep(5 * 1000);
                            continue;
                        }
                        break;
                    }
                }
            }
            return (server == null || !server.IsAlive) ? null : server;
        }
    }
}
