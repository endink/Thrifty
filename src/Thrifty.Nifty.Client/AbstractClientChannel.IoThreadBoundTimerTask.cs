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

        /**
         * Used to create TimerTasks that will fire
         */
        private class IoThreadBoundTimerTask : ITimerTask
        {
            private INiftyClientChannel channel;
            private ITimerTask timerTask;

            public IoThreadBoundTimerTask(INiftyClientChannel channel, Action<ITimeout> timerTask)
                : this(channel, new ITimerExtensions.DelegateTimerTask(timerTask))
            {

            }

            public IoThreadBoundTimerTask(INiftyClientChannel channel, ITimerTask timerTask)
            {
                this.channel = channel;
                this.timerTask = timerTask;
            }

            public void Run(ITimeout timeout)
            {
                channel.ExecuteInIoThread(new DelegateRunnable(() =>
                {
                    try
                    {
                        timerTask.Run(timeout);
                    }
                    catch (Exception e)
                    {
                        channel.NettyChannel.Pipeline.FireExceptionCaught(e);
                    }
                }));
            }


        }
    }
}
