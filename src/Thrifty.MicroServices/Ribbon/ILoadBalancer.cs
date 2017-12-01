using System.Collections.Generic;

namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// 负载均衡
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// 添加服务器
        /// </summary>
        /// <param name="servers"></param>
        void AddServers(IList<Server> servers);
        /// <summary>
        /// 选择一个服务器
        /// </summary>
        /// <returns></returns>
        Server Choose();
        /// <summary>
        /// 下线服务器
        /// </summary>
        /// <param name="server"></param>
        void MarkServerDown(Server server);
        /// <summary>
        /// 可达的服务器
        /// </summary>
        /// <returns></returns>
        IList<Server> ReachableServers();
        /// <summary>
        /// 全部服务器
        /// </summary>
        /// <returns></returns>
        IList<Server> AllServers();
    }
}
