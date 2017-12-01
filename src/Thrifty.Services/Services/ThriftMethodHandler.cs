using System;
using Thrifty.Codecs;
using Thrifty.Nifty.Core;
using Thrifty.Services.Metadata;
using System.Collections.Generic;
using System.Linq;
using Thrifty.Codecs.Metadata;
using System.Collections.Immutable;
using Thrifty.Nifty.Client;
using Thrift.Protocol;
using DotNetty.Buffers;
using Thrifty.Codecs.Internal;
using Thrift;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Thrifty.Services
{
    public partial class ThriftMethodHandler
    {
        private readonly ParameterHandler[] _parameterCodecs;
        private readonly IThriftCodec _successCodec;
        private readonly IDictionary<short, IThriftCodec> _exceptionCodecs;
        private readonly bool _oneway;
        private readonly bool _invokeAsynchronously;

        public ThriftMethodHandler(ThriftMethodMetadata methodMetadata, ThriftCodecManager codecManager)
        {
            this.QualifiedName = methodMetadata.QualifiedName;
            this.Name = methodMetadata.Name;
            this._invokeAsynchronously = methodMetadata.IsAsync;
            this._oneway = !_invokeAsynchronously && methodMetadata.IsOneWay;

            ParameterHandler[] parameters = new ParameterHandler[methodMetadata.Parameters.Count()];

            foreach (var fieldMetadata in methodMetadata.Parameters)
            {
                ThriftParameterInjection parameter = (ThriftParameterInjection)fieldMetadata.Injections.First();

                ParameterHandler handler = new ParameterHandler(
                        fieldMetadata.Id,
                        fieldMetadata.Name,
                        codecManager.GetCodec(fieldMetadata.ThriftType));

                parameters[parameter.ParameterIndex] = handler;
            }
            this._parameterCodecs = parameters;

            var builder = ImmutableDictionary.CreateBuilder<short, IThriftCodec>();
            foreach (var entry in methodMetadata.Exceptions)
            {
                builder.Add(entry.Key, codecManager.GetCodec(entry.Value));
            }
            _exceptionCodecs = builder.ToImmutableDictionary();

            // get the thrift codec for the return value
            _successCodec = codecManager.GetCodec(methodMetadata.ReturnType);
        }


        public String QualifiedName { get; }

        public String Name { get; }

        public Object Invoke(
            IRequestChannel channel,
            TChannelBufferInputTransport inputTransport,
            TChannelBufferOutputTransport outputTransport,
            TProtocol inputProtocol,
            TProtocol outputProtocol,
            int sequenceId,
            ClientContextChain contextChain,
            params object[] args)
        {
            return InnerInvoke(channel, inputTransport, outputTransport, inputProtocol, outputProtocol, sequenceId, contextChain, args);
        }

        private object InnerInvoke(IRequestChannel channel, TChannelBufferInputTransport inputTransport,
            TChannelBufferOutputTransport outputTransport, TProtocol inputProtocol, TProtocol outputProtocol, int sequenceId,
            ClientContextChain contextChain, object[] args)
        {
            if (_invokeAsynchronously)
            {
                // This method declares a Future return value: run it asynchronously
                return AsynchronousInvoke(channel, inputTransport, outputTransport, inputProtocol, outputProtocol, sequenceId,
                    contextChain, args);
            }
            else
            {
                try
                {
                    // This method declares an immediate return value: run it synchronously
                    return SynchronousInvoke(channel, inputTransport, outputTransport, inputProtocol, outputProtocol,
                        sequenceId, contextChain, args);
                }
                finally
                {
                    contextChain.Done();
                }
            }
        }

        private Object SynchronousInvoke(
            IRequestChannel channel,
            TChannelBufferInputTransport inputTransport,
            TChannelBufferOutputTransport outputTransport,
            TProtocol inputProtocol,
            TProtocol outputProtocol,
            int sequenceId,
            ClientContextChain contextChain,
            Object[] args)
        {
            Object results = null;

            // write request
            contextChain.PreWrite(args);
            outputTransport.ResetOutputBuffer();
            WriteArguments(outputProtocol, sequenceId, args);
            // Don't need to copy the output buffer for sync case

            IByteBuffer requestBuffer = outputTransport.OutputBuffer;
            contextChain.PostWrite(args);

            if (!this._oneway)
            {
                IByteBuffer responseBuffer;
                try
                {
                    responseBuffer = SyncClientHelpers.SendSynchronousTwoWayMessage(channel, requestBuffer);
                }
                catch (Exception e)
                {
                    contextChain.PreReadException(e);
                    throw;
                }
                finally
                {
                    requestBuffer.Release();
                }

                // read results
                contextChain.PreRead();
                try
                {
                    inputTransport.SetInputBuffer(responseBuffer);
                    WaitForResponse(inputProtocol, sequenceId);
                    results = ReadResponse(inputProtocol);
                    contextChain.PostRead(results);
                }
                catch (Exception e)
                {
                    contextChain.PostReadException(e);
                    throw;
                }
            }
            else
            {
                try
                {
                    SyncClientHelpers.SendSynchronousOneWayMessage(channel, requestBuffer);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return results;
        }

        public Task AsynchronousInvoke(
            IRequestChannel channel,
            TChannelBufferInputTransport inputTransport,
            TChannelBufferOutputTransport outputTransport,
            TProtocol inputProtocol,
            TProtocol outputProtocol,
            int sequenceId,
            ClientContextChain contextChain,
            Object[] args)
        {
            //Mark: 微软没有提供 TaskCompletionSource 的非泛型类型，只能使用动态类型处理。
            TaskCompletionSourceEx future;
            if (_successCodec.Type != ThriftType.Void)
            {
                future = new TaskCompletionSourceEx(_successCodec.Type.CSharpType);
            }
            else
            {
                future = new TaskCompletionSourceEx(typeof(Object));
            }
            var requestContext = RequestContexts.GetCurrentContext();

            contextChain.PreWrite(args);
            outputTransport.ResetOutputBuffer();
            WriteArguments(outputProtocol, sequenceId, args);
            IByteBuffer requestBuffer = outputTransport.OutputBuffer.Copy();
            contextChain.PostWrite(args);

            requestBuffer.Retain();
            channel.SendAsynchronousRequest(requestBuffer, false,
                new RequestListener(
                    onRequestSent: reqs =>
                    {
                        reqs.Release();
                        if (this._oneway)
                        {
                            try
                            {
                                ForceDoneChain(contextChain);
                                future.TrySetResult(null);
                            }
                            catch (Exception e)
                            {
                                ForceDoneChain(contextChain);
                                future.TrySetException(e);
                            }
                        }
                    },
                    onResponseReceive: message =>
                    {
                        IRequestContext oldRequestContext = RequestContexts.GetCurrentContext();
                        RequestContexts.SetCurrentContext(requestContext);
                        try
                        {
                            contextChain.PreRead();
                            inputTransport.SetInputBuffer(message);
                            WaitForResponse(inputProtocol, sequenceId);
                            Object results = ReadResponse(inputProtocol);
                            contextChain.PostRead(results);
                            ForceDoneChain(contextChain);
                            future.TrySetResult(results);
                        }
                        catch (Exception e)
                        {
                            var wrapException = ThriftClientManager.WrapTException(e);
                            contextChain.PostReadException(e);
                            ForceDoneChain(contextChain);
                            future.TrySetException(wrapException);
                        }
                        finally
                        {
                            RequestContexts.SetCurrentContext(oldRequestContext);
                        }
                    },
                    onChannelError: e =>
                    {
                        if (requestBuffer.ReferenceCount > 0)
                        {
                            requestBuffer.Release();
                        }

                        IRequestContext oldRequestContext = RequestContexts.GetCurrentContext();
                        RequestContexts.SetCurrentContext(requestContext);
                        try
                        {
                            contextChain.PreReadException(e);
                            ForceDoneChain(contextChain);

                            var wrappedException = ThriftClientManager.WrapTException(e);
                            future.TrySetException(wrappedException);
                        }
                        finally
                        {
                            RequestContexts.SetCurrentContext(oldRequestContext);
                        }
                    }
                    ));
            return future.Task;
        }

        private static void ForceDoneChain(ClientContextChain contextChain)
        {
            try
            {
                //有可能是 Done 引发了异常，需要处理一下。
                contextChain.Done();
            }
            catch (Exception ex)
            {
                ex.ThrowIfNecessary();
            }
        }

        private void WaitForResponse(TProtocol inProtocol, int sequenceId)
        {
            TMessage message = inProtocol.ReadMessageBegin();
            if (message.Type == TMessageType.Exception)
            {
                TApplicationException exception = TApplicationException.Read(inProtocol);
                inProtocol.ReadMessageEnd();
                throw exception;
            }
            if (message.Type != TMessageType.Reply)
            {
                throw new TApplicationException(TApplicationException.ExceptionType.InvalidMessageType,
                                                $"Received invalid message type {message.Type} from server");
            }
            if (!message.Name.Equals(this.QualifiedName))
            {
                throw new TApplicationException(TApplicationException.ExceptionType.WrongMethodName,
                                                $"Wrong method name in reply: expected {this.QualifiedName} but received {message.Name}");
            }
            if (message.SeqID != sequenceId)
            {
                throw new TApplicationException(TApplicationException.ExceptionType.BadSequenceID, $"{this.QualifiedName} failed: out of sequence response");
            }
        }

        private Object ReadResponse(TProtocol inProtocol)
        {
            TProtocolReader reader = new TProtocolReader(inProtocol);
            reader.ReadStructBegin();
            Object results = null;
            Exception exception = null;
            while (reader.NextField())
            {
                if (reader.GetFieldId() == 0)
                {
                    results = reader.ReadField(_successCodec);
                }
                else
                {
                    IThriftCodec exceptionCodec = null;
                    if (_exceptionCodecs.TryGetValue(reader.GetFieldId(), out exceptionCodec))
                    {
                        exception = (Exception)reader.ReadField(exceptionCodec);
                    }
                    else
                    {
                        reader.SkipFieldData();
                    }
                }
            }
            reader.ReadStructEnd();
            inProtocol.ReadMessageEnd();

            if (exception != null)
            {
                throw exception;
            }

            if (_successCodec.Type == ThriftType.Void)
            {
                // TODO: check for non-null return from a void function?
                return null;
            }

            if (results == null)
            {
                throw new TApplicationException(TApplicationException.ExceptionType.MissingResult, $"{this.QualifiedName} failed: unknown result");
            }
            return results;
        }

        private void WriteArguments(TProtocol outProtocol, int sequenceId, Object[] args)
        {
            // Note that though setting message type to ONEWAY can be helpful when looking at packet
            // captures, some clients always send CALL and so servers are forced to rely on the "oneway"
            // attribute on thrift method in the interface definition, rather than checking the message
            // type.
            outProtocol.WriteMessageBegin(new TMessage(this.QualifiedName, _oneway ? TMessageType.Oneway : TMessageType.Call, sequenceId));

            // write the parameters
            TProtocolWriter writer = new TProtocolWriter(outProtocol);
            writer.WriteStructBegin(this.Name + "_args");
            for (int i = 0; i < args.Length; i++)
            {
                Object value = args[i];
                ParameterHandler parameter = _parameterCodecs[i];
                writer.WriteField(parameter.Name, parameter.Id, parameter.Codec, value);
            }
            writer.WriteStructEnd();

            outProtocol.WriteMessageEnd();
            outProtocol.Transport.Flush();
        }

    }
}