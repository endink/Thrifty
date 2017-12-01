using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.AppInfo;
using Thrifty.MicroServices.Client.Pooling;
using Thrifty.MicroServices.Commons;
using Thrifty.MicroServices.Ribbon;
using Thrifty.MicroServices.Ribbon.Rules;
using Thrifty.Nifty.Client;
using Thrifty.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Thrifty.Codecs;

namespace Thrifty.MicroServices.Client
{
    public partial class ThriftyClient : IDisposable
    {
        private static readonly ModuleBuilder ModuleBuilder;
        private static readonly MethodInfo GetMethodFromHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });
        private static readonly MethodInfo CallMethodInfo = typeof(Func<ThriftyClient, MethodInfo, ClientSslConfig, Ribbon.Server, string, string, object[], object>).GetMethod("Invoke");
        private static readonly ConstructorInfo DebuggerBrowsableConstructor = typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) });

        static ThriftyClient()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Thrifty.MicroServices.DynamicAssembly"),
                AssemblyBuilderAccess.Run);
            ModuleBuilder = assemblyBuilder.DefineDynamicModule("<main>");
        }

        private readonly ILogger _logger;
        private readonly IPing _swiftyPing;
        private readonly Lazy<HashedWheelEvictionTimer> _timeoutTimer;
        private readonly ThriftyClientOptions _swiftyClientOptions;
        private readonly IServerStatusCollector _collector = new DefaultServerStatusCollector();

        private readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> _methodCache = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        private readonly ConcurrentDictionary<Type, Func<ThriftyClient, Ribbon.Server, string, string, ClientSslConfig, object>> _proxyCreater = new ConcurrentDictionary<Type, Func<ThriftyClient, Ribbon.Server, string, string, ClientSslConfig, object>>();

        private readonly Dictionary<LoadBalanceKey, LoadBalancerCommand> _balancerCommands = new Dictionary<LoadBalanceKey, LoadBalancerCommand>();

        private static readonly ConcurrentDictionary<LoadBalanceKey, object> Syncs = new ConcurrentDictionary<LoadBalanceKey, object>();



        private volatile bool _disposed;
        private IClientConnectionPool _channelPools;
        private ThriftClientManager _thriftClientManager;

        public ThriftyClient(ThriftyClientOptions swiftyClientOptions)
        {
            _swiftyClientOptions = swiftyClientOptions ?? new ThriftyClientOptions();
            var loggerFactory = _swiftyClientOptions.LoggerFactory;

            _logger = loggerFactory?.CreateLogger(typeof(ThriftyClient)) ?? NullLogger.Instance;

            _thriftClientManager = new ThriftClientManager(new ThriftCodecManager(),
                new NiftyClient(swiftyClientOptions.NettyClientConfig ?? new NettyClientConfig(), _swiftyClientOptions.LoggerFactory),
                Enumerable.Empty<ThriftClientEventHandler>(), loggerFactory: _swiftyClientOptions.LoggerFactory);

            _swiftyPing = new ThriftyPing(_thriftClientManager);
            _timeoutTimer = new Lazy<HashedWheelEvictionTimer>(() =>
            {
                var timer = Utils.Timer;
                return new HashedWheelEvictionTimer(timer);
            }, true);

            _channelPools = swiftyClientOptions.ConnectionPoolEnabled ?
                new NiftyClientChannelPool(_thriftClientManager, _timeoutTimer.Value, swiftyClientOptions)
                : (IClientConnectionPool)new NoneClientChannelPool(_thriftClientManager, swiftyClientOptions);


            if (_swiftyClientOptions.EurekaEnabled)
            {
                var eureka = _swiftyClientOptions.Eureka;
                DiscoveryManager.Instance.Initialize(eureka, loggerFactory);

                var interval = eureka.RegistryFetchIntervalSeconds * 1000 / 2;//TODO last的事件处理 
                _timeoutTimer.Value.Schedule(this.RefreshServers, TimeSpan.FromMilliseconds(interval));
            }
        }


        private void RefreshServers()
        {
            var all = _balancerCommands.ToArray();
            var count = all.Length;
            for (var i = 0; i < count; i++)
            {
                var item = all[i];
                var vipAddress = item.Key.VipAddress;
                var balancer = item.Value.LoadBalancer;

                var servers = DiscoveryManager.Instance.Client
                    .GetInstancesByVipAddress(vipAddress, _swiftyClientOptions.Secure)
                    .Select(ins => new DiscoveryEnabledServer(ins, _swiftyClientOptions.Secure,
                        _swiftyClientOptions.Eureka.AddressUsage) as Ribbon.Server).ToList();

                balancer.AddServers(servers);
            }
        }


        private static bool CircuitTrippingException(Exception e)
        {
            return false;
        }

        private static bool RetriableException(Exception exception, bool sameServer)
        {
            if (exception is ThriftyTransportException) return true;
            return false;
        }


        private LoadBalancerCommand GetCommand(LoadBalanceKey key)
        {
            if (_swiftyClientOptions.Eureka == null)
            {
                return null;
            }
            if (_balancerCommands.TryGetValue(key, out LoadBalancerCommand x))
            {
                return x;
            }
            lock (Syncs.GetOrAdd(key, k => new object()))
            {
                if (_balancerCommands.TryGetValue(key, out LoadBalancerCommand command))
                {
                    return command;
                }

                var version = key.Version;
                var vipAddress = key.VipAddress;
                var rule = new FilterableRule(new VersionAffinityFilter(version));
                var eureka = _swiftyClientOptions.Eureka;

                var balancer = LoadBalancerBuilder.NewBuilder()
                    .WithPing(_swiftyPing)
                    .WithPingStrategy(_swiftyClientOptions.PingStrategy)
                    .WithRule(rule)
                    .Build($"{vipAddress}:{version}", eureka.RegistryFetchIntervalSeconds * 1000 / 2);

                var collector = new DefaultServerStatusCollector();
                var retryHandler = new DefaultRetryHandler(
                    _swiftyClientOptions.RetriesSameServer,
                    _swiftyClientOptions.RetriesNextServer,
                    _swiftyClientOptions.RetryEnabled,
                    CircuitTrippingException,
                    RetriableException);
                command = new LoadBalancerCommand(balancer, collector, retryHandler, null);

                var servers = DiscoveryManager.Instance.Client
                    .GetInstancesByVipAddress(vipAddress, _swiftyClientOptions.Secure)
                    .Where(ins => ins.Status == InstanceStatus.UP)
                    .Select(ins => new DiscoveryEnabledServer(ins, _swiftyClientOptions.Secure, _swiftyClientOptions.Eureka.AddressUsage) as Ribbon.Server)
                    .ToList();

                balancer.AddServers(servers);

                _balancerCommands.Add(key, command);
                return command;
            }
        }

        private object GetStub(ThriftyRequest request, ChannelKey key, INiftyClientChannel channel)
        {
            var method = request.Method;
            var service = method.DeclaringType;
            var client = _thriftClientManager.CreateClient(channel, service, $"{service.Name }{key}", _swiftyClientOptions.EventHandlers);
            request.Stub = client;
            request.ChannelKey = key;
            return client;
        }

        [DebuggerHidden]
        private static Func<object, object[], object> CreateFunc(MethodInfo method)
        {
            var p1 = Expression.Parameter(typeof(object));
            var p2 = Expression.Parameter(typeof(object[]));
            var type = method.DeclaringType;
            if (type == null)
            {
                throw new NotSupportedException("not supported the Type with no DeclaringType");
            }
            var instance = Expression.Convert(p1, type);

            var parameters = method.GetParameters();
            Expression[] paramExpressions = new Expression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var arrayIndex = Expression.ArrayIndex(p2, Expression.Constant(i));
                paramExpressions[i] = Expression.Convert(arrayIndex, parameter.ParameterType);
            }

            var methodCall = Expression.Call(instance, method, paramExpressions);

            Expression<Func<object, object[], object>> result;
            if (method.ReturnType != typeof(void))
            {
                var convert = Expression.Convert(methodCall, typeof(object));
                result = Expression.Lambda<Func<object, object[], object>>(convert, p1, p2);
            }
            else
            {
                var block = Expression.Block(methodCall, Expression.Constant(null));
                result = Expression.Lambda<Func<object, object[], object>>(block, p1, p2);
            }
            return result.Compile();
        }

        [DebuggerHidden]
        private ThriftyResponse Execute(ThriftyRequest request, Ribbon.Server server, ClientSslConfig sslConfig)
        {
            var func = _methodCache.GetOrAdd(request.Method, CreateFunc);

            _logger.LogDebug(new EventId(0, "ThriftyClient"), $"use {server}");

            var discoveryEnabledServer = server as DiscoveryEnabledServer;
            var host = discoveryEnabledServer == null ? server.Host : discoveryEnabledServer.InstanceInfo.IpAddr;
            var port = discoveryEnabledServer?.Port ?? server.Port;
            var address = string.Compare("localhost", host, StringComparison.OrdinalIgnoreCase) == 0
                ? IPAddress.Loopback
                : IPAddress.Parse(host);

            var key = new ChannelKey(new IPEndPoint(address, port), sslConfig,
                request.ConnectTimeout, request.ReveiveTimeout, request.WriteTimeout, request.ReadTimeout);

            using (var channel = new PooledClientChannel(this._channelPools, key))
            {
                var proxy = GetStub(request, key, channel);
                var result = func(proxy, request.Args);
                return new ThriftyResponse(result, true);
            }
        }

        [DebuggerHidden]
        private ThriftyResponse RibbonCall(ThriftyRequest request, ClientSslConfig sslConfig, Ribbon.Server server, string version, string vipAddress)
        {
            if (server != null)
            {
                return Execute(request, server, sslConfig);
            }
            var command = GetCommand(new LoadBalanceKey(version, vipAddress));
            if (command == null)
            {
                throw new ThriftyException("need config Eureka");
            }
            return command.Submit(s =>
             {
                 var result = Execute(request, s, sslConfig);
                 return Task.FromResult(result);
             }).GetAwaiter().GetResult();
        }

        //[DebuggerHidden]
        private object Call(MethodInfo method, ClientSslConfig sslConfig, Ribbon.Server server, string version, string vipAddress, object[] args)
        {
            var request = new ThriftyRequest(_swiftyClientOptions.RetryEnabled,
                _swiftyClientOptions.RetriesNextServer, _swiftyClientOptions.RetriesSameServer,
                _swiftyClientOptions.ReadTimeoutMilliseconds, _swiftyClientOptions.WriteTimeoutMilliseconds,
                _swiftyClientOptions.ReceiveTimeoutMilliseconds,
                _swiftyClientOptions.ConnectTimeoutMilliseconds, args, method);
            var response = RibbonCall(request, sslConfig, server, version, vipAddress);
            return response.Payload;
        }

        [DebuggerHidden]
        private T InnerCreate<T>(string version, string vipAddress, Ribbon.Server server, ClientSslConfig sslConfig) where T : class
        {
            this.ThrowIfDisposed();
            var type = typeof(T);
            if (!type.GetTypeInfo().IsInterface)
                throw new NotSupportedException($"{type} must be an interface");
            var proxy = FakeProxy<T>.Create(this, server, version, vipAddress, sslConfig);
            return proxy;
        }

        [DebuggerHidden]
        private object InnerCreate(Type type, string version, string vipAddress, Ribbon.Server server, ClientSslConfig sslConfig)
        {
            this.ThrowIfDisposed();

            if (!type.GetTypeInfo().IsInterface)
                throw new ThriftyException($"swifty service must be an interface, but '{type}' was not.");
            return _proxyCreater.GetOrAdd(type, t =>
            {
                var fullType = typeof(FakeProxy<>).MakeGenericType(t);
                var method = fullType.GetMethod("Create", new[] { typeof(ThriftyClient), typeof(Ribbon.Server), typeof(string), typeof(string), typeof(ClientSslConfig) });
                var p1 = Expression.Parameter(typeof(ThriftyClient));
                var p2 = Expression.Parameter(typeof(Ribbon.Server));
                var p3 = Expression.Parameter(typeof(string));
                var p4 = Expression.Parameter(typeof(string));
                var p5 = Expression.Parameter(typeof(ClientSslConfig));
                var call = Expression.Call(method, p1, p2, p3, p4, p5);
                var lambda = Expression.Lambda<Func<ThriftyClient, Ribbon.Server, string, string, ClientSslConfig, object>>(call, p1, p2, p3, p4, p5).Compile();
                return lambda;
            })(this, server, version, vipAddress, sslConfig);
        }

        public T Create<T>(string version, string vipAddress, ClientSslConfig ssl = null) where T : class => InnerCreate<T>(version, vipAddress, null, ssl);
        public T Create<T>(string hostAndPort, ClientSslConfig ssl = null) where T : class => InnerCreate<T>(null, null, new Ribbon.Server(hostAndPort), ssl);

        public object Create(Type type, string version, string vipAddress, ClientSslConfig sslConfig = null) => InnerCreate(type, version, vipAddress, null, sslConfig);
        public object Create(Type type, string hostAndPort, ClientSslConfig sslConfig = null) => InnerCreate(type, null, null, new Ribbon.Server(hostAndPort), sslConfig);

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _disposed = true;
                if (_timeoutTimer.IsValueCreated)
                {
                    _timeoutTimer.Value.Dispose();
                }

                _channelPools?.Dispose();
                _thriftClientManager?.Dispose();

                _channelPools = null;
                _thriftClientManager = null;
            }
        }
    }
}
