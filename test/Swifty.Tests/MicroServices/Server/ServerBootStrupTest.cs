using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steeltoe.Discovery.Eureka;

namespace Thrifty.Tests.MicroServices.Server
{
    using Moq;
    using Thrifty.Codecs;
    using Thrifty.MicroServices.Commons;
    using Thrifty.MicroServices.Server;
    using Thrifty.Services;
    using Thrifty.Tests.MicroServices.Stup;
    using Xunit;

    [Collection("ServerBootStrupTest")]
    public class ServerBootStrupTest
    {
        ThriftyBootstrap _serverBootstrup;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public ServerBootStrupTest()
        {
            IServiceLocator serviceLocator = this.GetServiceLocatorMock();
            this._serverBootstrup = new ThriftyBootstrap(serviceLocator, new ThriftyServerOptions(), new InstanceDescription("test", "TestApp", "11.11.11.11"));
        }

        private IServiceLocator GetServiceLocatorMock()
        {
            var mock = new Mock<IServiceLocator>();
            return mock.Object;
        }

        [Fact]
        public void SettingType_Success()
        {
            this._serverBootstrup.AddService<IServiceStup>();
        }

        [Fact]
        public void SettingType_Error()
        {
            Assert.Throws<ThriftyException>(() => this._serverBootstrup.AddServices(this.GetType()));
        }

        [Fact]
        public void SettingType_NoAnnotation_ThrowException()
        {
            Assert.Throws<ThriftyException>(() => this._serverBootstrup.AddService<INoServiceInterface>());
        }


        [Fact]
        public void SettingCodecs_Success()
        {
            Assert.Same(this._serverBootstrup.Codecs(Mock.Of<IThriftCodec>()), this._serverBootstrup);
        }

        [Fact]
        public void Handles_Success()
        {
            Assert.Same(this._serverBootstrup.Handles((lst) => { }), this._serverBootstrup);
        }

        [Fact]
        public void ServiceType_Success()
        {
            Assert.Same(this._serverBootstrup.AddServices(typeof(IServiceStup)), this._serverBootstrup);
        }

        [Fact]
        public void Bind_Success()
        {
            this._serverBootstrup.Bind("11.11.11.11", 1024);
        }

        [Fact]
        public void BackLog_Success()
        {
            this._serverBootstrup.AcceptBacklog(1);
        }

        [Fact]
        public void ConnectionLimit_Success()
        {
            this._serverBootstrup.ConnectionLimit(10);
        }

        [Fact]
        public void DiscoveryClientConfig()
        {
            this._serverBootstrup.EurekaConfig(true, new EurekaClientConfig());
        }


        [Fact]
        public void IdleConnectionTimeout_Success()
        {
            this._serverBootstrup.IdleConnectionTimeout(TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public void TaskExpirationTimeout_Success()
        {
            this._serverBootstrup.TaskExpirationTimeout(TimeSpan.FromDays(1));
        }

        [Fact]
        public void QueueTimeout_Success()
        {
            this._serverBootstrup.QueueTimeout(TimeSpan.MaxValue);
        }

        [Fact]
        public void AcceptorThreadCount_Success()
        {
            this._serverBootstrup.AcceptorThreadCount(1024);
        }

        [Fact]
        public void IoThreadCount_Success()
        {
            this._serverBootstrup.IOThreadCount(100);
        }

        [Fact]
        public void WorkderThreadCount()
        {
            this._serverBootstrup.WorkerThreadCount(1024);
        }

        [Fact]
        public void MaxQueuedResponsesPerConnection()
        {
            this._serverBootstrup.MaxQueuedResponsesPerConnection(1024);
        }


    }
}
