using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Steeltoe.Discovery.Eureka;
using Thrifty.Codecs;
using Thrifty.MicroServices.Commons;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Thrifty.Nifty.Ssl;

namespace Thrifty.MicroServices.Server
{
    public class ThriftyBootstrap
    {
        /// <summary>
        /// 服务器配置
        /// </summary>
        private readonly ThriftyServerOptions _serverConfig;

        private IThriftCodec[] _codecs;
        private List<ThriftEventHandler> _handles;

        private IEurekaInstanceConfig _instanceConfig;
        private ThriftyServer _server;
        private readonly HashSet<ThriftyServiceDescriptor> _services;
        private readonly IServiceLocator _serviceLocator;
        private readonly InstanceDescription _instanceDescription;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        /// <summary>
        /// 服务启动配置实例
        /// </summary>
        /// <param name="services">服务实例（这些服务将以单例模式运行）。</param>
        /// <param name="serverConfig">服务器配置</param>
        /// <param name="instanceDescription">服务器实例描述</param>
        /// <param name="loggerFactory">日志</param>
        public ThriftyBootstrap(
            IEnumerable<Object> services,
            ThriftyServerOptions serverConfig,
            InstanceDescription instanceDescription,
            ILoggerFactory loggerFactory = null) : this(new InstanceServiceLocator(services), serverConfig, instanceDescription, loggerFactory)
        {
            Guard.ArgumentNotNull(services, nameof(services));
        }

        /// <summary>
        /// 服务启动配置实例
        /// </summary>
        /// <param name="serviceLocator">服务定位器</param>
        /// <param name="serverConfig">服务器配置</param>
        /// <param name="instanceDescription">服务器实例描述</param>
        /// <param name="loggerFactory">日志</param>
        public ThriftyBootstrap(
            IServiceLocator serviceLocator,
            ThriftyServerOptions serverConfig,
            InstanceDescription instanceDescription,
            ILoggerFactory loggerFactory = null)
        {
            Guard.ArgumentNotNull(serviceLocator, nameof(serviceLocator));
            Guard.ArgumentNotNull(serverConfig, nameof(serverConfig));
            Guard.ArgumentNotNull(instanceDescription, nameof(instanceDescription));

            this._logger = this._loggerFactory?.CreateLogger<ThriftyBootstrap>() ?? (ILogger)NullLogger.Instance;

            this._serverConfig = serverConfig;
            var checher = new ThriftyHealthCheck();
            this._serviceLocator = new DelegateServiceLocator((ctx, x) => x == typeof(IHealthCheck) ? checher : serviceLocator.GetService(ctx, x));
            this._instanceDescription = instanceDescription;
            this._loggerFactory = loggerFactory;
            this._services = new HashSet<ThriftyServiceDescriptor> { new ThriftyServiceDescriptor(typeof(IHealthCheck)) };
        }
        private ThriftyServer ThriftyServer
        {
            get
            {
                if (_server != null) return _server;
                _server = new ThriftyServer(
                    this._serviceLocator,
                    this._serverConfig,
                    this._handles ?? new List<ThriftEventHandler>(),
                    this._services.Select(s => s.ServiceType).ToArray(),
                    this._codecs,
                    this._serverConfig.Ssl,
                    this._loggerFactory);
                return _server;
            }
        }


        /// <summary>
        /// 传入自定义编码器
        /// </summary>
        /// <param name="codecs"></param>
        /// <returns></returns>
        public ThriftyBootstrap Codecs(params IThriftCodec[] codecs)
        {
            this.CheckCanAddFutures();
            this._codecs = codecs;
            return this;
        }

        /// <summary>
        /// 传入自定义Handlers
        /// </summary>
        /// <param name="handlers">handler初始化器</param>
        /// <returns>this</returns>
        public ThriftyBootstrap Handles(Action<IList<ThriftEventHandler>> handlers)
        {
            this.CheckCanAddFutures();
            if (this._handles == null)
                this._handles = new List<ThriftEventHandler>();

            handlers(this._handles);
            return this;
        }

        ///// <summary>
        ///// 传入服务实例
        ///// </summary>
        ///// <param name="services"></param>
        ///// <returns></returns>
        //public ServerBootStrap Services(params object[] services)
        //{
        //    this._services = services;
        //    return this;
        //}

        /// <summary>
        /// 设置服务类型
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public ThriftyBootstrap AddServices(params Type[] types)
        {
            return this.AddServices(types?.Select(t => new ThriftyServiceDescriptor(t))?.ToArray());
        }
        /// <summary>
        /// 设置服务类型
        /// </summary>
        /// <returns></returns>
        public ThriftyBootstrap AddServices(params ThriftyServiceDescriptor[] services)
        {
            this.CheckCanAddFutures();
            if (services?.Any() ?? false)
            {
                var notInterfaces = services.Where(x => !x.ServiceType.GetTypeInfo().IsInterface);
                if (notInterfaces.Any())
                {
                    throw new ThriftyException($"{string.Join(",", notInterfaces.Select(x => x.ServiceType.FullName))} cant used as a service, because they are not interfaces");
                }
                foreach (var t in services)
                {
                    this._services.Add(t);
                }
            }

            return this;
        }

        /// <summary>
        /// 设置服务类型
        /// </summary>
        /// <returns></returns>
        public ThriftyBootstrap AddService(Type serviceType, String version)
        {
            Guard.ArgumentNotNull(serviceType, nameof(serviceType));
            Guard.ArgumentNullOrWhiteSpaceString(version, nameof(version));
            return this.AddServices(new ThriftyServiceDescriptor(serviceType, version));
        }

        /// <summary>
        /// 设置服务类型
        /// </summary>
        /// <returns></returns>
        public ThriftyBootstrap AddService<T>()
        {
            return this.AddServices(typeof(T));
        }

        /// <summary>
        /// 绑定地址/端口
        /// </summary>
        /// <param name="host">host地址</param>
        /// <param name="port">端口</param>
        /// <returns></returns>
        public ThriftyBootstrap Bind(string host, int port)
        {
            this.CheckCanAddFutures();
            this._serverConfig.BindingAddress = host;
            this._serverConfig.Port = port;
            return this;
        }

        /// <summary>
        /// 设置backlog
        /// </summary>
        /// <param name="backlog">tcp参数backlog</param>
        /// <returns></returns>
        public ThriftyBootstrap AcceptBacklog(int backlog)
        {
            this.CheckCanAddFutures();
            this._serverConfig.AcceptBacklog = backlog;
            return this;
        }

        /// <summary>
        /// 设置连接限制
        /// </summary>
        /// <param name="connectionLimit">连接限制数</param>
        /// <returns></returns>
        public ThriftyBootstrap ConnectionLimit(int connectionLimit)
        {
            this.CheckCanAddFutures();
            this._serverConfig.ConnectionLimit = connectionLimit;
            return this;
        }

        /// <summary>
        /// 传入服务发现注册客户端配置
        /// </summary>
        /// <param name="enableEureka"></param>
        /// <param name="discoveryClientConfig">服务发现注册客户端配置</param>
        /// <returns></returns>
        public ThriftyBootstrap EurekaConfig(bool enableEureka, EurekaClientConfig discoveryClientConfig)
        {
            this.CheckCanAddFutures();
            this._serverConfig.EurekaEnabled = enableEureka;
            this._serverConfig.Eureka = discoveryClientConfig;
            return this;
        }

        private static bool IsIPAddress(String address)
        {
            return IPAddress.TryParse(address, out IPAddress add);
        }

        /// <summary>
        /// 获取或初始化实例配置，不存在则使用默认方式初始化一个
        /// </summary>
        /// <returns></returns>
        private IEurekaInstanceConfig GetEurekaInstanceConfig()
        {
            if (_instanceConfig == null)
            {
                _instanceConfig = new EurekaRegisterInfo();
                _instanceConfig.IpAddress = "127.0.0.1"; //为空获取网卡报错？
                if (String.IsNullOrWhiteSpace(_serverConfig.BindingAddress) || _serverConfig.BindingAddress == "0.0.0.0")
                {
                    ThrowIfAddressWrong();

                    if (IsIPAddress(_instanceDescription.PublicAddress))
                    {
                        _instanceConfig.IpAddress = _instanceDescription.PublicAddress;
                    }
                    else if (!String.IsNullOrWhiteSpace(_instanceDescription.PublicAddress))
                    {
                        _instanceConfig.HostName = _instanceDescription.PublicAddress;
                    }
                }
                else
                {
                    _instanceConfig.IpAddress = _serverConfig.BindingAddress;
                }

                _instanceConfig.SecurePortEnabled = false;
                _instanceConfig.NonSecurePort = _serverConfig.Port;
                _instanceConfig.AppName = _instanceDescription.AppName;
                _instanceConfig.VirtualHostName = _instanceDescription.VipAddress;
            }
            return _instanceConfig;
        }

        private void ThrowIfAddressWrong()
        {
            if (String.IsNullOrWhiteSpace(_instanceDescription.PublicAddress))
            {
                string bindAddress = $"{ nameof(ThriftyServerOptions) }.{ nameof(ThriftyServerOptions.BindingAddress)}";
                string ipAddress = $"{nameof(InstanceDescription)}.{nameof(InstanceDescription.PublicAddress)}";
                throw new ThriftyException($"{nameof(ThriftyBootstrap)} error : if assign null or '0.0.0.0' value to {bindAddress}, need assign an ip address value to {ipAddress} .");
            }
        }


        /// <summary>
        /// 设置空闲连接超时时间
        /// </summary>
        /// <param name="idleConnectionTimeout">空闲连接超时时间</param>
        /// <returns></returns>
        public ThriftyBootstrap IdleConnectionTimeout(TimeSpan idleConnectionTimeout)
        {
            this.CheckCanAddFutures();
            this._serverConfig.IdleConnectionTimeout = idleConnectionTimeout;
            return this;
        }

        /// <summary>
        /// 设置任务执行超时时间
        /// </summary>
        /// <param name="taskExpirationTimeout">任务执行超时时间</param>
        /// <returns></returns>
        public ThriftyBootstrap TaskExpirationTimeout(TimeSpan taskExpirationTimeout)
        {
            this.CheckCanAddFutures();
            this._serverConfig.TaskExpirationTimeout = taskExpirationTimeout;
            return this;
        }

        /// <summary>
        /// 设置队列超时时间
        /// </summary>
        /// <param name="queueTimeout">队列超时时间</param>
        /// <returns></returns>
        public ThriftyBootstrap QueueTimeout(TimeSpan queueTimeout)
        {
            this.CheckCanAddFutures();
            this._serverConfig.QueueTimeout = queueTimeout;
            return this;
        }

        /// <summary>
        /// 设置acceptor线程数目
        /// </summary>
        /// <param name="acceptorThreadCount">acceptor线程数目</param>
        /// <returns></returns>
        public ThriftyBootstrap AcceptorThreadCount(int acceptorThreadCount)
        {
            this.CheckCanAddFutures();
            this._serverConfig.AcceptorThreadCount = acceptorThreadCount;
            return this;
        }


        /// <summary>
        /// 设置ssl
        /// </summary>
        /// <param name="config">SslConfig</param>
        /// <returns></returns>
        public ThriftyBootstrap SslConfig(SslConfig config)
        {
            this.CheckCanAddFutures();
            this._serverConfig.Ssl = config;
            return this;
        }

        /// <summary>
        /// 设置io线程数
        /// </summary>
        /// <param name="ioThreadCount">io线程数</param>
        /// <returns></returns>
        public ThriftyBootstrap IOThreadCount(int ioThreadCount)
        {
            this.CheckCanAddFutures();
            this._serverConfig.IOThreadCount = ioThreadCount;
            return this;
        }

        /// <summary>
        /// 设置工作线程数
        /// </summary>
        /// <param name="workerThreadsCount">工作线程数</param>
        /// <returns></returns>
        public ThriftyBootstrap WorkerThreadCount(int workerThreadsCount)
        {
            this.CheckCanAddFutures();
            this._serverConfig.WorkerThreadCount = workerThreadsCount;
            return this;
        }

        ///// <summary>
        ///// 设置队列最大请求数
        ///// </summary>
        ///// <param name="maxQueuedRequests">队列最大请求数</param>
        ///// <returns></returns>
        //public ServerBootStrap MaxQueuedRequests(int maxQueuedRequests)
        //{
        //    _serverConfig.MaxQueuedRequests =  maxQueuedRequests;
        //    return this;
        //}

        /// <summary>
        /// 设置单连接最大响应数
        /// </summary>
        /// <param name="maxQueuedResponsesPerConnection">单连接最大响应数</param>
        /// <returns></returns>
        public ThriftyBootstrap MaxQueuedResponsesPerConnection(int maxQueuedResponsesPerConnection)
        {
            this.CheckCanAddFutures();
            this._serverConfig.MaxQueuedResponsesPerConnection = maxQueuedResponsesPerConnection;
            return this;
        }

        public Task<ThriftyServer> ShutdownAsync()
        {
            return this.ThriftyServer.ShutdownAsync().ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    ThriftyRegistryService.ThriftyRegistryServiceInstance.SetAppDown();
                }
                return this.ThriftyServer;
            });
        }

        private bool IsExcludeType(Type type)
        {
            return typeof(IHealthCheck).GetTypeInfo().IsAssignableFrom(type);
        }

        public Task<ThriftyServer> StartAsync()
        {
            ThriftyServer OnStarted(Task<ThriftyServer> serverTask)
            {
                if (serverTask.Status == TaskStatus.RanToCompletion && _serverConfig.EurekaEnabled)
                {
                    if (String.IsNullOrWhiteSpace(_serverConfig.Eureka.EurekaServerServiceUrls))
                    {
                        throw new ThriftyException(@"The ""EurekaEnabled"" is configured, but the ""Eureka.EurekaServerServiceUrls"" is empty");
                    }
                    try
                    {
                        var instanceConfig = this.GetEurekaInstanceConfig();
                        var services = _services.Where(x => !IsExcludeType(x.ServiceType)).Select(s => s.Metadata);
                        instanceConfig.MetadataMap.Add("services", JsonConvert.SerializeObject(services));
                        instanceConfig.InstanceId = $"{Environment.MachineName}:{serverTask.Result.Port}";

                        var instance = this.GetEurekaInstanceConfig();
                        instance.NonSecurePort = serverTask.Result.Port;

                        ThriftyRegistryService.ThriftyRegistryServiceInstance.SetAppUp(
                            instance,
                            _serverConfig.Eureka, this._loggerFactory);

                        this._logger.LogDebug("start registering service to eureka success.");
                    }
                    catch (Exception e)
                    {
                        this.ThriftyServer.ShutdownAsync();
                        throw new ThriftyException($"register service to eureka fault.", e);
                    }
                }
                return serverTask.Result;
            }

            return this.ThriftyServer.StartAsync().ContinueWith(OnStarted);
        }

        /// <summary>
        /// 判断是否可以继续添加特性
        /// </summary>
        private void CheckCanAddFutures()
        {
            if (this._server != null && this.ThriftyServer.Status != ThriftyServerStatus.Init)
                throw new ThriftyException("cant add futures after swifty server has started");
        }
    }
}