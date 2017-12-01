using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Thrifty.Nifty.Core;
using DotNetty.Codecs;

namespace Thrifty.Nifty.Codecs
{
    public class DefaultThriftFrameEncoder : ThriftFrameEncoder
    {
        private readonly long _maxFrameSize;
        public DefaultThriftFrameEncoder(long maxFrameSize)
        {
            this._maxFrameSize = maxFrameSize;
        }

        protected override IByteBuffer Encode(IChannelHandlerContext context, ThriftMessage message)
        {
            int frameSize = message.Buffer.ReadableBytes;

            if (message.Buffer.ReadableBytes > _maxFrameSize)
            {
                context.FireExceptionCaught(new TooLongFrameException(
                        String.Format(
                                "Frame size exceeded on encode: frame was {0:d} bytes, maximum allowed is {1:d} bytes",
                                frameSize,
                                _maxFrameSize)));
                return null;
            }

            switch (message.TransportType)
            {
                case ThriftTransportType.Unframed:
                    return message.Buffer;

                case ThriftTransportType.Framed:
                    var buffer = Unpooled.Buffer(4 + message.Buffer.ReadableBytes, 4 + message.Buffer.ReadableBytes);
                    buffer.WriteInt(message.Buffer.ReadableBytes);
                    buffer.WriteBytes(message.Buffer, message.Buffer.ReadableBytes);
                    return buffer;
                    //return Buffers.WrappedBuffer(context.Allocator, buffer, message.Buffer);

                case ThriftTransportType.Header:
                    throw new NotSupportedException("Header transport is not supported");

                case ThriftTransportType.Http:
                    throw new NotSupportedException("HTTP transport is not supported");

                default:
                    throw new NotSupportedException("Unrecognized transport type");
            }
        }
        

        //protected void Encode(IChannelHandlerContext context, ThriftMessage message, List<object> output)
        //{
        //    Guard.ArgumentNotNull(context, nameof(context));
        //    Guard.ArgumentNotNull(message, nameof(message));
        //    Guard.ArgumentNotNull(output, nameof(output));

        //    IByteBuffer buffer = null;

        //    try
        //    {
        //        if (context.Channel.IsWritable)
        //        {
        //            buffer = this.Encode(context, message);
        //            output.Add(buffer);
        //            //context.Channel.WriteAsync(buffer);
        //            buffer = null; //如果执行成功，下面的 Release 将无效。
        //            Console.WriteLine("message was encoded !");
        //        }
        //    }
        //    finally
        //    {
        //        buffer?.Release();
        //    }
            
        //}
    }
}
