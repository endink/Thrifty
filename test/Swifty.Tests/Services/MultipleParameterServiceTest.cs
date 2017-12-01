using Thrifty.Services;
using Thrifty.Tests.TestModel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Xunit;
using static Thrifty.Tests.TestModel.Services.MultipleParameterService;

namespace Thrifty.Tests.Services
{
    [Collection("MultipleParameterService")]
    public class MultipleParameterServiceTest
    {
        [Theory(DisplayName = "MultipleParameterService:基本数据类型调用参数测试")]
        [InlineData("OKOKOK", 1, 1.1f, 1.2d, true, 3, TestEnum.EnumValue1, 9999, 88, 2, 2.1f, 2.2d, false, (byte)4, TestEnum.EnumValue2, 8888L, (short)99)]
        [InlineData(null, 1, 1.1f, 1.2d, true, 3, TestEnum.EnumValue1, 9999, 88, null, null, null, null, null, TestEnum.EnumValue1, null, null)]
        public void ParametersTest(String args1,
            int args2,
            float args3,
            double args4,
            bool args5,
            byte args6,
            TestEnum args7,
            long args8,
            short args9,
            int? nargs2,
            float? nargs3,
            double? nargs4,
            bool? nargs5,
            byte? nargs6,
            TestEnum? nargs7,
            long? nargs8,
            short? nargs9)
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new MultipleParameterService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IMultipleParameterService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        Random random = new Random();
                        DateTime date = RandomDateTime(random);
                        DateTime? date2 = RandomDateTime(random);

                        String bytesString = Guid.NewGuid().ToString();
                        byte[] bytes = Encoding.UTF8.GetBytes(bytesString);

                        string expected = $@"{args1}{args2}{args3}{args4}{args5}{args6}{args7}{args8}{args9}"
               + $@"{nargs2}{nargs3}{nargs4}{nargs5}{nargs6}{nargs7}{nargs8}{nargs9}{date}{date2}{bytesString}";

                        string result = client.MergeString(
                            args1,
                            args2,
                            args3,
                            args4,
                            args5,
                            args6,
                            args7,
                            args8,
                            args9,
                            nargs2,
                            nargs3,
                            nargs4,
                            nargs5,
                            nargs6,
                            nargs7,
                            nargs8,
                            nargs9,
                            date,
                            date2,
                            bytes);

                        Assert.Equal(expected, result);
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }

        private static DateTime RandomDateTime(Random random)
        {
            return new DateTime(1992, random.Next(2, 11),
                                        random.Next(2, 20),
                                        random.Next(2, 20),
                                        random.Next(1, 59),
                                        random.Next(1, 59),
                                        random.Next(1, 999),
                                        DateTimeKind.Utc);
        }

        [Theory(DisplayName = "MultipleParameterService:字节数组类型测试")]
        [InlineData("OKOKOK")]
        [InlineData("NoNoNo")]
        public void ByteTest(String content)
        {
            using (ScopedServer server = new ScopedServer(new TBinaryProtocol.Factory(), new MultipleParameterService()))
            {
                using (ThriftClientManager manager = new ThriftClientManager())
                {
                    using (var client = server.CreateScribeClient<IMultipleParameterService>(manager, server, new TBinaryProtocol.Factory()))
                    {
                        var test = Encoding.UTF8.GetBytes(content);
                        var bytesString = client.BytesToString(test);

                        Assert.Equal(content, bytesString);
                    }

                    manager.CloseAsync(TimeSpan.FromSeconds(3));
                }
            }
        }
    }
}
