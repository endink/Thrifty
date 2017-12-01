using Chopin.Pooling;
using DotNetty.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chopin.Pooling.Impl;

namespace Thrifty.MicroServices.Client.Pooling
{
    public class HashedWheelEvictionTimer : IEvictionTimer
    {
        private HashedWheelTimer _timer;
        private Dictionary<Evictor, ITimeout> _tasks;
        private bool _stopTimerIfDispose;
        private volatile bool _disposed;

        public HashedWheelEvictionTimer(HashedWheelTimer timer, bool stopTimerIfDispose = true)
        {
            Guard.ArgumentNotNull(timer, nameof(timer));
            _timer = timer;
            _stopTimerIfDispose = stopTimerIfDispose;
            _tasks = new Dictionary<Evictor, ITimeout>();
        }

        public void Cancel(Evictor task)
        {
            this.ThrowIfDisposed();
            lock (this)
            {
                if (_tasks.TryGetValue(task, out ITimeout to))
                {
                    to.Cancel();
                    _tasks.Remove(task);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                var tasks = _tasks.ToArray();
                _tasks.Clear();
                foreach (var t in tasks)
                {
                    t.Value.Cancel();
                }
                _tasks.Clear();

                if (_stopTimerIfDispose)
                {
                    _timer.StopAsync();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public void Schedule(Action action, TimeSpan period)
        {
            Guard.ArgumentNotNull(action, nameof(action));
            this.Schedule(new Evictor(action), period, period);
        }

        public void Schedule(Evictor task, TimeSpan delay, TimeSpan period)
        {
            this.ThrowIfDisposed();
            void ExecuteTask(Evictor ev, TimeSpan per)
            {
                ev.Run();

                if (_tasks.ContainsKey(ev)) // canceled
                {
                    _tasks[ev] = _timer.NewTimeout(to => ExecuteTask(ev, per), delay);
                }
            }

            this.Cancel(task);

            lock (this)
            {
                var r = _timer.NewTimeout(timeout =>
                {
                    ExecuteTask(task, period);
                }, delay);

                _tasks.Add(task, r);
            }
        }

    }
}
