using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.Nifty.Core;
using Thrifty.Nifty.Duplex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;

namespace Thrifty.Nifty.Client
{
    /// <summary>
    /// 客户端通道基础类型，线程不安全，同一实例不能多线程使用。
    /// 虽然 RequestMap 使用了读写锁防止多线程错误，但仅用于处理集合多线程操作情况，多线程无法保证 key 安全，因此，该类无法支持多线程。
    /// 大量的锁定可能导致性能下降，最佳做法是使用顺序操作，目前版本为读写锁。
    /// </summary>
    public abstract partial class AbstractClientChannel : ChannelHandlerAdapter, INiftyClientChannel, IDisposable
    {
        private readonly ILogger _logger;

        private readonly IDictionary<int, Request> _requestMap;
        private volatile TException _channelError;
        private ITimer _timer;
        private ReaderWriterLockSlim _readerWriterLock;
        private volatile bool _isDisposed;
        private int _invoked;
        private bool _closed;
        private readonly object _closeSyncRoot = new object();

        protected AbstractClientChannel(IChannel nettyChannel, ITimer timer, TDuplexProtocolFactory protocolFactory, ILoggerFactory loggerFactory = null)
        {
            this._requestMap = new Dictionary<int, Request>();
            this.NettyChannel = nettyChannel;
            this._timer = timer;
            this.ProtocolFactory = protocolFactory;
            this._logger = loggerFactory?.CreateLogger(this.GetType()) ?? NullLogger.Instance;
            this._readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        public IChannel NettyChannel { get; }

        public TDuplexProtocolFactory ProtocolFactory { get; }

        protected abstract IByteBuffer ExtractResponse(Object message);

        protected int ExtractSequenceId(IByteBuffer messageBuffer)
        {
            try
            {
                messageBuffer.MarkReaderIndex();
                using (TTransport inputTransport = new TChannelBufferInputTransport(messageBuffer))
                {
                    using (TProtocol inputProtocol = this.ProtocolFactory.GetInputProtocolFactory().GetProtocol(inputTransport))
                    {
                        TMessage message = inputProtocol.ReadMessageBegin();
                        messageBuffer.ResetReaderIndex();
                        return message.SeqID;
                    }
                }
            }
            catch (Exception t)
            {
                throw new TTransportException(TTransportException.ExceptionType.Unknown,
                    $"Could not find sequenceId in Thrift message{Environment.NewLine}{ t.Message}");
            }
        }



        public TimeSpan? SendTimeout { get; set; }

        /// <summary>
        /// 整个请求的超时时间。
        /// </summary>
        public TimeSpan? ReceiveTimeout { get; set; }

        /// <summary>
        /// 从服务器读取数据的超时时间。
        /// </summary>
        public TimeSpan? ReadTimeout { get; set; }

        protected abstract Task WriteRequestAsync(IByteBuffer request);

        public Task CloseAsync()
        {
            if (!_closed)
            {
                lock (_closeSyncRoot)
                {
                    if (!_closed)
                    {
                        _closed = true;
                        if (this.NettyChannel.Open)
                        {
                            return Task.Run(() =>
                                {
                                    this.WaitForFree();
                                })
                                .ContinueWith(t => this.NettyChannel.CloseAsync());
                        }
                    }
                }
            }
            return Task.FromResult(0);
        }

        public bool HasError
        {
            get { return _channelError != null; }
        }

        public TException GetError()
        {
            return _channelError;
        }

        public void ExecuteInIoThread(IRunnable runnable)
        {
            TcpSocketChannel nioSocketChannel = (TcpSocketChannel)this.NettyChannel;
            nioSocketChannel.EventLoop.Execute(runnable);
        }

        public void SendAsynchronousRequest(IByteBuffer message, bool oneway, IListener listener)
        {
            int sequenceId = 0;
            bool requestMaked = false;
            try
            {
                sequenceId = ExtractSequenceId(message);
                Request request = MakeRequest(sequenceId, listener, oneway);
                requestMaked = true;
                if (!this.NettyChannel.Active)
                {
                    FireChannelErrorCallback(listener, new TTransportException(TTransportException.ExceptionType.NotOpen, "Channel closed"));
                    return;
                }

                if (this.HasError)
                {
                    FireChannelErrorCallback(
                            listener,
                            new TTransportException(TTransportException.ExceptionType.Unknown, "Channel is in a bad state due to failing a previous request"));
                    return;
                }
#if DEBUG
                _logger.LogDebug($"client write message to server: {message.ForDebugString()}");
#endif
                Task sendFuture = WriteRequestAsync(message);
                QueueSendTimeout(request);
                
                sendFuture.ContinueWith(t =>
                {
                    OnMessageSent(t, request, oneway, message);
                });
            }
            catch (Exception t)
            {
                // onError calls all registered listeners in the requestMap, but this request
                // may not be registered yet. So we try to remove it (to make sure we don't call
                // the callback twice) and then manually make the callback for this request
                // listener.
                if (requestMaked)
                {
                    _readerWriterLock.EnterWriteLock();
                    try
                    {
                        _requestMap.Remove(sequenceId);
                    }
                    finally
                    {
                        _readerWriterLock.ExitWriteLock();
                    }
                }
                FireChannelErrorCallback(listener, t);

                OnError(t);
            }
        }

        private void OnMessageSent(Task future, Request request, bool oneway, IByteBuffer message)
        {
            try
            {
                if (future.Exception == null)
                {
                    CancelRequestTimeouts(request);
                    FireRequestSentCallback(request.Listener, message);
                    if (oneway)
                    {
                        RetireRequest(request);
                    }
                    else
                    {
                        QueueReceiveAndReadTimeout(request);
                    }
                }
                else
                {
                    TTransportException transportException = new TTransportException(TTransportException.ExceptionType.Unknown,
                        $"Sending request failed: {Environment.NewLine}{future.Exception.ToString()}");
                    OnError(transportException);
                }
            }
            catch (Exception t)
            {
                OnError(t);
            }
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            try
            {
                IByteBuffer response = ExtractResponse(message);
                if (response != null)
                {
                    int sequenceId = ExtractSequenceId(response);
                    OnResponseReceived(sequenceId, response);
                }
                else // for one-way
                {

                    //FireResponseReceivedCallback(request.Listener, response);
                }
            }
            catch (Exception t)
            {
                OnError(t);
            }
            finally
            {
                Interlocked.Decrement(ref _invoked);
                context.FireChannelReadComplete();
            }
        }

      

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            OnError(exception);
        }


        private Request MakeRequest(int sequenceId, IListener listener, bool oneway = false)
        {
            Request request = new Request(listener);
            if (!oneway)
            {
                _readerWriterLock.EnterWriteLock();
                try
                {
                    _requestMap[sequenceId] = request;
                }
                finally
                {
                    _readerWriterLock.ExitWriteLock();
                }
            }
            return request;
        }

        private void RetireRequest(Request request)
        {
            CancelRequestTimeouts(request);
        }

        private void CancelRequestTimeouts(Request request)
        {
            ITimeout sendTimeout = request.SendTimeout;
            if (sendTimeout != null && !sendTimeout.Canceled)
            {
                sendTimeout.Cancel();
            }

            ITimeout receiveTimeout = request.ReceiveTimeout;
            if (receiveTimeout != null && !receiveTimeout.Canceled)
            {
                receiveTimeout.Cancel();
            }

            ITimeout readTimeout = request.ReadTimeout;
            if (readTimeout != null && !readTimeout.Canceled)
            {
                readTimeout.Cancel();
            }
        }

        private void CancelAllTimeouts()
        {
            Request[] values = GetAllRequests();

            foreach (Request request in values)
            {
                CancelRequestTimeouts(request);
            }
        }

        private Request[] GetAllRequests()
        {
            Request[] values;
            _readerWriterLock.EnterReadLock();
            try
            {
                values = _requestMap.Values.ToArray();
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }

            return values;
        }

        private void OnResponseReceived(int sequenceId, IByteBuffer response)
        {
            _readerWriterLock.EnterReadLock();
            Request request = null;
            try
            {
                request = _requestMap.RemoveAndGet(sequenceId);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            if (request == null)
            {
                OnError(new TTransportException("Bad sequence id in response: " + sequenceId));
            }
            else
            {
                RetireRequest(request);
                FireResponseReceivedCallback(request.Listener, response);
            }
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            if ((_requestMap?.Count ?? 0) > 0)
            {
                OnError(new TTransportException(TTransportException.ExceptionType.NotOpen, "Client was disconnected by server"));
            }
        }


        protected void OnError(Exception t)
        {
            _invoked = int.MinValue;
            TException wrappedException = WrapException(t);

            if (_channelError == null)
            {
                _channelError = wrappedException;
            }

            CancelAllTimeouts();

            List<Request> requests = new List<Request>();
            var all = this.GetAllRequests();
            requests.AddRange(all);
            _readerWriterLock.EnterWriteLock();
            try
            {
                _requestMap.Clear();
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            foreach (Request request in requests)
            {
                FireChannelErrorCallback(request.Listener, wrappedException);
            }

            IChannel channel = this.NettyChannel;
            if (channel.Open)
            {
                channel.CloseAsync();
            }
        }

        protected TException WrapException(Exception t)
        {
            if (t is TException)
            {
                return (TException)t;
            }
            else
            {
                return new TTransportException(t.Message);
            }
        }

        private void FireRequestSentCallback(IListener listener, IByteBuffer request)
        {
            try
            {
                listener.OnRequestSent(request);
            }
            catch (Exception t)
            {
                _logger.LogWarning(default(EventId), t, "Request sent listener callback triggered an exception");
            }
        }

        private void FireResponseReceivedCallback(IListener listener, IByteBuffer response)
        {
            try
            {
                listener.OnResponseReceived(response);
            }
            catch (Exception t)
            {
                _logger.LogWarning(default(EventId), t, "Response received listener callback triggered an exception");
            }
        }

        private void FireChannelErrorCallback(IListener listener, TException exception)
        {
            try
            {
                listener.OnChannelError(exception);
            }
            catch (Exception t)
            {
                _logger.LogWarning(default(EventId), t, "Channel error listener callback triggered an exception");
                t.ThrowIfNecessary();
            }
        }

        private void FireChannelErrorCallback(IListener listener, Exception throwable)
        {
            FireChannelErrorCallback(listener, WrapException(throwable));
        }

        private void OnSendTimeoutFired(Request request)
        {
            CancelAllTimeouts();

            FireChannelErrorCallback(request.Listener, new TTransportException(TTransportException.ExceptionType.TimedOut,
                $"Timed out waiting {SendTimeout.Value.TotalSeconds} seconds to send data to server"));
        }

        private void OnReceiveTimeoutFired(Request request)
        {
            CancelAllTimeouts();
            FireChannelErrorCallback(request.Listener, new TTransportException(TTransportException.ExceptionType.TimedOut,
                $"Timed out waiting {this.ReceiveTimeout.Value.TotalSeconds} seconds to receive response"));
        }

        private void OnReadTimeoutFired(Request request)
        {
            CancelAllTimeouts();
            Interlocked.Decrement(ref _invoked);
            FireChannelErrorCallback(request.Listener, new TTransportException(
                TTransportException.ExceptionType.TimedOut, $"Timed out waiting {this.ReadTimeout.Value.TotalSeconds} secondes to read data from server"));
        }


        private void QueueSendTimeout(Request request)
        {
            Interlocked.Increment(ref _invoked);
            if (this.SendTimeout.HasValue)
            {
                if (this.SendTimeout > TimeSpan.Zero)
                {
                    ITimerTask sendTimeoutTask = new IoThreadBoundTimerTask(this, t => OnSendTimeoutFired(request));

                    ITimeout sendTimeout;
                    try
                    {
                        sendTimeout = _timer.NewTimeout(sendTimeoutTask, this.SendTimeout.Value);
                    }
                    catch (Exception)
                    {
                        throw new TTransportException(TTransportException.ExceptionType.Unknown, "Unable to schedule send timeout");
                    }
                    request.SendTimeout = sendTimeout;
                }
            }
        }

        private void QueueReceiveAndReadTimeout(Request request)
        {
            if (this.ReceiveTimeout.HasValue)
            {
                if (this.ReceiveTimeout.Value > TimeSpan.Zero)
                {
                    ITimerTask receiveTimeoutTask = new IoThreadBoundTimerTask(this, t => OnReceiveTimeoutFired(request));

                    ITimeout timeout;
                    try
                    {
                        timeout = _timer.NewTimeout(receiveTimeoutTask, this.ReceiveTimeout.Value);
                    }
                    catch (Exception)
                    {
                        throw new TTransportException(TTransportException.ExceptionType.Unknown, "Unable to schedule request timeout");
                    }
                    request.ReceiveTimeout = timeout;
                }
            }

            if (this.ReadTimeout != null)
            {
                long readTimeoutMills = (long)Math.Floor(this.ReadTimeout.Value.TotalMilliseconds);
                if (readTimeoutMills > 0)
                {
                    ITimerTask readTimeoutTask = new IoThreadBoundTimerTask(this, new ReadTimeoutTask(readTimeoutMills, request, this));

                    ITimeout timeout;
                    try
                    {
                        timeout = _timer.NewTimeout(readTimeoutTask, TimeSpan.FromMilliseconds(readTimeoutMills));
                    }
                    catch (Exception e)
                    {
                        throw new TTransportException($"Unable to schedule read timeout{Environment.NewLine}{e.Message}");
                    }
                    request.ReadTimeout = timeout;
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                this.CloseAsync();
                this._timer?.StopAsync();
                this._readerWriterLock?.Dispose();
                this._readerWriterLock = null;
                this._timer = null;
            }
        }

        public void WaitForFree()
        {
            while (_invoked > 0)
            {
                Thread.Sleep(1);
            }
        }
    }
}
