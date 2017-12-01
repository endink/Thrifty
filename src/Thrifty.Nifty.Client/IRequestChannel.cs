using DotNetty.Buffers;
using Thrifty.Nifty.Duplex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thrift;

namespace Thrifty.Nifty.Client
{
    public interface IRequestChannel
    {
        void WaitForFree();

        /// <summary>
        /// Sends a single message asynchronously, and notifies the {@link Listener}when the request is finished sending,
        /// when the response has arrived, and/or when an error occurs.
        /// </summary>
        void SendAsynchronousRequest(IByteBuffer request,
                bool oneway, IListener listener);

        /// <summary>
        /// Closes the channel
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();

        /// <summary>
        /// Checks whether the channel has encountered an error. 
        /// </summary>
        bool HasError { get; }

        /// <summary>
        /// Returns the {@link TException} representing the error the channel encountered, if any.
        /// </summary>
        /// <returns>An instance of {@link TException} or {@code null} if the channel is still good.</returns>
        TException GetError();


        /// <summary>
        /// Returns the {@link TDuplexProtocolFactory} that should be used by clients code to serialize messages for sending on this channel.
        /// </summary>
        TDuplexProtocolFactory ProtocolFactory { get; }
    }
}
