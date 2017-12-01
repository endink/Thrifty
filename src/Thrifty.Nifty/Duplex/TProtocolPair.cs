using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Duplex
{
    /// <summary>
    /// Represents a pair of protocols: one for input and one for output.
    /// </summary>
    public class TProtocolPair
    {
        protected TProtocolPair(TProtocol inputProtocol, TProtocol outputProtocol)
        {
            this.InputProtocol = inputProtocol;
            this.OutputProtocol = outputProtocol;
        }

        public TProtocol InputProtocol { get; }

        public TProtocol OutputProtocol { get; }
        
        public static TProtocolPair FromSeparateProtocols(TProtocol inputProtocol, TProtocol outputProtocol)
        {
            return new TProtocolPair(inputProtocol, outputProtocol);
        }

        public static TProtocolPair FromSingleProtocol(TProtocol protocol)
        {
            return new TProtocolPair(protocol, protocol);
        }
    }
}
