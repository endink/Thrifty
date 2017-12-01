using DotNetty.Common.Utilities;
using Thrifty.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ITimerExtensions
    {
        /// <summary>
        /// 以 <paramref name="delay"/> 设置的时间，启动一个定时任务。
        /// 该任务只执行一次，并在 <paramref name="delay"/> 指定的时间之后执行。
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="task"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static ITimeout NewTimeout(this ITimer timer, Action<ITimeout> task, TimeSpan delay)
        {
            if (task == null)
            {
                throw new ArgumentNullException($"{nameof(ITimerExtensions)}.{nameof(NewTimeout)} 函数 {nameof(task)} 参数不能为空。");
            }
            return timer.NewTimeout(new DelegateTimerTask(task), delay);
        }

        public class DelegateTimerTask : ITimerTask
        {
            private Action<ITimeout> _func = null;

            public DelegateTimerTask(Action<ITimeout> task)
            {
                _func = task;
            }

            public void Run(ITimeout timeout)
            {
                _func(timeout);
            }
        }
    }
}
