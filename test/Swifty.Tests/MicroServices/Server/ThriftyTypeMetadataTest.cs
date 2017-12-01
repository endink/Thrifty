using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Services.Metadata;

namespace Thrifty.Tests.MicroServices.Server
{
    using Thrifty.MicroServices.Commons;
    using Thrifty.MicroServices.Server;
    using Thrifty.Tests.MicroServices.Stup;
    using Xunit;

    public class ThriftyTypeMetadataTest
    {
        [Fact]
        public void Initial_Success()
        {
            new ThriftyServiceMetadata(typeof(IServiceStup), "2.0.0");
        }

        [Fact]
        public void Initial_NotInterface_ThrowEx()
        {
            Assert.Throws<ThriftyException>(() => new ThriftyServiceMetadata(this.GetType()));
        }

        [Fact]
        public void GetProperties_Success()
        {
            var swiftMetadata = new ThriftyServiceMetadata(typeof(IServiceStup), "1.1");

            //这个测试有点....
            Assert.Equal(swiftMetadata.ServiceName, ThriftServiceMetadata.ParseServiceName(typeof(IServiceStup)));
            Assert.Equal(swiftMetadata.Version, "1.1");
        }
    }
}
