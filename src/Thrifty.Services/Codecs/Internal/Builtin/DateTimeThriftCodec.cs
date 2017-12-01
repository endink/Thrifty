using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class DateTimeThriftCodec : AbstractThriftCodec<DateTime>
    {
        private static readonly DateTime Jan1st1970 = new DateTime
           (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override ThriftType Type { get { return ThriftType.DateTime; } }

        protected override DateTime OnRead(TProtocolReader reader)
        {
            long value = reader.ReadI64();
            return FromLongValue(value);
        }


        protected override void OnWrite(DateTime value, TProtocolWriter writer)
        {
            //if (value.Kind != DateTimeKind.Utc)
            //{
            //    throw new ArgumentException("datetime thrift field invalid, only the utc datetime type are supported.");
            //}
            long longValue = ToLongValue(value);
            writer.WriteI64(longValue);
        }

        internal static long ToLongValue(DateTime value)
        {
            var r = (value.ToUniversalTime().Ticks - Jan1st1970.Ticks) / 10000;
            return r;
        }

        internal static DateTime FromLongValue(long value)
        {
            var r = new DateTime((value * 10000 + Jan1st1970.Ticks), DateTimeKind.Utc);
            return r;
        }
    }
}
