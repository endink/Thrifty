using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public interface IThriftClientFactory
    {
        Object CreateClient(
            INiftyClientChannel channel,
            Type clientType,
            ThriftClientMetadata clientMetadata,
            IEnumerable<ThriftClientEventHandler> clientHandlers,
            string clientDescription);
    }
}
