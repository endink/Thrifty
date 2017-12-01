using Thrifty.Nifty.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Transport;
using DotNetty.Buffers;
using Thrift;
using DotNetty.Common.Utilities;
using System.Diagnostics.Contracts;
using Thrift.Protocol;
using System.Reflection;
using Thrifty.Nifty.Client;

namespace Thrifty.Nifty.Client
{
    public class TNiftyClientChannelTransport : TTransport
    {
        private Type _clientClass;
        private INiftyClientChannel _channel;
        private Dictionary<String, Boolean> _methodNameToOneWay;
        private TChannelBufferOutputTransport _requestBufferTransport;
        private TChannelBufferInputTransport _responseBufferTransport;
        private ConcurrentQueue<ResponseListener> _queuedResponses;

        public TNiftyClientChannelTransport(Type clientType, INiftyClientChannel channel)
        {
            Guard.ArgumentNotNull(clientType, nameof(clientType));
            this._clientClass = clientType;
            this._channel = channel;

            this._methodNameToOneWay = new Dictionary<string, bool>();
            this._requestBufferTransport = new TChannelBufferOutputTransport();
            //think: Unpooled 0 是否不需要释放资源？
            this._responseBufferTransport = new TChannelBufferInputTransport(Unpooled.Buffer(0, 0));
            this._queuedResponses = new ConcurrentQueue<ResponseListener>();
        }

        public INiftyClientChannel ClientChannel { get; }

        public override bool IsOpen
        {
            get { return this.ClientChannel.NettyChannel.Open; }
        }

        public override void Close()
        {
            this.ClientChannel.CloseAsync().GetAwaiter().GetResult();

            this._requestBufferTransport?.Dispose();
            this._responseBufferTransport?.Dispose();

            this._requestBufferTransport = null;
            this._responseBufferTransport = null;
        }

        public override void Open()
        {
            if (!this.IsOpen)
            {
                throw new InvalidOperationException("TNiftyClientChannelTransport requires an already-opened channel");
            }
        }

        public override int Read(byte[] buf, int off, int len)
        {
            if (!_responseBufferTransport.IsReadable())
            {
                try
                {
                    // If our existing response transport doesn't have any bytes remaining to read,
                    // wait for the next queued response to arrive, and point our response transport
                    // to that.
                    ResponseListener listener;
                    if (_queuedResponses.TryDequeue(out listener))
                    {
                        IByteBuffer response = listener.GetResponseAsync().GetAwaiter().GetResult();

                        if (!response.IsReadable())
                        {
                            throw new NiftyException("Received an empty response");
                        }

                        _responseBufferTransport.SetInputBuffer(response);
                    }
                }
                catch (Exception e)
                {
                    // Error while waiting for response
                    if (e is TTransportException)
                    {
                        throw e;
                    }
                    e.ThrowIfNecessary();
                }
            }

            // Read as many bytes as we can (up to the amount requested) from the response
            return _responseBufferTransport.Read(buf, off, len);
        }

        public override void Write(byte[] buf, int off, int len)
        {
            _requestBufferTransport.Write(buf, off, len);
        }

        public override void Flush()
        {
            try
            {
                bool sendOneWay = InOneWayRequest();
                ResponseListener listener = new ResponseListener();
                _channel.SendAsynchronousRequest(_requestBufferTransport.OutputBuffer.Copy(), sendOneWay, listener);
                _queuedResponses.Enqueue(listener);
                _requestBufferTransport.ResetOutputBuffer();
            }
            catch (TException e)
            {
                if (e is TTransportException)
                {
                    throw new TTransportException($"Failed to use reflection on Client class to determine whether method is oneway.{Environment.NewLine}{e.Message}");
                }
            }
        }

        private bool InOneWayRequest()
        {
            bool isOneWayMethod = false;

            // Create a temporary transport wrapping the output buffer, so that we can read the method name for this message
            using (TChannelBufferInputTransport requestReadTransport = new TChannelBufferInputTransport(_requestBufferTransport.OutputBuffer.Duplicate()))
            {
                using (TProtocol protocol = _channel.ProtocolFactory.GetOutputProtocolFactory().GetProtocol(requestReadTransport))
                {
                    TMessage message = protocol.ReadMessageBegin();
                    String methodName = message.Name;

                    isOneWayMethod = ClientClassHasReceiveHelperMethod(methodName);
                    return isOneWayMethod;
                }
            }
        }

        private bool ClientClassHasReceiveHelperMethod(String methodName)
        {
            bool isOneWayMethod = false;

            if (!_methodNameToOneWay.ContainsKey(methodName))
            {
                try
                {
                    // Hack！OneWay 方法不包含 recv_ 开头的方法生成，我们以此来判定一个方法是否是 OneWay
                    // 'recv_foo' 仅支持 two-way 的方法.
                    //
                    // We should fix this by getting flushMessage()/flushOneWayMessage() added to
                    // TTransport.
                    _clientClass.GetTypeInfo().GetMethod("recv_" + methodName);
                }
                catch (MissingMethodException)
                {
                    isOneWayMethod = true;
                }

                // cache result so we don't use reflection every time
                _methodNameToOneWay[methodName] = isOneWayMethod;
            }
            else
            {
                _methodNameToOneWay.TryGetValue(methodName, out isOneWayMethod);
            }
            return isOneWayMethod;
        }

        protected override void Dispose(bool disposing)
        {
            this.Close();
        }

        #region

        private class ResponseListener : IListener
        {
            private Exception _excecption = null;
            private IByteBuffer _buffer = null;

            public void OnChannelError(TException requestException)
            {
                _excecption = requestException;
            }

            public void OnRequestSent(IByteBuffer reuqest)
            {

            }

            public void OnResponseReceived(IByteBuffer message)
            {
                _buffer = message;
            }

            public Task<IByteBuffer> GetResponseAsync()
            {
                if (_excecption != null)
                {
                    return TaskEx.FromException<IByteBuffer>(_excecption);
                }
                return Task.FromResult(_buffer);
            }
        }

        #endregion
    }
}
