using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public class IdleDisconnectHandler : ChannelHandlerAdapter
    {
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            IdleStateEvent idleStateEvent = evt as IdleStateEvent;
            if (idleStateEvent != null && idleStateEvent.State == IdleState.ReaderIdle)
            {
                //异步关闭，无需等待线程。
                context.Channel.CloseAsync();
            }
        }
    }
}
