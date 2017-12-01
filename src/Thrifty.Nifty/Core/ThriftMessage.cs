using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public delegate ThriftMessage ThriftMessageFactory(IByteBuffer buffer);

    public class ThriftMessage
    {

        public ThriftMessage(IByteBuffer buffer, ThriftTransportType transportType)
        {
            this.Buffer = buffer;
            this.TransportType = transportType;
        }

        public IByteBuffer Buffer { get; }

        public ThriftTransportType TransportType { get; }

        /// <summary>
        /// TO DO: NiftyDispatcher
        /// Gets a <see cref="ThriftMessageFactory"/> for creating messages similar to this one. 
        /// Used by NiftyDispatcher to create response messages that are similar to their corresponding request messages.
        /// </summary>
        /// <returns>返回 <see cref="ThriftMessageFactory"/> 对象。</returns>
        public ThriftMessageFactory MessageFactory
        {
            get
            {
                return (messageBuffer) => new ThriftMessage(messageBuffer, this.TransportType);
            }
        }

        /// <summary>
        ///Standard Thrift clients require ordered responses, so even though Nifty can run multiple
        ///requests from the same client at the same time, the responses have to be held until all
        ///previous responses are ready and have been written.However, through the use of extended
        ///protocols and codecs, a request can indicate that the client understands
        ///out-of-order responses.
        /// </summary>
        /// <returns></returns>
        public bool IsOrderedResponsesRequired
        {
            get { return true; }
        }

        public long ProcessStartTimeTicks { get; set; }
    }
}
