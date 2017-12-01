using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public interface INiftySecurityFactory
    {
        INiftySecurityHandlers GetSecurityHandlers(ThriftServerDef def, NettyServerConfig serverConfig);
    }
}
