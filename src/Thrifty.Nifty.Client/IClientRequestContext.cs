using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Client
{
    public interface IClientRequestContext
    {
        TProtocol OutputProtocol { get; }

        TProtocol InputProtocol { get; }

        IRequestChannel RequestChannel { get; }

        EndPoint RemoteAddress { get; }
    }
}
