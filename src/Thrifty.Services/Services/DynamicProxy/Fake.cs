using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Thrifty.Nifty.Client;
using Thrifty.Nifty.Core;
using Thrifty.Nifty.Duplex;
using Thrifty.Threading;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;

namespace Thrifty.Services.Services.DynamicProxy
{
    internal class Fake : IFake, IDisposable
    {
        private readonly string _clientDescription;
        private readonly ThriftClientMetadata _clientMetadata;
        private bool _disposed;
        private readonly IEnumerable<ThriftClientEventHandler> eventHandlers;
        private readonly IDictionary<MethodInfo, ThriftMethodHandler> methodHandlers;
        private TChannelBufferInputTransport _inputTransport;
        private TChannelBufferOutputTransport _outputTransport;
        private int _sequenceId = 1;
        public IRequestChannel Channel { get; }
        public TProtocol InputProtocol { get; private set; }
        public TProtocol OutputProtocol { get; private set; }

        public Fake(string clientDescription, ThriftClientMetadata clientMetadata, IRequestChannel channel,
            IEnumerable<ThriftClientEventHandler> immutableList)
        {
            this.Channel = channel;
            this._clientMetadata = clientMetadata;
            this._clientDescription = clientDescription;
            this.methodHandlers = clientMetadata.MethodHandlers;
            this.eventHandlers = immutableList;

            this._inputTransport = new TChannelBufferInputTransport();
            this._outputTransport = new TChannelBufferOutputTransport();

            var transportPair = TTransportPair.FromSeparateTransports(this._inputTransport, this._outputTransport);
            var protocolPair = channel.ProtocolFactory.GetProtocolPair(transportPair);
            this.InputProtocol = protocolPair.InputProtocol;
            this.OutputProtocol = protocolPair.OutputProtocol;
        }

        public object Invoke(MethodInfo method, object[] args)
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (args == null && string.Equals(nameof(IDisposable.Dispose), method.Name))
            {
                this.Channel.CloseAsync().GetAwaiter().GetResult();
                this.Dispose();
                return null;
            }
            if (args == null && string.Equals($"get_{nameof(INiftyClientChannelAware.ClientChannel)}", method.Name))
            {
                return (this.Channel as INiftyClientChannel);
            }
            try
            {
                return Process(method, args);
            }
            catch (TException e)
            {
                throw ThriftClientManager.WrapTException(e);
            }
        }

        private object Process(MethodInfo method, object[] args)
        {
            if (!methodHandlers.TryGetValue(method, out ThriftMethodHandler methodHandler))
                throw new TApplicationException(TApplicationException.ExceptionType.UnknownMethod, $"Unknown method : '{method.Name}'");

            if (Channel.HasError) throw new TTransportException(this.Channel.GetError().Message);
            var niftyClientChannel = this.Channel as INiftyClientChannel;

            var remoteAddress = niftyClientChannel?.NettyChannel.RemoteAddress;
            IClientRequestContext requestContext = new NiftyClientRequestContext(this.InputProtocol, this.OutputProtocol, this.Channel, remoteAddress);
            var context = new ClientContextChain(eventHandlers, methodHandler.QualifiedName, requestContext);

            var id = InterLockedEx.GetAndIncrement(ref this._sequenceId);
            var result = methodHandler.Invoke(this.Channel,
                _inputTransport,
                _outputTransport,
                InputProtocol,
                OutputProtocol,
                id,
                context, args ?? new object[0]);

            return result;
        }
        ~Fake()
        {
            this.Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this._inputTransport?.Dispose();
                    this._outputTransport?.Dispose();
                    this.InputProtocol?.Dispose();
                    this._outputTransport?.Dispose();

                    this._inputTransport = null;
                    this._outputTransport = null;
                    this.InputProtocol = null;
                    this.OutputProtocol = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString() => _clientDescription;
    }
}
