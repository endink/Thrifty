using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Internal.Builtin
{
    public class DateTimeArrayThriftCodec : AbstractThriftCodec<DateTime[]>
    {
        public override ThriftType Type { get { return ThriftType.Array(ThriftType.DateTime); } }

        protected override DateTime[] OnRead(TProtocolReader reader)
        {
            var longArray = reader.ReadI64Array();
            if (longArray == null)
            {
                return null;
            }
            else
            {
                DateTime[] result = new DateTime[longArray.Length];
                for (var i = 0; i < longArray.Length; i++)
                {
                    result[i] = DateTimeThriftCodec.FromLongValue(longArray[i]);
                }
                return result;
            }
        }

        protected override void OnWrite(DateTime[] value, TProtocolWriter writer)
        {
            if (value == null)
            {
                writer.WriteI64Array(null);
            }
            else
            {
                long[] result = new long[value.Length];
                for (var i = 0; i < value.Length; i++)
                {
                    result[i] = DateTimeThriftCodec.ToLongValue(value[i]);
                }
                writer.WriteI64Array(result);
            }
        }
    }
}
