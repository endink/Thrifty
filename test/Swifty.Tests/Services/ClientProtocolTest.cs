using Thrifty.Nifty.Client;
using Thrifty.Nifty.Core;
using Thrifty.Nifty.Duplex;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Thrift.Protocol;
using Xunit;

namespace Thrifty.Tests.Services
{
    [Collection("ClientProtocolTest")]
    public class ClientProtocolTest
    {
        [Fact(DisplayName = "ThriftClient: BinaryProtocol 测试")]
        public void TestBinaryProtocolClient()
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new ScribeTest()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IScribe>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        var code = client.log(new List<LogEntry>
                    {
                        new LogEntry("testCategory1", "testMessage1"),
                        new LogEntry("testCategory2", "testMessage2")
                    });
                        Assert.Equal(ResultCode.TRY_LATER, code);
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }

        [Fact(DisplayName = "ThriftClient: CompactProtocol 测试")]
        public void TestCompactProtocolClient()
        {
            using (ScopedServer server = new ScopedServer(new TCompactProtocol.Factory(), new ScribeTest()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IScribe>(manager, server, new TCompactProtocol.Factory()))
                    {
                        var code = client.log(new List<LogEntry>
                    {
                        new LogEntry("testCategory1", "testMessage1"),
                        new LogEntry("testCategory2", "testMessage2")
                    });
                        Assert.Equal(ResultCode.TRY_LATER, code);
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }



    }
}
