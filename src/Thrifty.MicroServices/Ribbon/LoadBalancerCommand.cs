using System;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon
{
    public class LoadBalancerCommand
    {
        private readonly Server _server;
        public IRetryHandler RetryHandler { get; }
        public IServerStatusCollector Collector { get; }
        public ILoadBalancer LoadBalancer { get; }
        public LoadBalancerCommand(ILoadBalancer loadBalancer, IServerStatusCollector collector, IRetryHandler retryHandler, Server server)
        {
            RetryHandler = retryHandler;
            LoadBalancer = loadBalancer;
            Collector = collector ?? new DefaultServerStatusCollector();
            _server = server;
        }

        public event ExecutionStartEventHandler OnExecutionStart;
        public event ChooseServerErrorEventHandler OnChooseServerError;
        public event ServerReadyEventHandler OnServerReady;
        public event ExecutingExceptionEventHandler OnExecutingException;
        public event ExecutionSuccessEventHandler OnExecutionSuccess;
        private async Task<T> InnerSubmit<T>(Func<Server, Task<T>> operation, int retryCount, bool same)
        {
            try
            {
                OnExecutionStart?.Invoke(this);
                var begin = DateTime.Now;
                Server server = null;
                server = _server ?? LoadBalancer.Choose();
                if (server == null) throw new NoServerFoundException("No service instances available in eureka server");
                try
                {
                    Collector.IncreaseOpenConntionsCount(server);
                    OnServerReady?.Invoke(this, new ServerReadyEventArgs(server));
                    Collector.IncrementActiveRequestsCount(server);
                    var result = operation(server).GetAwaiter().GetResult();
                    var end = DateTime.Now;
                    OnExecutionSuccess?.Invoke(this, new ExecutingSuccessEventArgs(result));
                    Collector.DecrementActiveRequestsCount(server);
                    Collector.IncrementRequestCount(server);
                    Collector.RecordResponseTime((long)(end - begin).TotalMilliseconds, server);
                    Collector.ClearSuccessiveConnectionFailureCount(server);
                    return result;
                }
                catch (Exception exception)
                {
                    var end = DateTime.Now;
                    OnExecutingException?.Invoke(this, new ExecutingExceptionEventArgs(exception));
                    Collector.DecrementActiveRequestsCount(server);
                    Collector.IncrementRequestCount(server);
                    Collector.RecordResponseTime((long)(end - begin).TotalMilliseconds, server);
                    if (RetryHandler.IsCircuitTrippingException(exception))
                    {
                        Collector.IncreaseServerFailureCount(server);
                        Collector.IncreaseSuccessiveConnectionFailureCount(server);
                    }
                    else
                    {
                        Collector.ClearSuccessiveConnectionFailureCount(server);
                        if (RetryHandler.IsRetriableException(exception, same))
                        {
                            if (same)
                            {
                                if (retryCount < RetryHandler.MaxRetriesOnSameServer)
                                {
                                    return await InnerSubmit(operation, retryCount + 1, true);
                                }
                                
                                return await InnerSubmit<T>(operation, 0, false);
                            }
                            if (retryCount < RetryHandler.MaxRetriesOnNextServer)
                            {
                                return await InnerSubmit<T>(operation, retryCount + 1, false);
                            }
                        }
                    }
                    throw;
                }
            }
            catch (Exception exception)
            {
                OnChooseServerError?.Invoke(this,
                    new ChooseServerErrorEventArgs(exception));
                throw;
            }
        }

        public async Task<T> Submit<T>(Func<Server, Task<T>> operation) => await InnerSubmit<T>(operation, 0, true);
    }
}
