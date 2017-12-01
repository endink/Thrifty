using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift;

namespace Thrifty.Nifty.Client
{
    /// <summary>
    /// The listener interface that must be implemented for callback objects passed to {@link #sendAsynchronousRequest}
    /// </summary>
    public interface IListener
    {
        /// <summary>
        /// This will be called when the request has successfully been written to the transport layer (e.g. socket)
        /// </summary>
        void OnRequestSent(IByteBuffer request);

        /// <summary>
        /// This will be called when a full response to the request has been received
        /// </summary>
        /// <param name="message"></param>
        void OnResponseReceived(IByteBuffer message);
        
        /// <summary>
        /// This will be called if the channel encounters an error before the request is sent or
        /// before a response is received
        /// </summary>
        /// <param name="requestException">A {@link TException} describing the problem that was encountered</param>
        void OnChannelError(TException requestException);
    }

    public class RequestListener : IListener
    {
        private Action<TException> _onChannelError;
        private Action<IByteBuffer> _onRequestSent;
        private Action<IByteBuffer> _onResponseReceived;

        public RequestListener(Action<IByteBuffer> onRequestSent = null,
            Action<IByteBuffer> onResponseReceive = null,
            Action<TException> onChannelError = null)
        {
            this._onRequestSent = onRequestSent;
            this._onResponseReceived = onResponseReceive;
            this._onChannelError = onChannelError;
        }

        public void OnChannelError(TException var1)
        {
            _onChannelError?.Invoke(var1);
        }

        public void OnRequestSent(IByteBuffer request)
        {
            _onRequestSent?.Invoke(request);
        }

        public void OnResponseReceived(IByteBuffer buffer)
        {
            _onResponseReceived?.Invoke(buffer);
        }
    }
}
