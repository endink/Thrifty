using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Client
{
    public interface INiftyClientChannel : IRequestChannel
    {
        /// <summary>
        /// a timeout used to limit elapsed time for sending a message.
        /// </summary>
        TimeSpan? SendTimeout { get; set; }

        /// <summary>
        /// Sets a timeout used to limit elapsed time between successful send, and reception of the response.
        /// </summary>
        TimeSpan? ReceiveTimeout { get; set; }

        /// <summary>
        /// Sets a timeout used to limit the time that the client waits for data to be sent by the server.
        /// </summary>
        TimeSpan? ReadTimeout { get; set; }

        /// <summary>
        /// Executes the given {@link Runnable} on the I/O thread that manages reads/writes for this channel.
        /// </summary>
        /// <param name="runnable"></param>
        void ExecuteInIoThread(IRunnable runnable);

        IChannel NettyChannel { get; }
    }
}
