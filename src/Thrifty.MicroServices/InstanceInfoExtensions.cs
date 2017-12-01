using Steeltoe.Discovery.Eureka.AppInfo;
using Thrifty.MicroServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.MicroServices
{
    internal static class InstanceInfoExtensions
    {
        public static string GetAddress(this InstanceInfo instance, ServiceAddressUsage usage)
        {
            switch (usage)
            {
                case ServiceAddressUsage.HostName:
                    return instance.HostName;
                case ServiceAddressUsage.IPAddress:
                default:
                    return instance.IpAddr;
            }
        }
    }
}
