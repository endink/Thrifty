using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Thrifty.Tests.Services.Codecs.Metadata
{
    public class ThriftStructMetadataBuilderTest
    {
        [Fact(DisplayName = "ThriftStructMetadataBuilder 简单类型元数据")]
        public void SimpleTypeTest()
        {
            var metadata = StructTest< SimpleStruct>();
            Assert.Equal("testName", metadata.StructName);
        }

        [Fact(DisplayName = "ThriftStructMetadataBuilder 复杂类型元数据")]
        public void ComplexTypeTest()
        {
            ThriftCatalog catalog = new ThriftCatalog();
            ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(catalog, typeof(ComplexStruct));
            ThriftStructMetadata metadata = builder.Build();

            var properties = typeof(ComplexStruct).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly);
            
            Assert.Equal(properties.Length, metadata.Fields.Count());

            Assert.Equal(false, metadata.IsException);
            Assert.Equal(false, metadata.IsUnion);
            Assert.Equal(MetadataType.Struct, metadata.MetadataType);
            Assert.Equal(0, metadata.MethodInjections.Count());
            Assert.Equal(null, metadata.BuilderType);
        }

        [Fact(DisplayName = "ThriftStructMetadataBuilder List递归性元数据")]
        public void ListRecursiveTypeTest()
        {
            ThriftCatalog catalog = new ThriftCatalog();
            ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(catalog, typeof(ListRecursiveStruct));
            ThriftStructMetadata metadata = builder.Build();
            
            Assert.Equal(1, metadata.Fields.Count());
        }

        [Fact(DisplayName = "ThriftStructMetadataBuilder 递归类型元数据")]
        public void RecursiveTypeTest()
        {
            ThriftCatalog catalog = new ThriftCatalog();
            ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(catalog, typeof(RecursiveStruct));
            var metadata = builder.Build();
            Assert.Equal(1, metadata.Fields.Count());
        }

        [Fact(DisplayName = "ThriftStructMetadataBuilder 递归类型元数据（异常测试）")]
        public void InvalidRecursiveTypeTest()
        {
            ThriftCatalog catalog = new ThriftCatalog();
            
            Assert.Throws<ThriftyException>(() =>
            {
                ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(catalog, typeof(InvalidRecursiveStruct));
                builder.Build();
            });
        }

        private ThriftStructMetadata StructTest<T>()
        {
            ThriftCatalog catalog = new ThriftCatalog();
            ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(catalog, typeof(T));
            ThriftStructMetadata metadata = builder.Build();

            var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            Assert.Equal(properties.Length, metadata.Fields.Count());
            
            Assert.Equal(false, metadata.IsException);
            Assert.Equal(false, metadata.IsUnion);
            Assert.Equal(MetadataType.Struct, metadata.MetadataType);
            Assert.Equal(0, metadata.MethodInjections.Count());
            Assert.Equal(null, metadata.BuilderType);
            return metadata;
        }

        [Fact(DisplayName = "ThriftStructMetadataBuilder 继承结构元数据")]
        public void DerivedTypeTest()
        {
            var metadata = StructTest<DerivedStruct>();
            Assert.Equal("DerivedStruct", metadata.StructName);
        }
    }
}
