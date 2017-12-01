using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using Thrifty.Nifty.Duplex;
using Thrift;
using System.Threading;

namespace Thrifty.MicroServices.Client.Pooling
{
    public sealed class PooledClientChannel : INiftyClientChannel, IDisposable
    {
        private IClientConnectionPool _pool;
        private volatile bool _disposed;
        private INiftyClientChannel _innerChannel;
        private ChannelKey _key;

        public PooledClientChannel(IClientConnectionPool pool, ChannelKey key)
        {
            Guard.ArgumentNotNull(pool, nameof(pool));
            _pool = pool;
            _innerChannel = _pool.BorrowChannel(key);
            this._key = key;
        }

        public TimeSpan? SendTimeout
        {
            get
            {
                this.ThrowIfDisposed();
                return _innerChannel.SendTimeout;
            }
            set
            {
                this.ThrowIfDisposed();
                _innerChannel.SendTimeout = value;
            }
        }
        public TimeSpan? ReceiveTimeout
        {
            get
            {
                this.ThrowIfDisposed();
                return _innerChannel.ReceiveTimeout;
            }
            set
            {
                this.ThrowIfDisposed();
                _innerChannel.ReceiveTimeout = value;
            }
        }

        public TimeSpan? ReadTimeout
        {
            get
            {
                this.ThrowIfDisposed();
                return _innerChannel.ReadTimeout;
            }
            set
            {
                this.ThrowIfDisposed();
                _innerChannel.ReadTimeout = value;
            }
        }
        public IChannel NettyChannel
        {
            get
            {
                ThrowIfDisposed();
                return _innerChannel.NettyChannel;
            }
        }

        public bool HasError
        {
            get
            {
                ThrowIfDisposed();
                return _innerChannel.HasError;
            }
        }

        public TDuplexProtocolFactory ProtocolFactory
        {
            get
            {
                ThrowIfDisposed();
                return _innerChannel.ProtocolFactory;
            }
        }

        public void WaitForFree()
        {
            _innerChannel.WaitForFree();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public Task CloseAsync()
        {
            ThrowIfDisposed();
            if (_innerChannel != null)
            {
                var channel = _innerChannel;
                _innerChannel = null;
                return Task.Run(() =>
                {
                    channel.WaitForFree();
                    _pool.ReturnChannel(_key, channel);
                });
            }
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _innerChannel.WaitForFree();
                _pool.ReturnChannel(this._key, _innerChannel);
                _innerChannel = null;
                _pool = null;
            }
        }

        public void ExecuteInIoThread(IRunnable runnable)
        {
            ThrowIfDisposed();
            _innerChannel.ExecuteInIoThread(runnable);
        }

        public TException GetError()
        {
            ThrowIfDisposed();
            return _innerChannel.GetError();
        }

        public void SendAsynchronousRequest(IByteBuffer request, bool oneway, IListener listener)
        {
            ThrowIfDisposed();
            _innerChannel.SendAsynchronousRequest(request, oneway, listener);
        }
    }
}
