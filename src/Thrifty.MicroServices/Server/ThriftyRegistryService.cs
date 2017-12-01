using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.AppInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.MicroServices.Commons;

namespace Thrifty.MicroServices.Server
{
    public class ThriftyRegistryService
    {
        static ThriftyRegistryService()
        {
            ThriftyRegistryServiceInstance = new ThriftyRegistryService();
        }

        ///// <summary>
        ///// 应用信息管理器
        ///// </summary>
        ApplicationInfoManager _applicationInfoManager;

        /// <summary>
        ///     Eureka客户端
        /// </summary>
        IEurekaClient _eurekaClient;

        ILoggerFactory _loggerFactory;

        ILogger _logger;
        ///// <summary>
        ///// Eureka客户端
        ///// </summary>
        //private IEurekaClient eurekaClient;

        ThriftyRegistryService()
        {
            this._logger = (ILogger)NullLogger.Instance;
        }

        /// <summary>
        ///     获取单例
        /// </summary>
        public static ThriftyRegistryService ThriftyRegistryServiceInstance { get; }

        public void SetAppUp(IEurekaInstanceConfig instanceConfig, EurekaClientConfig config,ILoggerFactory factory)
        {
            this._logger.LogDebug("Start registering service instance...");
            ApplicationInfoManager aim = this.InitializeApplicationInfoManager(instanceConfig);
            this.SetAppStatus(InstanceStatus.UP, aim);
            this._logger.LogDebug($"Service instance was registered (ip: {instanceConfig.IpAddress}, port: {instanceConfig.NonSecurePort}, sec-port: {instanceConfig.SecurePort})");

            this._eurekaClient = this.InitializeEurekaClient(instanceConfig, config, factory);

            for (int i = 0; i < 120; i++)
            {
                try
                {
                    IList<InstanceInfo> infoList = this._eurekaClient.GetInstanceById(instanceConfig.InstanceId);
                    if (infoList != null && infoList.Count > 0)
                    {
                        this._logger.LogDebug("Service instance was online.");
                        return;
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }

            throw new ThriftyException($"Unable to connect to the eureka server (eureka server: {config.EurekaServerServiceUrls}), please check the swifty server configuration");
        }

        /// <summary>
        ///     关闭服务发现注册客户端
        /// </summary>
        public void Shutdown()
        {
            this._eurekaClient?.ShutdownAsyc();
        }

        /// <summary>
        ///     将应用状态设置为down
        /// </summary>
        /// <returns>是否成功</returns>
        public bool SetAppDown()
        {
            if (this._applicationInfoManager == null)
            {
                return false;
            }
            else
            {
                this._logger.LogDebug("开始使用进行服务注销");
                this.SetAppStatus(InstanceStatus.DOWN, this._applicationInfoManager);
                this._logger.LogDebug("服务注销成功");
            }

            return true;
        }

        /// <summary>
        ///     将应用状态设置为out of service(应用下线)
        /// </summary>
        /// <returns>是否成功操作</returns>
        public bool SetAppOutOfService()
        {
            if (this._applicationInfoManager == null)
            {
                return false;
            }
            else
            {
                this.SetAppStatus(InstanceStatus.OUT_OF_SERVICE, this._applicationInfoManager);
            }

            return true;
        }

        /// <summary>
        ///     将应用设置为下线状态
        /// </summary>
        /// <param name="instanceConfig">instanceConfig 实例配置</param>
        public void SetAppOutOfService(EurekaInstanceConfig instanceConfig)
        {
            this.SetAppStatus(InstanceStatus.OUT_OF_SERVICE, this.InitializeApplicationInfoManager(instanceConfig));
        }

        /// <summary>
        ///     将应用设置为down的状态
        /// </summary>
        /// <param name="instanceConfig">instanceConfig 实例配置</param>
        public void SsetAppDown(EurekaInstanceConfig instanceConfig)
        {
            this.SetAppStatus(InstanceStatus.DOWN, this.InitializeApplicationInfoManager(instanceConfig));
        }

        /// <summary>
        ///     修改实例状态
        /// </summary>
        /// <param name="stauts">修改的状态</param>
        /// <param name="applicationInfoManager">应用管理器</param>
        void SetAppStatus(InstanceStatus stauts, ApplicationInfoManager applicationInfoManager)
        {
            applicationInfoManager.InstanceStatus = stauts;
        }

        /// <summary>
        ///     初始化Eureka客户端
        /// </summary>
        /// <param name="instanceConfig">实例配置</param>
        /// <param name="config">服务发现注册客户端配置</param>
        /// <param name="loggerFactory">loggerfactory</param>
        /// <returns>服务发现注册eureka客户端</returns>
        IEurekaClient InitializeEurekaClient(IEurekaInstanceConfig instanceConfig, EurekaClientConfig config, ILoggerFactory loggerFactory = null)
        {
            this._loggerFactory = loggerFactory;
            if (this._eurekaClient == null)
            {
                lock (this)
                {
                    if (DiscoveryManager.Instance.Client == null)
                    {
                        DiscoveryManager.Instance.Initialize(config, instanceConfig, loggerFactory);
                    }
                    this._eurekaClient = DiscoveryManager.Instance.Client;
                }
            }
            this._logger = loggerFactory?.CreateLogger<ThriftyRegistryService>() ?? (ILogger)NullLogger.Instance;
            return this._eurekaClient;
        }

        /// <summary>
        ///     初始化应用信息管理器
        /// </summary>
        /// <param name="instanceConfig">实例配置</param>
        /// <returns>应用信息管理器</returns>
        ApplicationInfoManager InitializeApplicationInfoManager(IEurekaInstanceConfig instanceConfig)
        {
            if (this._applicationInfoManager == null)
            {
                lock (this)
                {
                    if (ApplicationInfoManager.Instance.InstanceInfo == null)
                    {
                        ApplicationInfoManager.Instance.Initialize(instanceConfig);
                    }

                    this._applicationInfoManager = ApplicationInfoManager.Instance;
                }
            }
            return this._applicationInfoManager;
        }
    }
}