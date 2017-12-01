using System;
using System.Collections.Generic;
using System.Linq;
using Thrifty.MicroServices.Ribbon.Rules; 

namespace Thrifty.MicroServices.Client
{

    public class VersionAffinityFilter : IFilter
    {
        private readonly string _version;

        public VersionAffinityFilter(string version)
        {
            _version = version;
        }


        private IEnumerable<Ribbon.Server> InnerFiltration(IEnumerable<Ribbon.Server> servers)
        {
            if (servers == null) yield return null;
            foreach (var s in servers)
            {
                var server = s as DiscoveryEnabledServer;
                if (server == null)
                {
                    continue;
                }

                var services = server.ServiceMetadata;
                var found = false;

                foreach (var service in services)
                {
                    var name = service.ServiceName;
                    var version = service.Version;

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
                    {
                        continue;
                    }

                    if (string.Compare(_version, version, StringComparison.Ordinal) != 0)
                    {
                        continue;
                    }
                    found = true;
                }
                if (!found) continue;
                yield return s;
            }
        }

        public IEnumerable<Ribbon.Server> Filtration(IEnumerable<Ribbon.Server> servers) => InnerFiltration(servers).ToArray();
    }
}
