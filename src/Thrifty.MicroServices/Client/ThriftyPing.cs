using Thrifty.MicroServices.Commons;
using Thrifty.MicroServices.Ribbon;
using Thrifty.Services;

namespace Thrifty.MicroServices.Client
{
    internal class ThriftyPing : IPing
    {
        private readonly ThriftClientManager _thriftClientManager;
        internal ThriftyPing(ThriftClientManager thriftClientManager)
        {
            _thriftClientManager = thriftClientManager;
        }

        public bool IsAlive(Ribbon.Server server)
        {
            var discoveryEnabledServer = server as DiscoveryEnabledServer;
            var host = discoveryEnabledServer == null ? server.Host : discoveryEnabledServer.InstanceInfo.IpAddr;
            var port = discoveryEnabledServer?.Port ?? server.Port;
            var checker = _thriftClientManager.CreateClientAsync<IHealthCheck>(host, port).GetAwaiter().GetResult();
            var x = checker.Ping();
            return x == (byte)1;
        }
    }
}
