using System;

namespace Thrifty.MicroServices.Commons
{
    [ThriftService(HealthCheckIdentifier.ServiceName)]
    public interface IHealthCheck
    {
        [ThriftMethod(HealthCheckIdentifier.PingMethodName)]
        byte Ping();
    } 
}
