
using System.Collections.Generic;

namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// 负载均衡的规则
    /// </summary>
    public interface IRule
    {
        Server Choose(ILoadBalancer loadBalancer);
    }
}
