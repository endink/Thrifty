using Steeltoe.Discovery.Eureka;
using Thrifty.MicroServices.Commons;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrifty.Nifty.Ssl;

namespace Thrifty.MicroServices.Server
{
    public class ThriftyServerOptions : ThriftServerConfig
    {
        private EurekaClientConfig _eurekaConfig = null;
        private SslConfig _sslConfig = null;

        public ThriftyServerOptions()
        {
            this.EurekaEnabled = true;
        }
        
        public bool EurekaEnabled { get; set; }

        public SslConfig Ssl
        {
            get { return _sslConfig ?? (_sslConfig = new SslConfig()); }
            set { _sslConfig = value; }
        }


        public EurekaClientConfig Eureka
        {
            get { return _eurekaConfig ?? (_eurekaConfig = new EurekaClientConfig()); }
            set { _eurekaConfig = value; }
        }
    }
}
