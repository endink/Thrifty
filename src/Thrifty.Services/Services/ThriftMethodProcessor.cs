using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Thrifty.Codecs.Metadata;
using Thrifty.Codecs;
using Thrifty.Services.Metadata;
using Thrift.Protocol;
using Thrifty.Nifty.Core;
using System.Collections.Concurrent;
using Thrift;
using Thrifty.Codecs.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Thrifty.Services
{
    [DebuggerDisplay("{QualifiedName}", TargetTypeName = "Thrifty.Services.ThriftMethodProcessor")]
    public class ThriftMethodProcessor
    {
        private readonly Func<IRequestContext, Object> _serviceFactory;
        private readonly MethodInfo _method;
        private readonly String _resultStructName;
        private readonly bool _oneway;
        private readonly IEnumerable<ThriftFieldMetadata> _parameters;
        private readonly IDictionary<short, IThriftCodec> _parameterCodecs;
        private readonly IDictionary<short, short> _thriftParameterIdToCSharpArgument;
        private readonly IThriftCodec _successCodec;
        private readonly IDictionary<Type, ExceptionProcessor> _exceptionCodecs;
        private ILogger _logger;

        public ThriftMethodProcessor(
            Func<IRequestContext, Object> service,
            String serviceName,
            ThriftMethodMetadata methodMetadata,
            ThriftCodecManager codecManager,
            ILoggerFactory loggerFactory = null)
        {
            Guard.ArgumentNotNull(service, nameof(service));

            _logger = loggerFactory?.CreateLogger<ThriftMethodProcessor>() ?? (ILogger)NullLogger.Instance;

            this._serviceFactory = service;
            this.ServiceName = serviceName;
            this.Name = methodMetadata.Name;
            this.QualifiedName = methodMetadata.QualifiedName;
            this._resultStructName = $"{this.Name}_result";
            this._method = methodMetadata.Method;
            this._oneway = methodMetadata.IsOneWay && !methodMetadata.IsAsync;
            this._parameters = methodMetadata.Parameters;

            var builder = ImmutableDictionary.CreateBuilder<short, IThriftCodec>();
            foreach (ThriftFieldMetadata fieldMetadata in methodMetadata.Parameters)
            {
                builder.Add(fieldMetadata.Id, codecManager.GetCodec(fieldMetadata.ThriftType));
            }
            this._parameterCodecs = builder.ToImmutableDictionary();

            // Build a mapping from thrift parameter ID to a position in the formal argument list
            var parameterOrderingBuilder = ImmutableDictionary.CreateBuilder<short, short>();
            short argumentPosition = 0;
            foreach (ThriftFieldMetadata fieldMetadata in methodMetadata.Parameters)
            {
                parameterOrderingBuilder.Add(fieldMetadata.Id, argumentPosition++);
            }
            _thriftParameterIdToCSharpArgument = parameterOrderingBuilder.ToImmutableDictionary();

            var exceptions = ImmutableDictionary.CreateBuilder<Type, ExceptionProcessor>();
            foreach (var entry in methodMetadata.Exceptions)
            {
                ExceptionProcessor processor = new ExceptionProcessor(entry.Key, codecManager.GetCodec(entry.Value));
                exceptions.Add(entry.Value.CSharpType, processor);
            }
            this._exceptionCodecs = exceptions.ToImmutableDictionary();

            this._successCodec = codecManager.GetCodec(methodMetadata.ReturnType);
        }

        public String QualifiedName { get; }

        public String ServiceName { get; }

        public String Name { get; }

        private Object[] ReadArguments(TProtocol inProtocol)
        {
            try
            {
                int numArgs = _method.GetParameters().Length;
                Object[] args = new Object[numArgs];
                TProtocolReader reader = new TProtocolReader(inProtocol);

                // Map incoming arguments from the ID passed in on the wire to the position in the
                // java argument list we expect to see a parameter with that ID.
                reader.ReadStructBegin();
                while (reader.NextField())
                {
                    short fieldId = reader.GetFieldId();

                    IThriftCodec codec = null;
                    if (!_parameterCodecs.TryGetValue(fieldId, out codec))
                    {
                        // unknown field
                        reader.SkipFieldData();
                    }
                    else
                    {
                        // Map the incoming arguments to an array of arguments ordered as the java
                        // code for the handler method expects to see them
                        args[_thriftParameterIdToCSharpArgument[fieldId]] = reader.ReadField(codec);
                    }
                }
                reader.ReadStructEnd();

                // Walk through our list of expected parameters and if no incoming parameters were
                // mapped to a particular expected parameter, fill the expected parameter slow with
                // the default for the parameter type.
                int argumentPosition = 0;
                foreach (ThriftFieldMetadata argument in _parameters)
                {
                    if (args[argumentPosition] == null)
                    {
                        Type argumentType = argument.ThriftType.CSharpType;

                        if (!argumentType.Equals(typeof(void)))
                        {
                            args[argumentPosition] = ThriftyUtilities.GetDefaultValue(argumentType);
                        }
                    }
                    argumentPosition++;
                }

                return args;
            }
            catch (TProtocolException e)
            {
                // TProtocolException is the only recoverable exception
                // Other exceptions may have left the input stream in corrupted state so we must
                // tear down the socket.
                throw new TApplicationException(TApplicationException.ExceptionType.ProtocolError, e.Message);
            }
        }

        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> Cache =
            new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        private Task<Object> InvokeMethod(IRequestContext requestContext, Object[] args)
        {
            var source = new TaskCompletionSource<Object>();
            try
            {
                var func = Cache.GetOrAdd(_method, meth =>
                {
                    var p1 = Expression.Parameter(typeof(object));
                    var p2 = Expression.Parameter(typeof(object[]));
                    var call = Expression.Call(Expression.Convert(p1, meth.DeclaringType), meth,
                        meth.GetParameters().Select((p, i) => Expression.Convert(Expression.ArrayIndex(p2, Expression.Constant(i)), p.ParameterType)));
                    if (meth.ReturnType != typeof(void))
                        return Expression
                            .Lambda<Func<object, object[], object>>(Expression.Convert(call, typeof(object)), p1, p2)
                            .Compile();
                    return Expression
                        .Lambda<Func<object, object[], object>>(Expression.Block(call, p1), p1, p2)
                        .Compile();
                });
                object response = null;
                try
                {
                    _logger.LogDebug(new EventId(0, "Thrift"), $"start execution method {_method.Name}[{_method.DeclaringType?.FullName}]. ");
                    Object serviceInstance = _serviceFactory(requestContext);
                    if (serviceInstance == null)
                    {
                        source.TrySetException(new ThriftyException($"{nameof(IServiceLocator)}.{nameof(IServiceLocator.GetService)} return null ."));
                    }
                    response = func(serviceInstance, args);
                    _logger.LogDebug(new EventId(0, "Thrift"), $"The execution of method {_method.Name}[{_method.DeclaringType?.FullName}] is complete. ");
                }
                catch (Exception e)
                {
                    var cause = e.GetBaseException() ?? e;
                    _logger.LogDebug(new EventId(0, "Thrift"), cause, $"An error occurred execute method {_method.Name}[{_method.DeclaringType?.FullName}]. ");
                    source.TrySetException(cause);
                    return source.Task;
                }
                if (response is Task)
                {
                    var typeInfo = response.GetType().GetTypeInfo();
                    if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Task<>)) //判断是否为 Task<>
                    {
                        source.TrySetResult(((dynamic)response).Result);
                    }
                    else
                    {
                        source.TrySetResult(null);
                    }
                }
                else
                {
                    source.TrySetResult(response);
                }
            }
            catch (AggregateException ex)
            {
                _logger.LogError(new EventId(0, "Thrift"), ex, $"An error occurred execute method {_method.Name}[{_method.DeclaringType?.FullName}].");
                source.SetException(ex);
            }
            catch (ArgumentException aex)
            {
                _logger.LogError(new EventId(0, "Thrift"), aex, $"An error occurred execute method {_method.Name}[{_method.DeclaringType?.FullName}].");
                // These really should never happen, since the method metadata should have prevented it
                source.TrySetException(aex);
            }
            //catch (TargetInvocationException e)
            //{
            //    var cause = e.GetBaseException();
            //    source.TrySetException(cause ?? e);
            //}

            return source.Task;
        }

        private void WriteResponse(TProtocol outProtocol,
                                   int sequenceId,
                                   TMessageType responseType,
                                   String responseFieldName,
                                   short responseFieldId,
                                   IThriftCodec responseCodec,
                                   Object result)
        {
            outProtocol.WriteMessageBegin(new TMessage(this.QualifiedName, responseType, sequenceId));

            TProtocolWriter writer = new TProtocolWriter(outProtocol);
            writer.WriteStructBegin(_resultStructName);
            writer.WriteField(responseFieldName, (short)responseFieldId, responseCodec, result);
            writer.WriteStructEnd();

            outProtocol.WriteMessageEnd();
            outProtocol.Transport.Flush();
        }

        public Task<Boolean> Process(TProtocol inProtocol, TProtocol outProtocol, int sequenceId, ContextChain contextChain)
        {
            // read args
            contextChain.PreRead();
            Object[] args = ReadArguments(inProtocol);
            contextChain.PostRead(args);
            IRequestContext requestContext = RequestContexts.GetCurrentContext();

            inProtocol.ReadMessageEnd();
            if (!_oneway)
            {
                // invoke method
                var invokeFuture = InvokeMethod(requestContext, args);
                return CompleteMethodInvoke(invokeFuture, outProtocol, sequenceId, contextChain, requestContext).ContinueWith(t =>
                {
                    contextChain.Done();
                    return t.Result;
                });
            }
            else
            {
                InvokeOneWayMethod(requestContext, args, contextChain);
                //完全不用处理，因为客户端没有记录 oneway 的 sequence id，处理反而造成麻烦。
                //try
                //{
                //    WriteResponse(outProtocol,
                //                          sequenceId,
                //                          TMessageType.Reply,
                //                          "success",
                //                          (short)0,
                //                          _successCodec,
                //                          null);

                //}
                //catch (Exception e)
                //{
                //    TaskCompletionSource<Boolean> resultFuture = new TaskCompletionSource<bool>();
                //    resultFuture.TrySetException(e);
                //    return resultFuture.Task;
                //}
                return Task.FromResult(true);
            }

        }

        private void InvokeOneWayMethod(IRequestContext context, object[] args, ContextChain chain)
        {
            Task.Run(() =>
            {
                // invoke method
                var invokeFuture = InvokeMethod(context, args);
                invokeFuture.ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogError(new EventId(0, "Thrift"), t.Exception, $"An error occurred execute method {_method.Name}[{_method.DeclaringType?.FullName}].");
                    }

                    chain.Done();
                });
            });
        }

        private Task<bool> CompleteMethodInvoke(Task<object> invokeTask, TProtocol outProtocol, int sequenceId, ContextChain contextChain, IRequestContext requestContext)
        {
            TaskCompletionSource<Boolean> resultFuture = new TaskCompletionSource<bool>();

            invokeTask.ContinueWith(t =>
            {
                if (t.Exception == null)
                {
                    var result = t.Result;
                    IRequestContext oldRequestContext = RequestContexts.GetCurrentContext();
                    RequestContexts.SetCurrentContext(requestContext);

                    // write success reply
                    try
                    {
                        contextChain.PreWrite(result);

                        WriteResponse(outProtocol,
                                      sequenceId,
                                      TMessageType.Reply,
                                      "success",
                                      (short)0,
                                      _successCodec,
                                      result);

                        contextChain.PostWrite(result);
                        
                        resultFuture.TrySetResult(true);
                    }
                    catch (Exception e)
                    {
                        // An exception occurred trying to serialize a return value onto the output protocol
                        resultFuture.TrySetException(e);
                    }
                    finally
                    {
                        RequestContexts.SetCurrentContext(oldRequestContext);
                    }
                }
                else
                {
                    IRequestContext oldRequestContext = RequestContexts.GetCurrentContext();
                    RequestContexts.SetCurrentContext(requestContext);
                    try
                    {
                        contextChain.PreWriteException(t.Exception);
                        ExceptionProcessor exceptionCodec;

                        if (!_exceptionCodecs.TryGetValue(t.Exception.GetType(), out exceptionCodec))
                        {
                            // In case the method throws a subtype of one of its declared
                            // exceptions, exact lookup will fail. We need to simulate it.
                            // (This isn't a problem for normal returns because there the
                            // output codec is decided in advance.)
                            foreach (var entry in _exceptionCodecs)
                            {
                                if (entry.Key.GetTypeInfo().IsAssignableFrom(t.Exception.GetType()))
                                {
                                    exceptionCodec = entry.Value;
                                    break;
                                }
                            }
                        }

                        if (exceptionCodec != null)
                        {
                            contextChain.DeclaredUserException(t.Exception, exceptionCodec.Codec);
                            // write expected exception response
                            WriteResponse(
                                outProtocol,
                                sequenceId,
                                TMessageType.Reply,
                                "exception",
                                exceptionCodec.Id,
                                exceptionCodec.Codec,
                                t);
                            contextChain.PostWriteException(t.Exception);
                        }
                        else
                        {
                            contextChain.UndeclaredUserException(t.Exception);
                            // unexpected exception
                            TApplicationException applicationException =
                                ThriftServiceProcessor.CreateAndWriteApplicationException(
                                    outProtocol,
                                    requestContext,
                                    _method.Name,
                                    sequenceId,
                                    TApplicationException.ExceptionType.InternalError,
                                    $"Internal error processing '{_method.Name}'.",
                                    t.Exception,
                                    _logger);

                            contextChain.PostWriteException(applicationException);
                        }
                        if (!_oneway) resultFuture.TrySetResult(true);
                    }
                    catch (Exception e)
                    {
                        // An exception occurred trying to serialize an exception onto the output protocol
                        resultFuture.TrySetException(e);
                    }
                    finally
                    {
                        RequestContexts.SetCurrentContext(oldRequestContext);
                    }
                }
            });
            return resultFuture.Task;
        }

        private sealed class ExceptionProcessor
        {
            public ExceptionProcessor(short id, IThriftCodec coded)
            {
                this.Id = id;
                this.Codec = coded;
            }

            public short Id { get; }

            public IThriftCodec Codec { get; }
        }
    }
}
