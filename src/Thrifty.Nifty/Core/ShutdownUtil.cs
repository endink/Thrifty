using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Core
{
    public class ShutdownUtil
    {
        public static Task ShutdownChannelAsync(
                                              IEventExecutorGroup bossExecutor,
                                              IEventExecutorGroup workerExecutor,
                                              IChannelGroup allChannels,
                                              TimeSpan timeSpan)
        {
            timeSpan = timeSpan.TotalSeconds <= 2 ? TimeSpan.FromSeconds(3) : timeSpan;
            List<Task> tasks = new List<Task>();
            // Close all channels
            if (allChannels != null)
            {
                tasks.Add(allChannels.CloseAsync());
            }

            // Stop boss threads
            if (bossExecutor != null)
            {
                tasks.Add(bossExecutor.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), timeSpan));
            }

            // Finally stop I/O workers
            if (workerExecutor != null)
            {
                tasks.Add(workerExecutor.ShutdownGracefullyAsync(TimeSpan.FromSeconds(2), timeSpan));
            }
            return Task.WhenAll(tasks.ToArray());
        }
    }
}
