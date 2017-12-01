using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using System.Threading;
using Microsoft.Extensions.Logging;
using Thrifty;
using Microsoft.Extensions.Logging.Abstractions;

namespace Thrifty.Nifty.Core
{
    partial class NettyServerTransport
    {
        private class ConnectionLimiter : ChannelHandlerAdapter
        {
            private int numConnections;
            private readonly int maxConnections;
            private ILogger _logger = null;

            public ConnectionLimiter(int maxConnections, ILoggerFactory loggerFactory = null)
            {
                this.maxConnections = maxConnections;
                _logger = loggerFactory?.CreateLogger<ConnectionLimiter>() ?? (ILogger)NullLogger.Instance;
            }

            public override void ChannelActive(IChannelHandlerContext context)
            {
                if (maxConnections > 0 && Interlocked.Increment(ref numConnections) > maxConnections)
                {
                    context.Channel.CloseAsync().GetAwaiter().GetResult();
                    // numConnections will be decremented in channelClosed
                    _logger.LogInformation("Accepted connection above limit (%s). Dropping.", maxConnections);
                }
                base.ChannelActive(context);
            }
        }
    }
}
