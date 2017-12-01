using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Thrifty.Nifty.Duplex;
using Thrifty.Threading;
using DotNetty.Common.Utilities;

namespace Thrifty.Nifty.Client
{
    public class UnframedClientChannel : AbstractClientChannel
    {
        public UnframedClientChannel(IChannel nettyChannel, ITimer timer, TDuplexProtocolFactory protocolFactory, ILoggerFactory loggerFactory = null) : 
            base(nettyChannel, timer, protocolFactory, loggerFactory)
        {
        }

        protected override IByteBuffer ExtractResponse(object message)
        {
            IByteBuffer buffer = message as IByteBuffer;
            if (buffer == null)
            {
                return null;
            }


            if (!buffer.IsReadable())
            {
                return null;
            }

            return buffer;
        }

        protected override Task WriteRequestAsync(IByteBuffer request)
        {
            return this.NettyChannel.WriteAndFlushAsync(request);
        }
    }
}
