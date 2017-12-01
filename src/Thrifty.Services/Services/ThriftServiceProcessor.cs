using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Nifty.Core;
using Thrift;
using Thrift.Protocol;
using Thrifty.Nifty.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.Codecs;
using Thrifty.Services.Metadata;
using System.Collections.Immutable;

namespace Thrifty.Services
{
    public class ThriftServiceProcessor : INiftyProcessor
    {
        private ILogger _logger;

        private IEnumerable<ThriftEventHandler> _eventHandlers;

        public ThriftServiceProcessor(ILoggerFactory loggerFactory = null, params Object[] services)
            :this(services, null, null, null)
        {

        }

        public ThriftServiceProcessor(
            IEnumerable<Object> services,
            ThriftCodecManager codecManager = null,
            IEnumerable<ThriftEventHandler> eventHandlers = null,
            ILoggerFactory loggerFactory = null)
        {
            if (services == null || !services.Any())
            {
                throw new ArgumentException($"{nameof(ThriftServiceProcessor)} 构造函数参数 {nameof(services)} 不能为空或空集合。");
            }

            codecManager = codecManager ?? new ThriftCodecManager();

            this.BuildMethods(services.ToDictionary(o=>o.GetType(), o=>new Func<IRequestContext, object>(ctx => o)), codecManager, loggerFactory);
            _logger = loggerFactory?.CreateLogger<ThriftServiceProcessor>() ?? (ILogger)NullLogger.Instance;
            _eventHandlers = eventHandlers ?? Enumerable.Empty<ThriftEventHandler>();
        }

        public ThriftServiceProcessor(
            IServiceLocator serviceLocator,
            IEnumerable<Type> serviceTypes,
            ThriftCodecManager codecManager = null,
            IEnumerable<ThriftEventHandler> eventHandlers = null,
            ILoggerFactory loggerFactory = null)
        {
            Guard.ArgumentNotNull(serviceLocator, nameof(serviceLocator));
            if (serviceTypes == null || !serviceTypes.Any())
            {
                throw new ArgumentException($"{nameof(ThriftServiceProcessor)} 构造函数参数 {nameof(serviceTypes)} 不能为空或空集合。");
            }

            codecManager = codecManager ?? new ThriftCodecManager();

            var processorMap = ImmutableDictionary.CreateBuilder<String, ThriftMethodProcessor>();

            this.BuildMethods(
                serviceTypes.ToDictionary(t => t, t => new Func<IRequestContext, object>(ctx=>serviceLocator.GetService(ctx, t))),
                codecManager, loggerFactory);

            _logger = loggerFactory?.CreateLogger<ThriftServiceProcessor>() ?? (ILogger)NullLogger.Instance;
            _eventHandlers = eventHandlers ?? Enumerable.Empty<ThriftEventHandler>();
        }

        public IDictionary<String, ThriftMethodProcessor> Methods { get; private set; }


        private void BuildMethods(IDictionary<Type, Func<IRequestContext, Object>> services, ThriftCodecManager codecManager, ILoggerFactory loggerFactory)
         {
            var processorMap = ImmutableDictionary.CreateBuilder<String, ThriftMethodProcessor>();
            foreach (Type serviceType in services.Keys)
            {
                ThriftServiceMetadata serviceMetadata = new ThriftServiceMetadata(serviceType, codecManager.Catalog);
                foreach (ThriftMethodMetadata methodMetadata in serviceMetadata.Methods.Values)
                {
                    String methodName = methodMetadata.QualifiedName;
                    ThriftMethodProcessor methodProcessor = new ThriftMethodProcessor(services[serviceType], serviceMetadata.Name, methodMetadata, codecManager, loggerFactory);
                    if (processorMap.ContainsKey(methodName))
                    {
                        throw new ThriftyException($"Multiple ThriftMethod-attributed methods named '{methodMetadata.Name}' found in the given service type '{serviceType.Name}'");
                    }
                    processorMap.Add(methodName, methodProcessor);
                }
            }
            this.Methods = processorMap.ToImmutableDictionary();
        }


        public static TApplicationException CreateAndWriteApplicationException(TProtocol outProtocol,
            IRequestContext requestContext,
            string methodName,
            int sequenceId,
            TApplicationException.ExceptionType exceptionType,
            string message,
            AggregateException cause,
            ILogger logger)
        {
            // unexpected exception
            String error = message;
            if (cause != null)
            {
                error = String.Concat(error, $"{Environment.NewLine}{cause.ToString()}");
            }
            TApplicationException applicationException = new TApplicationException(exceptionType, error);

            return WriteApplicationException(outProtocol, requestContext, methodName, sequenceId, applicationException, logger);
        }

        public static TApplicationException WriteApplicationException(
            TProtocol outputProtocol,
            IRequestContext requestContext,
            String methodName,
            int sequenceId,
            TApplicationException applicationException,
            ILogger logger)
        {
            logger?.LogError(default(EventId), applicationException, applicationException.Message);
            TNiftyTransport requestTransport = (requestContext as NiftyRequestContext)?.NiftyTransport;

            // Application exceptions are sent to client, and the connection can be reused
            outputProtocol.WriteMessageBegin(new TMessage(methodName, TMessageType.Exception, sequenceId));
            applicationException.Write(outputProtocol);
            outputProtocol.WriteMessageEnd();
            if (requestTransport != null)
            {
                requestTransport.setTApplicationException(applicationException);
            }
            outputProtocol.Transport.Flush();

            return applicationException;
        }

        public Task<bool> ProcessAsync(TProtocol protocolIn, TProtocol protocolOut, IRequestContext requestContext)
        {
            String methodName = null;
            int sequenceId = 0;
            try
            {
                return this.ProcessCoreAsync(protocolIn, protocolOut, requestContext, out methodName, out sequenceId);
            }
            catch (TApplicationException ex)
            {
                WriteApplicationException(protocolOut, requestContext, methodName, sequenceId, ex, _logger);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                TaskCompletionSource<Boolean> resultFuture = new TaskCompletionSource<bool>();
                resultFuture.TrySetException(e);
                return resultFuture.Task;
            }
        }

        public Task<bool> ProcessCoreAsync(TProtocol protocolIn, TProtocol protocolOut, IRequestContext requestContext, out String methodName, out int sequenceId)
        {
            TMessage message = protocolIn.ReadMessageBegin();
            methodName = message.Name;
            sequenceId = message.SeqID;

            // lookup method
            ThriftMethodProcessor method;
            if (!this.Methods.TryGetValue(methodName, out method))
            {
                //TProtocolUtil.Skip(protocolIn, TType.Struct);
                CreateAndWriteApplicationException(protocolOut,
                    requestContext, methodName,
                    sequenceId,
                    TApplicationException.ExceptionType.UnknownMethod,
                    $"Invalid method name: '{methodName}'",
                    null,
                    _logger);

                return Task.FromResult(true);
            }

            switch (message.Type)
            {
                case TMessageType.Call:
                case TMessageType.Oneway:
                    // Ideally we'd check the message type here to make the presence/absence of
                    // the "oneway" keyword annotating the method matches the message type.
                    // Unfortunately most clients send both one-way and two-way messages as CALL
                    // message type instead of using ONEWAY message type, and servers ignore the
                    // difference.
                    break;

                default:
                    TProtocolUtil.Skip(protocolIn, TType.String);
                    CreateAndWriteApplicationException(protocolOut,
                        requestContext,
                        methodName,
                        sequenceId,
                        TApplicationException.ExceptionType.InvalidMessageType,
                        $"Received invalid message type {message.Type} from client",
                        null,
                        _logger);
                    return Task.FromResult(true);
            }


            TaskCompletionSource<Boolean> resultFuture = new TaskCompletionSource<bool>();
            // invoke method
            ContextChain context = new ContextChain(this._eventHandlers, method.QualifiedName, requestContext);
            Task<Boolean> processResult = method.Process(protocolIn, protocolOut, sequenceId, context);
            processResult.ContinueWith(task => 
            {
                if (task.Exception == null)
                {
                    resultFuture.TrySetResult(task.Result);
                }
                else
                {
                    _logger.LogError(default(EventId), task.Exception, $"Failed to process method [{method.QualifiedName}] of service [{method.ServiceName}].");
                    resultFuture.TrySetException(task.Exception);
                }
            });
            return resultFuture.Task;
        }
    }
}
