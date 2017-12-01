namespace Thrifty.MicroServices.Ribbon
{
    public class LoadBalancerContext
    {
        public int MaxAutoRetriesNextServer { get; }
        public int MaxAutoRetries { get; }
        public ILoadBalancer LoadBalancer { get; }
        public string ClinetName { get; }
        public IRetryHandler RetryHandler { get; }
        public bool RetryOnAllOperations { get; }
        public string VipAddress { get; }

        public LoadBalancerContext(ILoadBalancer loadBalancer, string vipAddress)
        {
            ClinetName = "default";
            MaxAutoRetriesNextServer = 1;
            MaxAutoRetries = 0;
            RetryHandler = new DefaultRetryHandler();
            RetryOnAllOperations = false;
            VipAddress = vipAddress;
            LoadBalancer = loadBalancer;
        }

        public LoadBalancerContext(ILoadBalancer loadBalancer, IRetryHandler retryHandler, string vipAddress)
            : this(loadBalancer, vipAddress)
        {
            RetryHandler = retryHandler;
        }
    }
}
