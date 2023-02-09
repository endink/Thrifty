using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Thrifty.Services;
using Thrifty.Tests.TestModel.Codecs;

using Xunit;

namespace Thrifty.Tests.Services.Codecs.Internal
{
    public class EnumThriftCodecTest
    {
        [Fact(DisplayName = "EnumThriftCodec enum枚举类型序列化")]
        public void EnumFielReadAndWritedTest()
        {
            var enumStruct = new EnumStruct()
            {
                DefaultEnum = DefaultEnum.Node2,
                ComplexEnum = ComplexEnum.Node1,
                //ErrorEnum = ErrorEnum.Node1,
            };

            ThriftSerializer serializer = new ThriftSerializer();
            var bytes = serializer.Serialize(enumStruct);

            var obj = serializer.Deserialize<EnumStruct>(bytes);

            Assert.NotNull(obj);
            Assert.Equal(DefaultEnum.Node2, obj.DefaultEnum);
            Assert.Equal(ComplexEnum.Node1, obj.ComplexEnum);
        }
    }
}
