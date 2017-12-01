using Thrifty.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Thrifty.Tests.Services.Codecs.Metadata
{
    public class ThriftServiceMetadataTest
    {
        // Passes because only a single ancestor class/interface declares @ThriftService (BaseService)
        //
        // Implementation -- @ThriftService BaseService
        //
        [Fact(DisplayName = "ThriftServiceMetadata : 基本服务接口元数据")]
        public void InheritBaseInterface()
        {
            ThriftServiceMetadata.GetThriftServiceAttribute(typeof(BaseServiceImplementation));
        }

        // Passes because even though multiple ancestor class/interfaces declare @ThriftService
        // (BaseService and DerivedServiceOne), BaseService is inherited indirectly through
        // DerivedServiceOne, so DerivedServiceOne's annotation takes precedence.
        //
        // Implementation -- @ThriftService DerivedServiceOne -- @ThriftService BaseService
        //
        [Fact(DisplayName = "ThriftServiceMetadata : 单一继承接口元数据")]
        public void InheritSingleDerivedInterface()
        {
            ThriftServiceMetadata.GetThriftServiceAttribute(typeof(SingleDerivedServiceImplementation));
        }

        // Fails because multiple ancestors declare @ThriftService, and there is a conflict between
        // the @ThriftService annotations on DerviceServiceOne and DerivceServiceTwo which cannot
        // be resolved because neither takes precedence over the other
        //
        //                  / @ThriftService DerivedServiceOne \
        // Implementation --                                      -- @ThriftService BaseService
        //                  \ @ThriftService DerivedServiceTwo /
        //
        [Fact(DisplayName = "ThriftServiceMetadata : 多接口继承元数据")]
        public void InheritMultipleDerivedInterfaces()
        {
            Assert.Throws<ThriftyException>(() =>
            {
                ThriftServiceMetadata.GetThriftServiceAttribute(typeof(MultipleDerivedServiceImplementation));
            });
        }

        // Passes because even though the there would be a conflict, the implementation class explicitly
        // declares it's own @ThriftService, overriding all those from ancestors and resolving the conflict
        //
        //                                 / @ThriftService DerivedServiceOne \
        // @ThriftService Implementation --                                    -- @ThriftService BaseService
        //                                 \ @ThriftService DerivedServiceTwo /
        //
        [Fact(DisplayName = "ThriftServiceMetadata : 服务类注解元数据")]
        public void InheritMultipleDerviedInterfacesWithExplicitAttribute()
        {
            ThriftServiceMetadata.GetThriftServiceAttribute(typeof(MultipleDerivedServiceImplementationWithExplicitAttribute));
        }

        // Passes because even though multiple ancestors declare @ThriftService, they are all inherited
        // through CombinedService, so it's @ThriftService annotation takes precedence.
        //
        //                                                    / @ThriftService DerivedServiceOne \
        // Implementation -- @ThriftService CombinedService --                                    -- @ThriftService BaseService
        //                                                    \ @ThriftService DerivedServiceTwo /
        //
        [Fact(DisplayName = "ThriftServiceMetadata : 组合接口继承元数据")]
        public void InheritCombinedInterface()
        {
            ThriftServiceMetadata.GetThriftServiceAttribute(typeof(CombinedServiceImplementation));
        }
    }
}
