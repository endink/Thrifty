using Thrifty.Nifty.Ssl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using DotNetty.Common.Utilities;
using DotNetty.Buffers;

namespace Thrifty.Nifty.Core
{
    public class ConnectionContextHandler : ChannelHandlerAdapter
    {
        public const String NiftyConnectionContextKey = "Nifty.NContext";
        

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            AttributeKey<NiftyConnectionContext> key = AttributeKey<NiftyConnectionContext>.ValueOf(NiftyConnectionContextKey);
            NiftyConnectionContext nContext = context.GetAttribute<NiftyConnectionContext>(key).Get();
            base.ChannelRead(context, message);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            NiftyConnectionContext ncontext = new NiftyConnectionContext();
            ncontext.RemoteAddress = context.Channel.RemoteAddress;
            AttributeKey<NiftyConnectionContext> key = AttributeKey<NiftyConnectionContext>.ValueOf(NiftyConnectionContextKey);
            context.GetAttribute<NiftyConnectionContext>(key).Set(ncontext);
        }
    }
}
