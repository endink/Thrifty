using Thrifty.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Commons
{
    public static class HealthCheckIdentifier
    {
        public const String ServiceName = "_HealthCheck";

        public const String PingMethodName = "ping";

        public static String PingMethodQualifiedName
        {
            get { return ThriftMethodMetadata.GetQualifiedName(ServiceName, PingMethodName); }
        }
    }
}
