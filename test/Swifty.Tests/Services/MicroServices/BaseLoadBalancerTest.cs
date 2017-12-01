
using System.Threading.Tasks;
using Thrifty.MicroServices.Ribbon;
using Xunit;

namespace Thrifty.Tests.Services.MicroServices
{
    public class BaseLoadBalancerTest : BaseTest
    {
        [Fact(DisplayName = "LoadBalancerBuilder测试")]
        public void Test()
        {
            //使用ribbon规则和基于http的ping的一个负载
            var balancer = LoadBalancerBuilder.NewBuilder()
                .WithPing(Ping)
                .WithLoggerFactory(LoggerFactory)
                .WithRule(new Thrifty.MicroServices.Ribbon.Rules.RoundRobinRule()).Build("demo", 1 * 1000);
            //添加需要被负载的服务器
            balancer.AddServers(Servers);
            //按照负载规则选择一台服务器
            var server = balancer.Choose();
            Assert.NotNull(server);
            Assert.True(server.Port % 2 == 0);
        }
        [Fact(DisplayName = "LoadBalancerCommand 的Submit测试")]
        public async void CommandTest()
        {
            var balancer = LoadBalancerBuilder.NewBuilder()
                .WithPing(Ping)
                .WithRule(new Thrifty.MicroServices.Ribbon.Rules.RoundRobinRule()).Build("demo", 1 * 1000);
            balancer.AddServers(Servers);
            var command = new LoadBalancerCommand(balancer, null, null, null);
            var x = await command.Submit(s => Task.FromResult(Ping.IsAlive(s)));
            Assert.True(x);
        }
        [Fact(DisplayName = "WeightedResponseTimeRule测试")]
        public async void WeightedResponseTimeRuleTest()
        {
            var collector = new DefaultServerStatusCollector();
            var accumulater = new DefaultServerWeightAccumulater(collector);
            var rule = new Thrifty.MicroServices.Ribbon.Rules.WeightedResponseTimeRule(accumulater);
            var balancer = new BaseLoadBalancer(1 * 1000, "demo", rule, Ping, new SerialPingStrategy(), LoggerFactory);
            accumulater.LoadBalancer = balancer;
            balancer.AddServers(Servers);
            var command = new LoadBalancerCommand(balancer, null, null, null);
            const int count = 1000;
            var index = 0;
            while (index++ < count)
            {
                Assert.True(await command.Submit(server => Task.FromResult(Ping.IsAlive(server))));
            }
        }
    }
}
