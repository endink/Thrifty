using Thrifty.Nifty.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift;
using Thrift.Protocol;

namespace Thrifty.Nifty.Processors
{
    public interface INiftyProcessor
    {
        Task<bool> ProcessAsync(TProtocol protocolIn, TProtocol protocolOut, IRequestContext requestContext);
    }

   
}
