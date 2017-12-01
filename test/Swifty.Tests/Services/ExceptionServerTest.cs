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
    [Collection("ExceptionServerTest")]
    public class ExceptionServerTest
    {
        [Fact(DisplayName ="Server Exception: 普通异常")]
        public void TestServerException()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new ExceptionService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IExceptionService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        ThriftyException ex = Assert.Throws<ThriftyApplicationException>(() => client.ThrowArgumentException());
                        Assert.Contains(ExceptionService.ExceptionMessage, ex.Message);
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }
    }
}
