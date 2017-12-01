using DotNetty.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Thrift.Protocol;
using Thrifty.Nifty.Core;

namespace Thrifty.Nifty.Codecs
{
    /// <summary>
    /// 提供 Thrift 解码
    /// </summary>
    public abstract class ThriftFrameDecoder : ByteToMessageDecoder
    {
        protected abstract ThriftMessage Decode(IChannelHandlerContext ctx, IByteBuffer buffer);

        protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
        {
            Guard.ArgumentNotNull(context, nameof(context));
            Guard.ArgumentNotNull(message, nameof(message));
            Guard.ArgumentNotNull(output, nameof(output));

            var decoded = this.Decode(context, message);
            
            if (decoded != null)
            {
                output.Add(decoded);
            }
        }
    }
}
