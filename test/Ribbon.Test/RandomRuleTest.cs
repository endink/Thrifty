using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Thrifty.MicroServices.Ribbon;
using Thrifty.MicroServices.Ribbon.Rules;
using Thrifty.Tests.Services.MicroServices;

namespace Ribbon.Test
{
    internal class RandomRuleTest : Test
    {
        public RandomRuleTest() : base("RandomRule", new RandomRule())
        {
        }
    }
    internal class RoundRobinRuleTest : Test
    {
        public RoundRobinRuleTest() : base("RoundRobinRule", new RoundRobinRule())
        {
        }
    }

    internal class AvailabilityFilteringRuleTest : BaseTest
    {
        private readonly LoadBalancerCommand _command;
        private readonly IPing _ping;
        private readonly ILoadBalancer _loadBalancer;
        public AvailabilityFilteringRuleTest()
            : base("AvailabilityFilteringRule")
        {
            _ping = new EasyHttpPing(_factory, 20);
            var collector = new DefaultServerStatusCollector();
            var rule = new AvailabilityFilteringRule(collector);
            _loadBalancer = new BaseLoadBalancer(1 * 1000,base._name, rule, _ping, new SerialPingStrategy(), _factory);
            _command = new LoadBalancerCommand(_loadBalancer, collector, null, null);
        }
        private readonly IList<Server> errors = new List<Server>();
        protected override Server Choose()
        {
            return _command.Submit(server =>
          {
              var alive = _ping.IsAlive(server);
              if (!alive)
              {
                  errors.Add(server);
              }
              return Task.FromResult(server);
          }).GetAwaiter().GetResult();
        }

        public override string ToString()
        {
            return string.Join(",", errors);
        }

        protected override void AddServers(IList<Server> servers) => _loadBalancer.AddServers(servers);
    }

    internal class BestAvailableRuleTest : BaseTest
    {
        private IList<Server> errors = new List<Server>();
        private readonly LoadBalancerCommand _command;
        private readonly ILoadBalancer _loadBalancer;
        private readonly IPing _ping;
        public BestAvailableRuleTest()
            : base("BestAvailableRule")
        {
            _ping = new EasyHttpPing(_factory, 20);
            var collector = new DefaultServerStatusCollector();
            var rule = new BestAvailableRule(collector);
            _loadBalancer = new BaseLoadBalancer(1 * 1000, base._name, rule, _ping, new SerialPingStrategy(), _factory);
            _command = new LoadBalancerCommand(_loadBalancer, collector, null, null);
        }

        protected override Server Choose()
        {
            return _command.Submit(server =>
            {
                var alive = _ping.IsAlive(server);
                if (!alive)
                {
                    errors.Add(server);
                }
                return Task.FromResult(server);
            }).GetAwaiter().GetResult();
        }
        public override string ToString()
        {
            return string.Join(",", errors);
        }
        protected override void AddServers(IList<Server> servers) => _loadBalancer.AddServers(servers);
    }

    internal class WeightedResponseTimeRuleTest : BaseTest
    {
        private IList<Server> errors = new List<Server>();
        private readonly LoadBalancerCommand _command;
        private readonly ILoadBalancer _loadBalancer;
        private readonly IPing _ping;
        public WeightedResponseTimeRuleTest()
            : base("WeightedResponseTimeRule")
        {
            _ping = new EasyHttpPing(_factory, 20);
            var collector = new DefaultServerStatusCollector();
            var accumulater = new DefaultServerWeightAccumulater(collector);
            var rule = new WeightedResponseTimeRule(accumulater);
            _loadBalancer = new BaseLoadBalancer(1 * 1000, base._name, rule, _ping, new SerialPingStrategy(), _factory);
            _command = new LoadBalancerCommand(_loadBalancer, collector, null, null);
        }

        protected override Server Choose()
        {
            return _command.Submit(server =>
            {
                var alive = _ping.IsAlive(server);
                if (!alive)
                {
                    errors.Add(server);
                }
                return Task.FromResult(server);
            }).GetAwaiter().GetResult();
        }
        public override string ToString()
        {
            return string.Join(",", errors);
        }
        protected override void AddServers(IList<Server> servers) => _loadBalancer.AddServers(servers);
    }


}
