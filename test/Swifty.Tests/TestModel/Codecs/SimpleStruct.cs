using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftStruct("testName")]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class SimpleStruct
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        [ThriftField(11)]
        public bool BoolProperty { get; set; }

        [ThriftField(10)]
        public byte ByteProperty { get; set; }

        [ThriftField(9)]
        public double DoubleProperty { get; set; }

        [ThriftField(8)]
        public int IntProperty { get; set; }

        [ThriftField(7)]
        public long LongProperty { get; set; }

        [ThriftField(6)]
        public short ShortProperty { get; set; }

        [ThriftField(5)]
        public String StringProperty { get; set; }

        [ThriftField(4)]
        public bool? NullableBoolProperty { get; set; }

        [ThriftField(3)]
        public byte? NullableByteProperty { get; set; }

        [ThriftField(2)]
        public double? NullableDoubleProperty { get; set; }

        [ThriftField(1)]
        public int? NullableIntProperty { get; set; }

        [ThriftField(12)]
        public long? NullableLongProperty { get; set; }

        [ThriftField(13)]
        public short? NullableShortProperty { get; set; }

        [ThriftField(14)]
        public float FloatProperty { get; set; }

        [ThriftField(15)]
        public float? NullableFloatProperty { get; set; }

        [ThriftField(16)]
        public SimpleEnum EnumProperty { get; set; }

        [ThriftField(17)]
        public SimpleEnum? NullableEnumProperty { get; set; }

        [ThriftField(18)]
        public byte[] BufferProperty { get; set; }

        [ThriftField(19)]
        public DateTime DateTimeProperty { get; set; }

        [ThriftField(20)]
        public DateTime? NullableDateTimeProperty { get; set; }

        [ThriftField(21)]
        public Guid? NullableGuidProperty { get; set; }

        [ThriftField(22)]
        public Guid GuidProperty { get; set; } = Guid.NewGuid();

        [ThriftField(23)]
        public decimal? NullableDecimalProperty { get; set; }

        [ThriftField(24)]
        public decimal DecimalProperty { get; set; } 

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            bool equals = true;
            StringBuilder error = new StringBuilder();
            foreach (var property in this.GetType().GetTypeInfo().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
            {
                object v1 = property.GetValue(this);
                object v2 = property.GetValue(obj);
                if (property.Name.Equals(nameof(BufferProperty)))
                {
                    byte[] bytes1 = (byte[])v1;
                    byte[] bytes2 = (byte[])v2;
                    if (bytes1 == null && bytes2 == null)
                    {
                        continue;
                    }
                    else if (bytes1 == null || bytes2 == null)
                    {
                        error.AppendLine($"{nameof(BufferProperty) } 不相等。");
                        equals = false;
                        continue ;
                    }
                    else if (bytes1.Length != bytes2.Length)
                    {
                        error.AppendLine($"{nameof(BufferProperty) } 不相等。");
                        equals = false;
                        continue;
                    }
                    else
                    {
                        for (int i = 0; i < bytes1.Length; i++)
                        {
                            if (bytes1[i] != bytes2[i])
                            {
                                error.AppendLine($"{nameof(BufferProperty) } 不相等。");
                                equals = false;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    if (!Object.Equals(v1, v2))
                    {
                        error.AppendLine($"{property.Name} 不相等。");
                        equals = false;
                        continue;
                    }
                }
            }
            Debug.WriteLine(error);
            return equals;
        }

    }

    public enum SimpleEnum
    {
        ValueOne,
        ValueTwo
    }
}
