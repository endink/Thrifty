using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal
{
    public class TProtocolWriter : IDisposable
    {
        public TProtocolWriter(TProtocol protocol)
        {
            this.Protocol = protocol;
        }

        public TProtocol Protocol { get; private set; }

        public void WriteStructBegin(String name)
        {
            Protocol.WriteStructBegin(new TStruct(name));
        }

        public void WriteStructEnd()
        {
            Protocol.WriteFieldStop();
            Protocol.WriteStructEnd();
        }

        public void WriteField(String name, short id, IThriftCodec codec, Object value)
        {
            if (value == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, codec.Type.ProtocolType.ToTType(), id));
            codec.WriteObject(value, Protocol);
            Protocol.WriteFieldEnd();
        }

        public void WriteBinaryField(String name, short id, byte[] buf)
        {
            if (buf == null)
            {
                return;
            }
            Protocol.WriteFieldBegin(new TField(name, TType.String, id));
            Protocol.WriteBinary(buf);
            Protocol.WriteFieldEnd();
        }

        public void WriteBoolField(String name, short id, bool b)
        {
            Protocol.WriteFieldBegin(new TField(name, TType.Bool, id));
            Protocol.WriteBool(b);
            Protocol.WriteFieldEnd();
        }

        public void WriteByteField(String name, short id, byte b)
        {
            Protocol.WriteFieldBegin(new TField(name, TType.Byte, id));
            Protocol.WriteByte((sbyte)b);
            Protocol.WriteFieldEnd();
        }

        public void WriteDoubleField(String name, short id, double dub)
        {
            Protocol.WriteFieldBegin(new TField(name, TType.Double, id));
            Protocol.WriteDouble(dub);
            Protocol.WriteFieldEnd();
        }

        public void WriteI16Field(String name, short id, short i16)
        {
            Protocol.WriteFieldBegin(new TField(name, TType.I16, id));
            Protocol.WriteI16(i16);
            Protocol.WriteFieldEnd();
        }

        public void WriteI32Field(String name, short id, int i32)
        {
            Protocol.WriteFieldBegin(new TField(name, TType.I32, id));
            Protocol.WriteI32(i32);
            Protocol.WriteFieldEnd();
        }

        public void WriteI64Field(String name, short id, long i64)
        {
            Protocol.WriteFieldBegin(new TField(name, TType.I64, id));
            Protocol.WriteI64(i64);
            Protocol.WriteFieldEnd();
        }

        public void WriteStringField(String name, short id, String str)
        {
            if (str == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.String, id));
            Protocol.WriteString(str);
            Protocol.WriteFieldEnd();
        }

        public void WriteStructField<T>(String name, short id, IThriftCodec<T> codec, T fieldValue)
        {
            if (fieldValue == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.Struct, id));
            codec.Write(fieldValue, Protocol);
            Protocol.WriteFieldEnd();
        }

        public void WriteBoolArrayField(String name, short id, bool[] array)
        {
            if (array == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.List, id));
            WriteBoolArray(array);
            Protocol.WriteFieldEnd();
        }

        public void WriteI16ArrayField(String name, short id, short[] array)
        {
            if (array == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.List, id));
            WriteI16Array(array);
            Protocol.WriteFieldEnd();
        }

        public void WriteI32ArrayField(String name, short id, int[] array)
        {
            if (array == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.List, id));
            WriteI32Array(array);
            Protocol.WriteFieldEnd();
        }

        public void WriteI64ArrayField(String name, short id, long[] array)
        {
            if (array == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.List, id));
            WriteI64Array(array);
            Protocol.WriteFieldEnd();
        }

        public void WriteDoubleArrayField(String name, short id, double[] array)
        {
            if (array == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.List, id));
            WriteDoubleArray(array);
            Protocol.WriteFieldEnd();
        }

        public void WriteSetField<E>(String name, short id, IThriftCodec<ISet<E>> codec, ISet<E> set)
        {
            if (set == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.Set, id));
            codec.Write(set, Protocol);
            Protocol.WriteFieldEnd();
        }

        public void WriteListField<E>(String name, short id, IThriftCodec<IList<E>> codec, IList<E> list)
        {
            if (list == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.List, id));
            codec.Write(list, Protocol);
            Protocol.WriteFieldEnd();
        }

        public void WriteMapField<K, V>(String name, short id, IThriftCodec<IDictionary<K, V>> codec, IDictionary<K, V> map)
        {
            if (map == null)
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.Map, id));
            codec.Write(map, Protocol);
            Protocol.WriteFieldEnd();
        }

        public void WriteEnumField<T>(String name, short id, IThriftCodec<T> codec, T enumValue)
                where T : struct
        {

            if (enumValue.Equals(default(T)))
            {
                return;
            }

            Protocol.WriteFieldBegin(new TField(name, TType.I32, id));
            codec.Write(enumValue, Protocol);
            Protocol.WriteFieldEnd();
        }

        public void WriteBinary(byte[] buf)
        {
            if (buf == null)
            {
                return;
            }
            Protocol.WriteBinary(buf);
        }

        public void WriteBool(bool b)
        {
            Protocol.WriteBool(b);
        }

        public void WriteByte(byte b)
        {
            Protocol.WriteByte((sbyte)b);
        }

        public void WriteI16(short i16)
        {
            Protocol.WriteI16(i16);
        }

        public void WriteI32(int i32)
        {
            Protocol.WriteI32(i32);
        }

        public void WriteI64(long i64)
        {
            Protocol.WriteI64(i64);
        }

        public void WriteDouble(double dub)
        {
            Protocol.WriteDouble(dub);
        }

        public void WriteString(String str)
        {
            if (str == null)
            {
                return;
            }
            Protocol.WriteString(str);
        }

        public void WriteBoolArray(bool[] array)
        {
            Protocol.WriteListBegin(new TList(TType.Bool, array.Length));
            foreach (var booleanValue in array)
            {
                WriteBool(booleanValue);
            }
            Protocol.WriteListEnd();
        }

        public void WriteI16Array(short[] array)
        {
            Protocol.WriteListBegin(new TList(TType.I16, array.Length));
            foreach (int i16 in array)
            {
                WriteI32(i16);
            }
            Protocol.WriteListEnd();
        }

        public void WriteI32Array(int[] array)
        {
            Protocol.WriteListBegin(new TList(TType.I32, array.Length));
            foreach (int i32 in array)
            {
                WriteI32(i32);
            }
            Protocol.WriteListEnd();
        }

        public void WriteI64Array(long[] array)
        {
            Protocol.WriteListBegin(new TList(TType.I64, array.Length));
            foreach (long i64 in array)
            {
                WriteI64(i64);
            }
            Protocol.WriteListEnd();
        }

        public void WriteDoubleArray(double[] array)
        {
            Protocol.WriteListBegin(new TList(TType.Double, array.Length));
            foreach (double doubleValue in array)
            {
                WriteDouble(doubleValue);
            }
            Protocol.WriteListEnd();
        }

        public void WriteSet<T>(IThriftCodec<T> elementCodec, ISet<T> set)
        {
            if (set == null)
            {
                return;
            }

            Protocol.WriteSetBegin(new TSet(elementCodec.Type.ProtocolType.ToTType(), set.Count));

            foreach (T element in set)
            {
                elementCodec.Write(element, Protocol);
            }

            Protocol.WriteSetEnd();
        }

        public void WriteList<T>(IThriftCodec<T> elementCodec, IEnumerable<T> list)
        {
            if (list == null)
            {
                return;
            }

            Protocol.WriteListBegin(new TList(elementCodec.Type.ProtocolType.ToTType(), list.Count()));

            foreach (T element in list)
            {
                elementCodec.Write(element, Protocol);
            }

            Protocol.WriteListEnd();
        }

        public void WriteMap<K, V>(IThriftCodec<K> keyCodec, IThriftCodec<V> valueCodec, IDictionary<K, V> map)
        {

            if (map == null)
            {
                return;
            }

            Protocol.WriteMapBegin(new TMap(keyCodec.Type.ProtocolType.ToTType(), valueCodec.Type.ProtocolType.ToTType(), map.Count));

            foreach (var entry in map)
            {
                keyCodec.Write(entry.Key, Protocol);
                valueCodec.Write(entry.Value, Protocol);
            }

            Protocol.WriteMapEnd();
        }

        public void Dispose()
        {
            this.Protocol?.Dispose();
            this.Protocol = null;
        }
    }
}
