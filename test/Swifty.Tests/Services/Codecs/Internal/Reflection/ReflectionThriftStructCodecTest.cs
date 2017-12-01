using Thrifty.Codecs;
using Thrifty.Codecs.Internal.Reflection;
using Thrifty.Codecs.Metadata;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;
using Xunit;

namespace Thrifty.Tests.Services.Codecs.Internal.Reflection
{
    public class ReflectionThriftStructCodecTest
    {
        [Fact(DisplayName = "ReflectionThriftStructCodec : 简单实体序列化")]
        public void TestSimpleStruct()
        {
            SimpleStruct obj1 = CreateSimpleStructWithData();

            ThriftSerializer ser = new ThriftSerializer();
            byte[] objectBytes = ser.Serialize(obj1);
            SimpleStruct obj2 = ser.Deserialize<SimpleStruct>(objectBytes);

            Assert.False(Object.ReferenceEquals(obj1, obj2));
            Assert.Equal(obj1, obj2);
        }
        [Fact(DisplayName = "ReflectionThriftStructCodec : 简单实体数组序列化")]
        public void TestSimpleStructArray()
        {
            var obj1 = CreateSimpleStructWithData();
            var obj2 = CreateSimpleStructWithData();

            var ser = new ThriftSerializer();
            byte[] objectBytes = ser.Serialize(new[] { obj1, obj2 });
            var objs = ser.Deserialize<SimpleStruct[]>(objectBytes);

            Assert.False(Object.ReferenceEquals(objs[0], obj1));
            Assert.False(Object.ReferenceEquals(objs[1], obj2));
            Assert.Equal(objs[0], obj1);
            Assert.Equal(objs[1], obj2);
        }


        [Fact(DisplayName = "ReflectionThriftStructCodec : 复杂实体序列化")]
        public void TestComplexStruct()
        {
            SimpleStruct obj1 = CreateSimpleStructWithData();

            ComplexStruct complex1 = new ComplexStruct();
            complex1.Simple = obj1;
            complex1.IEnumerableProperty = new float[] { 99.99F, 88.88F };
            complex1.IListProperty = complex1.IEnumerableProperty.ToList();
            complex1.ISetProperty = new HashSet<float> { 55f, 66f };
            complex1.IDictionaryProperty = new Dictionary<float, String>
            {
                { 1f,  "A" },
                { 2f, "B" }
            };
            complex1.DictionaryIntKeyProperty = new Dictionary<int, SimpleStruct>
            {
                { 1, new SimpleStruct { StringProperty = "1Property" } },
                { 2, new SimpleStruct { StringProperty = "2Property" } }
            };

            complex1.EnumArrayProperty = new SimpleEnum[] { SimpleEnum.ValueTwo, SimpleEnum.ValueOne };
            complex1.EnumDictionaryProperty = new Dictionary<string, SimpleEnum>
            {
                { "d1Enum", SimpleEnum.ValueTwo },
                { "d2Enum", SimpleEnum.ValueOne }
            };

            complex1.EnumListProperty = new List<SimpleEnum>() { SimpleEnum.ValueTwo, SimpleEnum.ValueOne };
            complex1.EnumSetProperty = new HashSet<SimpleEnum> { SimpleEnum.ValueOne, SimpleEnum.ValueTwo };

            complex1.IntArrayProperty = new int[] { 1, 2, 3, 4, 5 };
            complex1.SimpleDictionaryProperty1 = new Dictionary<int, float>
            {
                { 1, 3.33f }, { 2, 4.44f }
            };
            complex1.SimpleDictionaryProperty2 = new Dictionary<string, float>
            {
                { "a", 3.33f }, { "b", 4.44f }
            };
            complex1.StructListProperty = new List<SimpleStruct>
            {
                new SimpleStruct { StringProperty = "listItem1" },
                 new SimpleStruct { StringProperty = "listItem2" }
            };
            complex1.StructSetProperty = new HashSet<SimpleStruct>
            {
                new SimpleStruct { StringProperty = "setItem1" },
                 new SimpleStruct { StringProperty = "setItem2" }
            };

            complex1.StructArrayProperty = new SimpleStruct[]
            {
                new SimpleStruct { StringProperty = "setItem1" },
                 new SimpleStruct { StringProperty = "setItem2" }
            };


            ThriftSerializer ser = new ThriftSerializer();
            byte[] objectBytes = ser.Serialize(complex1);
            ComplexStruct complex2 = ser.Deserialize<ComplexStruct>(objectBytes);

            Assert.False(Object.ReferenceEquals(complex1, complex2));
            Assert.Equal(complex1.Simple, complex2.Simple);
            AssertEx.Equals(complex1.DictionaryIntKeyProperty, complex2.DictionaryIntKeyProperty);
            AssertEx.Equals(complex1.EnumArrayProperty, complex2.EnumArrayProperty);
            AssertEx.Equals(complex1.EnumDictionaryProperty, complex2.EnumDictionaryProperty);
            AssertEx.Equals(complex1.EnumListProperty, complex2.EnumListProperty);
            AssertEx.Equals(complex1.EnumSetProperty, complex2.EnumSetProperty);
            AssertEx.Equals(complex1.IntArrayProperty, complex2.IntArrayProperty);
            AssertEx.Equals(complex1.SimpleDictionaryProperty1, complex2.SimpleDictionaryProperty1);
            AssertEx.Equals(complex1.SimpleDictionaryProperty2, complex2.SimpleDictionaryProperty2);
            AssertEx.Equals(complex1.StructListProperty, complex2.StructListProperty);
            AssertEx.Equals(complex1.StructSetProperty, complex2.StructSetProperty);
            AssertEx.Equals(complex1.IEnumerableProperty, complex2.IEnumerableProperty);
            AssertEx.Equals(complex1.IListProperty, complex2.IListProperty);
            AssertEx.Equals(complex1.ISetProperty, complex2.ISetProperty);
            AssertEx.Equals(complex1.IDictionaryProperty, complex2.IDictionaryProperty);
            AssertEx.Equals(complex1.StructArrayProperty, complex2.StructArrayProperty);
        }

        //[Fact(DisplayName = "ReflectionThriftStructCodec: 带构造函数标记实体序列化")]
        [Theory(DisplayName = "ReflectionThriftStructCodec: 带构造函数标记实体序列化")]
        [InlineData(null, 99)]
        [InlineData("a", 99)]
        public void TestSturctWithAttributedConstuctor(String a, int b)
        {
            StructWithConstructor obj1 = new StructWithConstructor(a, b);

            ThriftSerializer ser = new ThriftSerializer();
            byte[] objectBytes = ser.Serialize(obj1);
            StructWithConstructor obj2 = ser.Deserialize<StructWithConstructor>(objectBytes);

            Assert.False(Object.ReferenceEquals(obj1, obj2));
            Assert.Equal(obj1, obj2);
        }



        private static SimpleStruct CreateSimpleStructWithData()
        {
            SimpleStruct obj1 = new SimpleStruct();
            obj1.BoolProperty = false;
            obj1.NullableBoolProperty = true;
            obj1.DoubleProperty = 3.3333;
            obj1.EnumProperty = SimpleEnum.ValueTwo;
            obj1.NullableEnumProperty = SimpleEnum.ValueOne;
            obj1.NullableIntProperty = 9999;
            obj1.IntProperty = 3;
            obj1.NullableByteProperty = 1;
            obj1.ByteProperty = 244;
            obj1.StringProperty = "test";
            obj1.LongProperty = default(long);
            obj1.BufferProperty = new byte[] { 1, 245, 44, 234, 0, 8 };
            obj1.DecimalProperty = 44.3434M;
           // obj1.NullableDecimalProperty = 333.3333M;
            return obj1;
        }

        //private class ThriftSerializer<TStruct>
        //{
        //    private ReflectionThriftStructCodec<TStruct> _codec;
        //    public ThriftSerializer()
        //    {
        //        ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(new ThriftCatalog(), typeof(TStruct));
        //        var metadata = builder.Build();
        //        _codec = new ReflectionThriftStructCodec<TStruct>(new ThriftCodecManager(), metadata);
        //    }

        //    public byte[] Serialize(TStruct s)
        //    {
        //        byte[] objectBytes = null;
        //        using (MemoryStream outs = new MemoryStream())
        //        {
        //            TStreamTransport tt = new TStreamTransport(null, outs);
        //            _codec.WriteObject(s, new TBinaryProtocol(tt));
        //            objectBytes = outs.ToArray();
        //        }
        //        return objectBytes;
        //    }

        //    public TStruct Deserialize(byte[] objectBytes)
        //    {
        //        using (MemoryStream ins = new MemoryStream(objectBytes))
        //        {
        //            TStreamTransport tt = new TStreamTransport(ins, null);
        //            return (TStruct)_codec.ReadObject(new TBinaryProtocol(tt));
        //        }
        //    }
        //}
    }
}
