using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrifty.Codecs;
using Thrifty.Nifty.Processors;
using Thrifty.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.Nifty.Ssl;

namespace Thrifty.MicroServices.Server
{
    /// <summary>
    ///     Thrifty服务器
    /// </summary>
    public class ThriftyServer
    {
        /// <summary>
        ///     logFactory
        /// </summary>
        ILoggerFactory _loggerFactory;

        /// <summary>
        ///     服务定位器
        /// </summary>
        readonly IServiceLocator _serviceLocator;

        /// <summary>
        ///     Service类型集合
        /// </summary>
        readonly IEnumerable<Type> _serviceTypes;

        /// <summary>
        ///     Codecs
        /// </summary>
        readonly IEnumerable<IThriftCodec> _codescs;

        private readonly SslConfig _sslConfig;

        /// <summary>
        ///     服务器配置
        /// </summary>
        readonly ThriftServerConfig _config;

        /// <summary>
        ///     Thrift事件处理器
        /// </summary>
        readonly List<ThriftEventHandler> _handlers;

        /// <summary>
        ///     Thrift服务器
        /// </summary>
        ThriftServer _server;

        /// <summary>
        ///     ThriftyServer服务器状态
        /// </summary>
        volatile ThriftyServerStatus _state = ThriftyServerStatus.Init;

        readonly ILogger _logger;

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="serviceLocator">服务定位器</param>
        /// <param name="config">服务器配置</param>
        /// <param name="handlers">handlers</param>
        /// <param name="serviceTypes">实例对象类型</param>
        /// <param name="codescs">自定义codec</param>
        /// <param name="sslConfig"></param>
        /// <param name="loggerFactory">logger</param>
        internal ThriftyServer(
            IServiceLocator serviceLocator,
            ThriftServerConfig config,
            List<ThriftEventHandler> handlers,
            IEnumerable<Type> serviceTypes,
            IEnumerable<IThriftCodec> codescs,
            SslConfig sslConfig = null,
            ILoggerFactory loggerFactory = null)
        {
            this._config = config;
            this._handlers = handlers;
            this._serviceTypes = serviceTypes;
            this._codescs = codescs;
            this._sslConfig = sslConfig;
            this._loggerFactory = loggerFactory;
            this._serviceLocator = serviceLocator;
            this._logger = loggerFactory?.CreateLogger<ThriftyServer>() ?? (ILogger)NullLogger.Instance;
        }

        public ThriftyServerStatus Status => this._state;

        public int Port => _server.Port;

        /// <summary>
        ///     服务启动
        /// </summary>
        /// <returns></returns>
        internal Task<ThriftyServer> StartAsync()
        {
            Contract.Assert(this._config != null && this._config.Port != 0, "swifty server port is null");
            Contract.Assert(this._serviceTypes != null && this._serviceTypes.Any(), "No service can be used to start swifty server.");

            Contract.Assert(
                this._state == ThriftyServerStatus.Init,
                "swifty server can not start, the state is out of control");

            this._state = ThriftyServerStatus.Starting;

            Task task;
            try
            {
                ThriftCodecManager thriftCodecManager;

                thriftCodecManager = this._codescs == null ? new ThriftCodecManager() : new ThriftCodecManager(this._codescs);

                INiftyProcessor process = new ThriftServiceProcessor(
                    this._serviceLocator,
                    codecManager: thriftCodecManager,
                    eventHandlers: this._handlers,
                    serviceTypes: this._serviceTypes,
                    loggerFactory: this._loggerFactory);
                this._server = new ThriftServer(process, this._config, _sslConfig, this._loggerFactory);

                String addres = String.IsNullOrWhiteSpace(_config.BindingAddress) ? "0.0.0.0" : _config.BindingAddress;
                this._logger.LogDebug($"server is ready for starting: {addres}:{_config.Port}");
                task = this._server.StartAsync();
                return task.ContinueWith(t =>
                {
                    if (t.Exception == null)
                    {
                        this._state = ThriftyServerStatus.Running;
                    }
                    this._state = (t.Exception == null) ? ThriftyServerStatus.Running : ThriftyServerStatus.Error;
                    return this;
                });
            }
            catch (Exception e)
            {
                this._state = ThriftyServerStatus.Error;
                throw new ThriftyException(e.Message);
            }
        }

        /// <summary>
        /// 服务关闭
        /// </summary>
        internal Task<ThriftyServer> ShutdownAsync()
        {
            this._logger.LogDebug("server is ready for stopping");
            if (this._state != ThriftyServerStatus.Running || this._server == null)
                return Task.FromResult(this);

            this._state = ThriftyServerStatus.Shutingdown;

            return this._server.CloseAsync()
                .ContinueWith(
                    x =>
                    {
                        ThriftyRegistryService.ThriftyRegistryServiceInstance.Shutdown();
                        this._state = (x.Exception != null) ? ThriftyServerStatus.Shutingdown : ThriftyServerStatus.Error;
                        return this;
                    });
        }
    }

    /// <summary>
    ///     Thrifty服务器状态
    /// </summary>
    public enum ThriftyServerStatus
    {
        /// <summary>
        ///     初始化中
        /// </summary>
        Init = 0,

        /// <summary>
        ///     启动中
        /// </summary>
        Starting = 1,

        /// <summary>
        ///     运行中
        /// </summary>
        Running = 2,

        /// <summary>
        ///     关闭中
        /// </summary>
        Shutingdown = 3,

        /// <summary>
        ///     发生错误
        /// </summary>
        Error = 5,

        /// <summary>
        ///     关闭中
        /// </summary>
        Shutdown = 4
    }
}