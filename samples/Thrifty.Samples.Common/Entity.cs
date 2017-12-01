using System;

namespace Thrifty.Samples.Common
{
    [ThriftStruct("Entity")]
    public class Entity
    {
        [ThriftField(1)]
        public DayOfWeek Day { get; set; }
        [ThriftField(2)]
        public string StringValue { get; set; }
        [ThriftField(3)]
        public bool BoolValue { get; set; }
        [ThriftField(4)]
        public byte ByteNumber { get; set; }
        [ThriftField(5)]
        public short ShortNumber { get; set; }
        [ThriftField(6)]
        public int IntNumber { get; set; }
        [ThriftField(7)]
        public long LongNumber { get; set; }
        [ThriftField(8)]
        public double DoubleNumber { get; set; }
        [ThriftField(9)]
        public DateTime Now { get; set; } = DateTime.Now;
        //不支持guid

        [ThriftField(10)]
        public int[] IntArray { get; set; }

        [ThriftField(11)]
        public Guid? Guid { get; set; }

        [ThriftField(12)]
        public Guid[] GuidArray { get; set; }
    }
}
