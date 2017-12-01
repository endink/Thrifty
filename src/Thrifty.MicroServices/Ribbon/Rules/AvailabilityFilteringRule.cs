using System;
using System.Collections.Generic;
using System.Linq;

namespace Thrifty.MicroServices.Ribbon.Rules
{
    public class AvailabilityFilteringRule : FilterableRule, IRule
    {
        private class AvailabilityFilter : IFilter
        {
            private readonly IServerStatusCollector _collector;
            private static bool EnableCircuitBreakerFiltering = false;
            private static int MaxActiveRequestsCount = 200;
            private RoundRobinRule _ribonRule;
            private RoundRobinRule RibonRule => _ribonRule ?? (_ribonRule = new RoundRobinRule());

            private bool ShouldSkipServer(Server server)
            {
                if (_collector == null) return true;
                var status = _collector.ServerStatus(server);
                return (EnableCircuitBreakerFiltering && status.IsCircuitBreakerTripped(DateTime.Now))
                       || status.ActiveRequestsCount > MaxActiveRequestsCount;
            }
            public AvailabilityFilter(IServerStatusCollector collector)
            {
                _collector = collector;
            }

            public IEnumerable<Server> Filtration(IEnumerable<Server> servers) => servers.Where(x => !ShouldSkipServer(x)).ToArray();
        }

        public AvailabilityFilteringRule(IServerStatusCollector collector) : base(new AvailabilityFilter(collector))
        {

        }
    }
}
