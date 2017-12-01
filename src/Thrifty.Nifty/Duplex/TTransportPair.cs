using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Transport;

namespace Thrifty.Nifty.Duplex
{
    /// <summary>
    /// Represents a pair of transports: one for input and one for output.
    /// </summary>
    public class TTransportPair
    {
        protected TTransportPair(TTransport inputTransport, TTransport outputTransport)
        {
            this.InputTransport = inputTransport;
            this.OutputTransport = outputTransport;
        }

        public TTransport InputTransport { get; private set; }

        public TTransport OutputTransport { get; private set; }

        public static TTransportPair FromSeparateTransports(TTransport inputTransport, TTransport outputTransport)
        {
            return new TTransportPair(inputTransport, outputTransport);
        }

        public static TTransportPair FromSingleTransport(TTransport transport)
        {
            return new TTransportPair(transport, transport);
        }

        public void Release()
        {
            this.InputTransport = null;
            this.OutputTransport = null;
        }
    }
}
