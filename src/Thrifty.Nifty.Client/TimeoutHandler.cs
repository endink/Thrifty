using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Client
{
    public sealed class TimeoutHandler : ChannelHandlerAdapter
    {
        private const String Name = "_TIMEOUT_HANDLER";
        
        private static readonly Object SyncRoot = new Object();

        public static void AddToPipeline(IChannelPipeline cp)
        {
            lock (SyncRoot)
            {
                Guard.ArgumentNotNull(cp, nameof(cp));
                if (cp.Get(TimeoutHandler.Name) == null)
                {
                    cp.AddFirst(TimeoutHandler.Name, new TimeoutHandler());
                }
            }
        }

        public static TimeoutHandler FindTimeoutHandler(IChannelPipeline cp)
        {
            return (TimeoutHandler)cp.Get(TimeoutHandler.Name);
        }

        private TimeoutHandler()
        {
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            this.LastMessageReceivedMilliseconds = (long)Math.Floor(DateTime.UtcNow.Ticks / 10000d);

            base.ChannelRead(context, message);
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            this.LastMessageSentMilliseconds = (long)Math.Floor(DateTime.UtcNow.Ticks / 10000d);
            return base.WriteAsync(context, message);
        }

        public long LastMessageReceivedMilliseconds { get; private set; }

        public long LastMessageSentMilliseconds { get; private set; }
    }
}
