using Thrifty.Services;
using Thrifty.Tests.Services.Codecs;
using Thrifty.Tests.TestModel.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;
using Xunit;

namespace Thrifty.Tests.Services
{
    [Collection("AsyncServiceTest")]
    public class AsyncServiceTest
    {
        private static SimpleStruct CreateSimpleStructWithData()
        {
            SimpleStruct obj1 = new SimpleStruct();
            obj1.BoolProperty = false;
            obj1.NullableBoolProperty = true;
            obj1.DoubleProperty = 3.3333;
            obj1.EnumProperty = SimpleEnum.ValueTwo;
            obj1.NullableEnumProperty = SimpleEnum.ValueOne;
            obj1.NullableIntProperty = 9999;
            obj1.IntProperty = 3;
            obj1.NullableByteProperty = 1;
            obj1.ByteProperty = 244;
            obj1.StringProperty = "test";
            obj1.LongProperty = long.MaxValue;
            obj1.BufferProperty = new byte[] { 1, 245, 44, 234, 0, 8 };
            obj1.DateTimeProperty = new DateTime(1987, 12, 12, 0, 0, 0, DateTimeKind.Utc);
            return obj1;
        }

        [Fact(DisplayName = "AsyncService: one-way 调用")]
        public void TestOneWay()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new AsyncService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IAsyncService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        client.OneWayMethod().GetAwaiter().GetResult();
                        client.VerifyConnectionState();
                    }
                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }

        [Fact(DisplayName = "AsyncService: two-way 调用")]
        public void TestTwoWay()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new AsyncService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IAsyncService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        SimpleStruct structObj = CreateSimpleStructWithData();
                        var result = client.TwoWayMethod(structObj).GetAwaiter().GetResult();

                        Assert.Equal(structObj, result);
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }


        [Fact(DisplayName = "AsyncService: exception 调用")]
        public void TestException()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new AsyncService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IAsyncService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        SimpleStruct structObj = CreateSimpleStructWithData(); 
                        try
                        {
                            var task = client.ExceptionMethod();
                            task.GetAwaiter().GetResult();
                            Assert.True(false);
                        }
                        catch (ThriftyApplicationException e)
                        {
                            Assert.NotNull(e);
                            Assert.Contains(AsyncService.ExceptionMessage, e.Message);
                        }
                        
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }
    }
}
