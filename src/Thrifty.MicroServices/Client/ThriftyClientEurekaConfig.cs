using Steeltoe.Discovery.Eureka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Client
{
    public sealed class ThriftyClientEurekaConfig : EurekaClientConfig
    {
        public ServiceAddressUsage AddressUsage { get; set; } = ServiceAddressUsage.IPAddress;
    }
}
