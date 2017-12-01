using System;
using System.Threading;

namespace Thrifty.MicroServices.Ribbon
{
    public class Counter
    {
        private long _counter;
        public void Increment() => Interlocked.Increment(ref _counter);
        public void Decrement() => Interlocked.Decrement(ref _counter);
        public long Value
        {
            get => _counter;
            set => Interlocked.Exchange(ref _counter, value);
        }
    }


    public abstract class Disposable : IDisposable
    {
        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeUnmanagedResource() { }
        protected abstract void DisposeManagedResource();
        private void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                DisposeManagedResource();
            }
            disposed = true;
            DisposeUnmanagedResource();
        }
        ~Disposable()
        {
            Dispose(false);
        }
    }

}
