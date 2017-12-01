using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Server
{
    class EurekaRegisterInfo : IEurekaInstanceConfig
    {

        public EurekaRegisterInfo()
        {
            this.HostName = Environment.MachineName;
            this.IpAddress = GetHostAddress(true);

            IsInstanceEnabledOnInit = false;
            NonSecurePort = EurekaInstanceConfig.Default_NonSecurePort;
            SecurePort = EurekaInstanceConfig.Default_SecurePort;
            IsNonSecurePortEnabled = true;
            SecurePortEnabled = false;
            LeaseRenewalIntervalInSeconds = EurekaInstanceConfig.Default_LeaseRenewalIntervalInSeconds;
            LeaseExpirationDurationInSeconds = EurekaInstanceConfig.Default_LeaseExpirationDurationInSeconds;
            VirtualHostName = this.HostName + ":" + NonSecurePort;
            SecureVirtualHostName = this.HostName + ":" + SecurePort;
            AppName = EurekaInstanceConfig.Default_Appname;
            StatusPageUrlPath = EurekaInstanceConfig.Default_StatusPageUrlPath;
            HomePageUrlPath = EurekaInstanceConfig.Default_HomePageUrlPath;
            HealthCheckUrlPath = EurekaInstanceConfig.Default_HealthCheckUrlPath;
            MetadataMap = new Dictionary<string, string>();
            DataCenterInfo = new DataCenterInfo(DataCenterName.MyOwn);
        }
        public string InstanceId { get; set; }
        public string AppName { get; set; }
        public string AppGroupName { get; set; }
        public bool IsInstanceEnabledOnInit { get; set; }
        public int NonSecurePort { get; set; }
        public int SecurePort { get; set; }
        public bool IsNonSecurePortEnabled { get; set; }
        public bool SecurePortEnabled { get; set; }
        public int LeaseRenewalIntervalInSeconds { get; set; }
        public int LeaseExpirationDurationInSeconds { get; set; }
        public string VirtualHostName { get; set; }
        public string SecureVirtualHostName { get; set; }
        public string ASGName { get; set; }
        public IDictionary<string, string> MetadataMap { get; set; }
        public IDataCenterInfo DataCenterInfo { get; set; }
        public string IpAddress { get; set; }
        public string StatusPageUrlPath { get; set; }
        public string StatusPageUrl { get; set; }
        public string HomePageUrlPath { get; set; }
        public string HomePageUrl { get; set; }
        public string HealthCheckUrlPath { get; set; }
        public string HealthCheckUrl { get; set; }
        public string SecureHealthCheckUrl { get; set; }
        public string[] DefaultAddressResolutionOrder { get; set; }
        public bool PreferIpAddress { get; set; }

        public string HostName { get; set; }

        

        public string GetHostAddress(bool refresh)
        {
            if (String.IsNullOrWhiteSpace(this.IpAddress))
            {
                var addresses = from item in NetworkInterface.GetAllNetworkInterfaces()
                                where item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                                      || item.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                                from ipc in item.GetIPProperties().UnicastAddresses
                                let address = ipc.Address
                                where address.AddressFamily == AddressFamily.InterNetwork
                                select address;
                this.IpAddress = addresses.ToArray().FirstOrDefault()?.ToString() ?? "localhost";
            }
            return this.IpAddress;
        }

        public string GetHostName(bool refresh)
        {
            return this.HostName;
        }
    }
}
