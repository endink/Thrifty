using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public interface INiftySecurityHandlers
    {
        IChannelHandler GetAuthenticationHandler();
        IChannelHandler GetEncryptionHandler();
    }
}
