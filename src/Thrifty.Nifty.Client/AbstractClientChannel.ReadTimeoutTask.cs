using DotNetty.Common.Utilities;
using Thrifty.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Client
{
    partial class AbstractClientChannel
    {

        private sealed class ReadTimeoutTask : ITimerTask
        {
            private TimeoutHandler _timeoutHandler;
            private long _timeoutMilliseconds;
            private Request request;
            private AbstractClientChannel clientChannel;

            public ReadTimeoutTask(long timeoutMilliseconds, Request request, AbstractClientChannel clientChannel)
            {
                this.clientChannel = clientChannel;
                this._timeoutHandler = TimeoutHandler.FindTimeoutHandler(clientChannel.NettyChannel.Pipeline);
                this._timeoutMilliseconds = timeoutMilliseconds;
                this.request = request;
            }

            public void Run(ITimeout timeout)
            {
                if (_timeoutHandler == null)
                {
                    return;
                }

                if (timeout.Canceled)
                {
                    return;
                }

                if (!clientChannel.NettyChannel.Open)
                {
                    return;
                }

                long currentTimeNanos = DateTime.UtcNow.Ticks;

                long timePassed = currentTimeNanos - _timeoutHandler.LastMessageReceivedMilliseconds;
                long nextDelayMills = (_timeoutMilliseconds - timePassed) / 10000;

                if (nextDelayMills <= 0)
                {
                    clientChannel.OnReadTimeoutFired(request);
                }
                else
                {
                    request.ReadTimeout = (clientChannel._timer.NewTimeout(this, TimeSpan.FromMilliseconds(nextDelayMills)));
                }
            }
        }
    }
}
