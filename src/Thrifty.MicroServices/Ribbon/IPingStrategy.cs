using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// ping的结果
    /// </summary>
    public class PingResult
    {
        public PingResult(bool isAlive, Server server)
        {
            IsAlive = isAlive;
            Server = server;
        }
        /// <summary>
        /// 服务器是否alive
        /// </summary>
        public bool IsAlive { get; }
        /// <summary>
        /// 服务器
        /// </summary>
        public Server Server { get; }
    }

    public interface IPingStrategy
    {
        IList<PingResult> PingServers(IPing ping, IList<Server> servers);
    }
}
