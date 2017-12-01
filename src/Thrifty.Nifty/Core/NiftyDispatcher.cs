using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Thrifty.Nifty.Duplex;
using Thrift.Protocol;
using DotNetty.Common.Utilities;
using Thrift;
using Thrifty.Nifty.Processors;
using System.Threading;
using DotNetty.Common.Concurrency;
using static Thrift.TApplicationException;
using Thrifty.Threading;
using Thrifty;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DotNetty.Buffers;
using DotNetty.Handlers.Tls;

namespace Thrifty.Nifty.Core
{
    public class NiftyDispatcher : SimpleChannelInboundHandler<ThriftMessage>
    {
        private readonly INiftyProcessorFactory _processorFactory;
        private readonly long _taskTimeoutMillis;
        private readonly ITimer _taskTimeoutTimer;
        private readonly long _queueTimeoutMillis;
        private readonly int _queuedResponseLimit;
        private readonly Dictionary<long, ThriftMessage> _responseMap = new Dictionary<long, ThriftMessage>();
        private long _dispatcherSequenceId = 0;
        private long _lastResponseWrittenId = 0;
        private readonly TDuplexProtocolFactory _duplexProtocolFactory;
        private ILogger _logger = null;
        private TaskFactory _factory;

        public NiftyDispatcher(ThriftServerDef serverDef, ITimer timer, int? ioThreadCount, ILoggerFactory loggerFactory = null)
        {
            Guard.ArgumentNotNull(timer, nameof(timer));
            this._taskTimeoutTimer = timer;

            _logger = loggerFactory?.CreateLogger(this.GetType()) ?? NullLogger.Instance;
            this._processorFactory = serverDef.ProcessorFactory;
            //this.exe = serverDef.Executor;
            this._taskTimeoutMillis = (long)serverDef.TaskTimeout.TotalMilliseconds;
            this._queueTimeoutMillis = (long)serverDef.QueueTimeout.TotalMilliseconds;
            this._queuedResponseLimit = serverDef.QueuedResponseLimit;
            this._duplexProtocolFactory = serverDef.DuplexProtocolFactory;

            if (ioThreadCount.HasValue && ioThreadCount.Value > 0)
            {
                LimitedConcurrencyLevelTaskScheduler limitedScheduler = new LimitedConcurrencyLevelTaskScheduler(ioThreadCount.Value);
                _factory = new TaskFactory(limitedScheduler);
            }
            else
            {
                _factory = Task.Factory;
            }
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        protected override void ChannelRead0(IChannelHandlerContext ctx, ThriftMessage message)
        {
            message.ProcessStartTimeTicks = DateTime.UtcNow.Ticks;
            CheckResponseOrderingRequirements(ctx, message);

            TNiftyTransport messageTransport = new TNiftyTransport(ctx.Channel, message, true);
            TTransportPair transportPair = TTransportPair.FromSingleTransport(messageTransport);
            TProtocolPair protocolPair = this._duplexProtocolFactory.GetProtocolPair(transportPair);
            TProtocol inProtocol = protocolPair.InputProtocol;
            TProtocol outProtocol = protocolPair.OutputProtocol;

            long requestSequenceId = BlockReadingForOrderReponse(ctx);
            IByteBuffer buffer = message.Buffer;
            buffer.Retain();

            _factory.StartNew(() => ProcessRequestAsync(requestSequenceId, ctx, message, messageTransport, inProtocol, outProtocol)
               .ContinueWith(t =>
               {
                   inProtocol.Dispose();
                   outProtocol.Dispose();
                   buffer.Release();
                   messageTransport?.Dispose();
               }));
        }

        private void CheckResponseOrderingRequirements(IChannelHandlerContext ctx, ThriftMessage message)
        {
            bool messageRequiresOrderedResponses = message.IsOrderedResponsesRequired;

            if (!DispatcherContext.IsResponseOrderingRequirementInitialized(ctx))
            {
                // This is the first request. This message will decide whether all responses on the
                // channel must be strictly ordered, or whether out-of-order is allowed.
                DispatcherContext.SetResponseOrderingRequired(ctx, messageRequiresOrderedResponses);
            }
            else if (messageRequiresOrderedResponses != DispatcherContext.IsResponseOrderingRequired(ctx))
            {
                // This is not the first request. Verify that the ordering requirement on this message
                // is consistent with the requirement on the channel itself.
                throw new NiftyException("Every message on a single channel must specify the same requirement for response ordering");
            }
        }
        

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            switch (exception)
            {
                case NotSslRecordException ssl:
                    _logger.LogError(0, exception, "the SSL protocol is enabled on the server, but the client does not use the SSL protocol.");
                    break;
                default:
                    // Any out of band exception are caught here and we tear down the socket
                    _logger.LogError(0, exception, "nifty dispacth fault.");
                    break;
            }
            this.CloseChannel(context);
            // Send for logging
            context.FireExceptionCaught(exception);
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            // Reads always start out unblocked
            DispatcherContext.UnblockChannelReads(context);
            base.ChannelActive(context);
        }

        private void CloseChannel(IChannelHandlerContext ctx)
        {
            if (ctx.Channel.Open)
            {
                ctx.Channel.CloseAsync();
            }
        }

        private async Task ProcessRequestAsync(
            long requestSequenceId,
            IChannelHandlerContext ctx,
            ThriftMessage message,
            TNiftyTransport messageTransport,
            TProtocol inProtocol,
            TProtocol outProtocol)
        {
            //Task.Run(() =>
            //{
            try
            {
                AtomicBoolean responseSent = new AtomicBoolean(false);
                // Use AtomicReference as a generic holder class to be able to mark it final
                // and pass into inner classes. Since we only use .get() and .set(), we don't
                // actually do any atomic operations.
                AtomicReference<ITimeout> expireTimeout = new AtomicReference<ITimeout>(null);
                try
                {
                    try
                    {
                        long timeRemaining = 0;
                        long timeElapsed = (DateTime.UtcNow.Ticks - message.ProcessStartTimeTicks) / 10000;

                        if (_queueTimeoutMillis > 0)
                        {
                            if (timeElapsed >= _queueTimeoutMillis)
                            {
                                String error = "Task stayed on the queue for " + timeElapsed +
                                                " milliseconds, exceeding configured queue timeout of " + _queueTimeoutMillis +
                                                " milliseconds.";
                                _logger.LogWarning(error);
                                TApplicationException taskTimeoutException = new TApplicationException(
                                        ExceptionType.InternalError, error);
                                await SendTApplicationExceptionAsync(taskTimeoutException, ctx, message, requestSequenceId, messageTransport,
                                    inProtocol, outProtocol);
                                return;
                            }
                        }
                        else if (_taskTimeoutMillis > 0)
                        {
                            if (timeElapsed >= _taskTimeoutMillis)
                            {
                                String error = "Task stayed on the queue for " + timeElapsed +
                                                " milliseconds, exceeding configured task timeout of " + _taskTimeoutMillis +
                                                " milliseconds.";
                                _logger.LogWarning(error);
                                TApplicationException taskTimeoutException = new TApplicationException(
                                        ExceptionType.InternalError, error);
                                await SendTApplicationExceptionAsync(taskTimeoutException, ctx, message, requestSequenceId, messageTransport,
                                        inProtocol, outProtocol);
                                return;
                            }
                            else
                            {
                                timeRemaining = _taskTimeoutMillis - timeElapsed;
                            }
                        }

                        if (timeRemaining > 0)
                        {
                            expireTimeout.Value = _taskTimeoutTimer.NewTimeout(timeout =>
                            {
                                // The immediateFuture returned by processors isn't cancellable, cancel() and
                                // isCanceled() always return false. Use a flag to detect task expiration.
                                if (responseSent.CompareAndSet(false, true))
                                {
                                    TApplicationException ex = new TApplicationException(
                                            ExceptionType.InternalError,
                                            "Task timed out while executing."
                                    );
                                    // Create a temporary transport to send the exception
                                    var duplicateBuffer = message.Buffer.Duplicate();
                                    duplicateBuffer.ResetReaderIndex();
                                    using (TNiftyTransport temporaryTransport = new TNiftyTransport(
                                            ctx.Channel,
                                            duplicateBuffer,
                                            message.TransportType))
                                    {
                                        TProtocolPair protocolPair = _duplexProtocolFactory.GetProtocolPair(
                                                TTransportPair.FromSingleTransport(temporaryTransport));
                                        SendTApplicationExceptionAsync(ex, ctx, message,
                                                requestSequenceId,
                                                temporaryTransport,
                                                protocolPair.InputProtocol,
                                                protocolPair.OutputProtocol).GetAwaiter().GetResult();
                                    }
                                }
                            }, TimeSpan.FromMilliseconds(timeRemaining / 10000));
                        }
                        //SSL 部分暂时不处理
                        IConnectionContext connectionContext = ctx.Channel.GetConnectionContext();
                        IRequestContext requestContext = new NiftyRequestContext(connectionContext, inProtocol, outProtocol, messageTransport);
                        RequestContexts.SetCurrentContext(requestContext);
                        try
                        {
                            //关键部分：交给 Thrift 来处理二进制。
                            bool result = await _processorFactory.GetProcessor(messageTransport).ProcessAsync(inProtocol, outProtocol, requestContext);
                            DeleteExpirationTimer(expireTimeout.Value);
                            try
                            {
                                // Only write response if the client is still there and the task timeout
                                // hasn't expired.
                                if (ctx.Channel.Active && responseSent.CompareAndSet(false, true))
                                {
                                    ThriftMessage response = message.MessageFactory(
                                            messageTransport.OutBuffer);
                                    await WriteResponseAsync(ctx, response, requestSequenceId,
                                            DispatcherContext.IsResponseOrderingRequired(ctx));
                                }
                            }
                            catch (Exception t)
                            {
                                this.OnDispatchException(ctx, t);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(0,  ex, "write response to client fault.");
                            DeleteExpirationTimer(expireTimeout.Value);
                            OnDispatchException(ctx, ex);
                        }
                    }
                    finally
                    {
                        RequestContexts.ClearCurrentContext();
                    }
                }
                catch (Exception ex)
                {
                    OnDispatchException(ctx, ex);
                    ex.ThrowIfNecessary();
                }
            }
            catch (Exception ex)
            {
                ex.ThrowIfNecessary();
            }

            //});
        }

        private long BlockReadingForOrderReponse(IChannelHandlerContext ctx)
        {
            long requestSequenceId = Interlocked.Increment(ref _dispatcherSequenceId);

            if (DispatcherContext.IsResponseOrderingRequired(ctx))
            {
                lock (_responseMap)
                {
                    // Limit the number of pending responses (responses which finished out of order, and are
                    // waiting for previous requests to be finished so they can be written in order), by
                    // blocking further channel reads. Due to the way Netty frame decoders work, this is more
                    // of an estimate than a hard limit. Netty may continue to decode and process several
                    // more requests that were in the latest read, even while further reads on the channel
                    // have been blocked.
                    if (requestSequenceId > Interlocked.Read(ref _lastResponseWrittenId) + _queuedResponseLimit &&
                        !DispatcherContext.IsChannelReadBlocked(ctx))
                    {
                        DispatcherContext.BlockChannelReads(ctx);
                    }
                }
            }

            return requestSequenceId;
        }

        private void DeleteExpirationTimer(ITimeout timeout)
        {
            if (timeout == null)
            {
                return;
            }
            timeout.Cancel();
        }

        private async Task SendTApplicationExceptionAsync(
                TApplicationException x,
                IChannelHandlerContext ctx,
                ThriftMessage request,
                long responseSequenceId,
                TNiftyTransport requestTransport,
                TProtocol inProtocol,
                TProtocol outProtocol)
        {
            if (ctx.Channel.Open)
            {
                try
                {
                    TMessage message = inProtocol.ReadMessageBegin();
                    outProtocol.WriteMessageBegin(new TMessage(message.Name, TMessageType.Exception, message.SeqID));
                    x.Write(outProtocol);
                    outProtocol.WriteMessageEnd();
                    requestTransport.setTApplicationException(x);
                    outProtocol.Transport.Flush();
                    //unrelease on requestTransport dispose;

                    ThriftMessage response = request.MessageFactory.Invoke(requestTransport.OutBuffer);
                    await WriteResponseAsync(ctx, response, responseSequenceId, DispatcherContext.IsResponseOrderingRequired(ctx));
                }
                catch (TException ex)
                {
                    OnDispatchException(ctx, ex);
                }
            }
        }

        private void OnDispatchException(IChannelHandlerContext ctx, Exception t)
        {
            ctx.FireExceptionCaught(t);
            this.CloseChannel(ctx);
        }

        private async Task WriteResponseAsync(IChannelHandlerContext ctx,
                                   ThriftMessage response,
                                   long responseSequenceId,
                                   bool isOrderedResponsesRequired)
        {
            if (isOrderedResponsesRequired)
            {
                WriteResponseInOrder(ctx, response, responseSequenceId);
            }
            else
            {
                // No ordering required, just write the response immediately
                await ctx.WriteAndFlushAsync(response);
                
                Interlocked.Increment(ref this._lastResponseWrittenId);
            }
        }

        private void WriteResponseInOrder(IChannelHandlerContext ctx,
                                          ThriftMessage response,
                                          long responseSequenceId)
        {
            // Ensure responses to requests are written in the same order the requests
            // were received.
            lock(_responseMap) {
                long currentResponseId = Interlocked.Read(ref _lastResponseWrittenId) + 1;
                if (responseSequenceId != currentResponseId)
                {
                    // This response is NOT next in line of ordered responses, save it to
                    // be sent later, after responses to all earlier requests have been
                    // sent.
                    _responseMap[responseSequenceId] = response;
                }
                else
                {
                    // This response was next in line, write this response now, and see if
                    // there are others next in line that should be sent now as well.
                    do
                    {
                        ctx.Channel.WriteAndFlushAsync(response).GetAwaiter().GetResult();
                        Interlocked.Increment(ref _lastResponseWrittenId);
                        ++currentResponseId;
                        response = _responseMap.RemoveAndGet(currentResponseId);
                    } while (null != response);

                    // Now that we've written some responses, check if reads should be unblocked
                    if (DispatcherContext.IsChannelReadBlocked(ctx))
                    {
                        long lastRequestSequenceId = Interlocked.Read(ref _dispatcherSequenceId);
                        if (lastRequestSequenceId <= Interlocked.Read(ref _lastResponseWrittenId) + _queuedResponseLimit)
                        {
                            DispatcherContext.UnblockChannelReads(ctx);
                        }
                    }
                }
            }
        }


        private class DispatcherContext
        {
            private ReadBlockedState readBlockedState = ReadBlockedState.NotBlocked;
            private bool responseOrderingRequired = false;
            private bool responseOrderingRequirementInitialized = false;
            public const string AttributeKey = "DispatcherContext";

            public static bool IsChannelReadBlocked(IChannelHandlerContext ctx)
            {
                return GetDispatcherContext(ctx).readBlockedState == ReadBlockedState.Blocked;
            }

            public static void BlockChannelReads(IChannelHandlerContext ctx)
            {
                // Remember that reads are blocked (there is no Channel.getReadable())
                GetDispatcherContext(ctx).readBlockedState = ReadBlockedState.Blocked;

                // NOTE: this shuts down reads, but isn't a 100% guarantee we won't get any more messages.
                // It sets up the channel so that the polling loop will not report any new read events
                // and netty won't read any more data from the socket, but any messages already fully read
                // from the socket before this ran may still be decoded and arrive at this handler. Thus
                // the limit on queued messages before we block reads is more of a guidance than a hard
                // limit.
                ctx.Channel.Configuration.SetOption(ChannelOption.AutoRead, false);

                ctx.Channel.Configuration.SetOption(ChannelOption.AutoRead, false);
                //ctx.Channel.setReadable(false);
            }

            public static void UnblockChannelReads(IChannelHandlerContext ctx)
            {
                // Remember that reads are unblocked (there is no Channel.getReadable())
                GetDispatcherContext(ctx).readBlockedState = ReadBlockedState.NotBlocked;
                ctx.Channel.Configuration.SetOption(ChannelOption.AutoRead, true);
                ctx.Channel.Read();
                //ctx.Channel.setReadable(true);
            }

            public static void SetResponseOrderingRequired(IChannelHandlerContext ctx, bool required)
            {
                DispatcherContext dispatcherContext = GetDispatcherContext(ctx);
                dispatcherContext.responseOrderingRequirementInitialized = true;
                dispatcherContext.responseOrderingRequired = required;
            }

            public static bool IsResponseOrderingRequired(IChannelHandlerContext ctx)
            {
                return GetDispatcherContext(ctx).responseOrderingRequired;
            }

            public static bool IsResponseOrderingRequirementInitialized(IChannelHandlerContext ctx)
            {
                return GetDispatcherContext(ctx).responseOrderingRequirementInitialized;
            }

            private static DispatcherContext GetDispatcherContext(IChannelHandlerContext ctx)
            {
                AttributeKey<DispatcherContext> key = AttributeKey<DispatcherContext>.ValueOf(DispatcherContext.AttributeKey);

                var attachment = ctx.GetAttribute(key);
                DispatcherContext dispatcherContext = attachment.Get();
                if (dispatcherContext == null)
                {
                    // No context was added yet, add one
                    dispatcherContext = new DispatcherContext();
                    attachment.Set(dispatcherContext);
                }

                return dispatcherContext;
            }

            private enum ReadBlockedState
            {
                NotBlocked,
                Blocked,
            }
        }
    }
}
