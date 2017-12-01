using System.Collections.Generic;
using Steeltoe.Discovery.Eureka.AppInfo;
using Thrifty.MicroServices.Server;
using Newtonsoft.Json;
namespace Thrifty.MicroServices.Client
{
    public class DiscoveryEnabledServer : Ribbon.Server
    {
        public DiscoveryEnabledServer(InstanceInfo instanceInfo, bool useSecurePort, ServiceAddressUsage addressUsage)
            : base(instanceInfo.GetAddress(addressUsage),
                useSecurePort && instanceInfo.IsSecurePortEnabled ? instanceInfo.SecurePort : instanceInfo.Port)
        {
            InstanceInfo = instanceInfo;
            Metadata = instanceInfo.Metadata;
        }

        public InstanceInfo InstanceInfo { get; }

        public IDictionary<string, string> Metadata { get; }

        private static T Parse<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
            }
        }
        private ThriftyServiceMetadata[] _serviceMetadata;
        public ThriftyServiceMetadata[] ServiceMetadata
        {
            get
            {
                if (_serviceMetadata == null)
                {
                    //cache result
                    _serviceMetadata = Parse<ThriftyServiceMetadata[]>(Metadata["services"]) ?? new ThriftyServiceMetadata[0];
                }
                return _serviceMetadata;
            }
        }
    }
}
