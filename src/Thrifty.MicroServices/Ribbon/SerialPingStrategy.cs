using System;
using System.Collections.Generic;
using System.Linq;

namespace Thrifty.MicroServices.Ribbon
{
    public class SerialPingStrategy : IPingStrategy
    {

        private bool IsAlive(IPing ping, Server server)
        {
            try
            {
                return ping.IsAlive(server);
            }
            catch
            {
                return false;
            }
        }
        public IList<PingResult> PingServers(IPing ping, IList<Server> servers)
           => (from server in servers select new PingResult(IsAlive(ping, server), server))
            .ToList()
            .AsReadOnly();
    }
}
