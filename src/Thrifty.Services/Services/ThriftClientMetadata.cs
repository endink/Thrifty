using Thrifty.Codecs;
using Thrifty.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public class ThriftClientMetadata
    {
        private readonly ThriftServiceMetadata thriftServiceMetadata;

        internal ThriftClientMetadata(
                Type clientType,
                String clientName,
                ThriftCodecManager codecManager)
        {
            Guard.ArgumentNotNull(clientType, nameof(clientType));
            Guard.ArgumentNotNull(codecManager, nameof(codecManager));
            Guard.ArgumentNullOrWhiteSpaceString(clientName, nameof(clientName));

            this.ClientName = clientName;
            thriftServiceMetadata = new ThriftServiceMetadata(clientType, codecManager.Catalog);
            this.ClientType = thriftServiceMetadata.Name;

            this.MethodHandlers = new Dictionary<MethodInfo, ThriftMethodHandler>();
            foreach (ThriftMethodMetadata methodMetadata in thriftServiceMetadata.Methods.Values)
            {
                ThriftMethodHandler methodHandler = new ThriftMethodHandler(methodMetadata, codecManager);
                MethodHandlers.Add(methodMetadata.Method, methodHandler);
            }
        }

        private String ClientType { get; }
        private String ClientName { get; }

        public String Name
        {
            get { return thriftServiceMetadata.Name; }
        }

        public IDictionary<MethodInfo, ThriftMethodHandler> MethodHandlers { get; }
    }
}
