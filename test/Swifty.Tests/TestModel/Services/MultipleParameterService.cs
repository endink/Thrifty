using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Thrifty.Tests.TestModel.Services.MultipleParameterService;

namespace Thrifty.Tests.TestModel.Services
{
    [ThriftService]
    public interface IMultipleParameterService : IDisposable
    {
        [ThriftMethod]
        String BytesToString(byte[] bytes);

        [ThriftMethod]
        String MergeString(
            String args1,
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
            short? nargs9,
            DateTime args10,
            DateTime? args11,
            byte[] buffer
            );
    }

    public class MultipleParameterService : IMultipleParameterService
    {
        public void Dispose()
        { }

        public String BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public String MergeString(
            String args1,
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
            short? nargs9,
            DateTime args10,
            DateTime? args11,
            byte[] buffer
            )
        {
            String bytesString = buffer == null ? String.Empty : Encoding.UTF8.GetString(buffer);
            return $@"{args1}{args2}{args3}{args4}{args5}{args6}{args7}{args8}{args9}"
                + $@"{nargs2}{nargs3}{nargs4}{nargs5}{nargs6}{nargs7}{nargs8}{nargs9}{args10}{args11}{bytesString}";
        }


        public enum TestEnum
        {
            EnumValue1,
            EnumValue2
        }
    }
}
