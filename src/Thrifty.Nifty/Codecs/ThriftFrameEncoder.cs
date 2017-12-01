using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Thrift.Protocol;
using Thrifty.Nifty.Core;

namespace Thrifty.Nifty.Codecs
{
    public abstract class ThriftFrameEncoder : MessageToByteEncoder<ThriftMessage>
    {
        //private static int _count = 0;
        protected abstract IByteBuffer Encode(IChannelHandlerContext context, ThriftMessage message);
        protected override void Encode(IChannelHandlerContext context, ThriftMessage message, IByteBuffer output)
        {
            Guard.ArgumentNotNull(context, nameof(context));
            Guard.ArgumentNotNull(message, nameof(message));
            Guard.ArgumentNotNull(output, nameof(output));

            IByteBuffer buffer = null;

            try
            {
                if (context.Channel.IsWritable)
                {
                    buffer = this.Encode(context, message);
                    //output.Add(buffer);
                    output.WriteBytes(buffer);
                    //context.Channel.WriteAsync(buffer);
                    buffer = null; //如果执行成功，下面的 Release 将无效。
                }
            }
            finally
            {
                buffer?.Release();
            }
        }
        //protected override void Encode(IChannelHandlerContext context, ThriftMessage message, List<object> output)
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
        //        }
        //    }
        //    finally
        //    {
        //        buffer?.Release();
        //    }
        //}
    }
}
