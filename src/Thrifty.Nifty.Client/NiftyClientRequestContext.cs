using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Client
{
    public class NiftyClientRequestContext : IClientRequestContext
    {
        public NiftyClientRequestContext(TProtocol inputProtocol, TProtocol outputProtocol, IRequestChannel requestChannel, EndPoint remoteAddress)
        {
            this.InputProtocol = inputProtocol;
            this.OutputProtocol = outputProtocol;
            this.RequestChannel = requestChannel;
            this.RemoteAddress = remoteAddress;
        }

        public TProtocol InputProtocol { get; }

        public TProtocol OutputProtocol { get; }

        public EndPoint RemoteAddress { get; }

        public IRequestChannel RequestChannel { get; }
    }
}
