using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// 负载均衡构建器
    /// </summary>
    public sealed class LoadBalancerBuilder
    {
        private LoadBalancerBuilder() { }
        private LoadBalancerBuilder SetField<F>(F field, Action<LoadBalancerBuilder, F> setter)
        {
            setter(this, field);
            return this;
        }

        public static LoadBalancerBuilder NewBuilder() => new LoadBalancerBuilder();

        private IRule _rule;
        private IPing _ping;
        private IPingStrategy _pingStrategy;
        private ILoggerFactory _loggerFactory;
        public LoadBalancerBuilder WithLoggerFactory(ILoggerFactory factory) => SetField(factory, (b, f) => b._loggerFactory = f);
        public LoadBalancerBuilder WithRule(IRule rule) => SetField(rule, (b, f) => b._rule = f);
        public LoadBalancerBuilder WithPing(IPing ping) => SetField(ping, (b, f) => b._ping = f);
        public LoadBalancerBuilder WithPingStrategy(IPingStrategy pingStrategy) => SetField(pingStrategy, (b, f) => b._pingStrategy = f);

        public ILoadBalancer Build(string name, int pingInterval)
        {
            if (_ping == null) throw new ThriftyException($"{nameof(LoadBalancerBuilder)} missing {nameof(IPing)} configuration");
            return new BaseLoadBalancer(pingInterval, name, _rule ?? new Rules.RoundRobinRule(), _ping, _pingStrategy ?? new SerialPingStrategy(), _loggerFactory);
        }
    }
}
