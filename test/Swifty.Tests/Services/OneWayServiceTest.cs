using Thrifty.Services;
using Thrifty.Tests.TestModel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;
using Xunit;

namespace Thrifty.Tests.Services
{
    [Collection("OneWayServiceTest")]
    public class OneWayServiceTest
    {
        [Fact(DisplayName = "OneWayService: 异常测试")]
        public void TestOneWayException()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new OneWayService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IOneWayService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        //client.OneWayThrow();
                        client.VerifyConnectionState();
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }

        [Fact(DisplayName = "OneWayService: 调用测试1")]
        public void TestOneWay1()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new OneWayService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IOneWayService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        client.VerifyConnectionState();
                        client.OneWayMethod(); 
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }
        [Fact(DisplayName = "OneWayService: 调用测试2")]
        public void TestOneWay2()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new OneWayService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IOneWayService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        client.OneWayMethod();
                        client.VerifyConnectionState(); 
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }
    }
}
