using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Codecs
{
    public class DefaultThriftFrameCodec : ChannelHandlerAdapter, IThriftFrameCodec
    {
        private readonly ThriftFrameDecoder _decoder;
        private readonly ThriftFrameEncoder _encoder;
        private ILogger _logger = null;

        public DefaultThriftFrameCodec(long maxFrameSize, TProtocolFactory inputProtocolFactory, ILoggerFactory loggerFactory = null)
        {
            this._decoder = new DefaultThriftFrameDecoder(maxFrameSize, inputProtocolFactory);
            this._encoder = new DefaultThriftFrameEncoder(maxFrameSize);
            _logger = loggerFactory?.CreateLogger<DefaultThriftFrameCodec>() ?? (ILogger)NullLogger.Instance;
        }

        public DefaultThriftFrameCodec(long maxFrameSize)
            : this(maxFrameSize, new TBinaryProtocol.Factory(), null)
        {
        }
        

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
#if DEBUG
            _logger.LogDebug($"decode message {(message as IByteBuffer).ForDebugString()}");
#endif
            this._decoder.ChannelRead(context, message);
            
        }

        public override async Task WriteAsync(IChannelHandlerContext context, object message)
        {
            if (this._encoder.AcceptOutboundMessage(message))
            {
                await this._encoder.WriteAsync(context, message);
            }
        }
    }
}
