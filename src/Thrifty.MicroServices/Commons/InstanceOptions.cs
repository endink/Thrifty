namespace Thrifty.MicroServices.Commons
{
    public class InstanceDescription
    {
        /// <summary>初始化 <see cref="InstanceDescription" /> 类的新实例。</summary>
        /// <param name="appName">app name.</param>
        /// <param name="vipAddress">virtual host name or address.</param>
        /// <param name="publicAddress">ip address or hostName</param>
        public InstanceDescription(string appName, string vipAddress, string publicAddress = "127.0.0.1")
        {
            Guard.ArgumentNotNull(publicAddress, nameof(publicAddress));
            this.AppName = appName;
            this.VipAddress = vipAddress;
            this.PublicAddress = publicAddress;
        }

        public string PublicAddress { get; }

        public string AppName { get; }


        public string VipAddress { get; }
    }
}