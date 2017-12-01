using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal
{
    public class TProtocolReader : IDisposable
    {
        private TField? currentField;

        public TProtocolReader(TProtocol protocol)
        {
            this.Protocol = protocol;
        }

        public TProtocol Protocol { get; private set; }

        public void ReadStructBegin()
        {
            this.Protocol.ReadStructBegin();
            currentField = null;
        }

        public void ReadStructEnd()
        {
            if (currentField == null || currentField.Value.ID != (short)TType.Stop)
            {
                throw new ThriftyException("Some fields have not been consumed");
            }

            currentField = null;
            this.Protocol.ReadStructEnd();
        }

        public bool NextField()
        {
            // if the current field is a stop record, the caller must call readStructEnd.
            if (currentField != null && currentField.Value.ID == (short)TType.Stop)
            {
                throw new ThriftyException("must call readStructEnd");
            }
            if (currentField != null)
            {
                throw new ThriftyException("Current field was not read");
            }


            // advance to the next field
            currentField = Protocol.ReadFieldBegin();

            return currentField.Value.Type != (int)TType.Stop;
        }

        public short GetFieldId()
        {
            if (currentField == null)
            {
                throw new ThriftyException("No current field");
            }
            return currentField.Value.ID;
        }

        public byte GetFieldType()
        {
            if (currentField == null)
            {
                throw new ThriftyException("No current field");
            }
            return (byte)currentField.Value.Type;
        }

        public void SkipFieldData()
        {
            TProtocolUtil.Skip(this.Protocol, currentField.Value.Type);
            Protocol.ReadFieldEnd();
            currentField = null;
        }

        public Object ReadField(IThriftCodec codec)
        {
            if (!CheckReadState(codec.Type.ProtocolType.ToTType()))
            {
                return null;
            }
            currentField = null;
            Object fieldValue = codec.ReadObject(Protocol);
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public byte[] ReadBinaryField()
        {
            if (!CheckReadState(TType.String))
            {
                return null;
            }
            currentField = null;
            byte[] fieldValue = Protocol.ReadBinary();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public bool ReadBoolField()
        {
            if (!CheckReadState(TType.Bool))
            {
                return false;
            }
            currentField = null;
            bool fieldValue = Protocol.ReadBool();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public byte ReadByteField()
        {
            if (!CheckReadState(TType.Byte))
            {
                return 0;
            }
            currentField = null;
            byte fieldValue = (byte)Protocol.ReadByte();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public double ReadDoubleField()
        {
            if (!CheckReadState(TType.Double))
            {
                return 0;
            }
            currentField = null;
            double fieldValue = Protocol.ReadDouble();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public short ReadI16Field()
        {
            if (!CheckReadState(TType.I16))
            {
                return 0;
            }
            currentField = null;
            short fieldValue = Protocol.ReadI16();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public int ReadI32Field()
        {
            if (!CheckReadState(TType.I32))
            {
                return 0;
            }
            currentField = null;
            int fieldValue = Protocol.ReadI32();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public long ReadI64Field()
        {
            if (!CheckReadState(TType.I64))
            {
                return 0;
            }
            currentField = null;
            long fieldValue = Protocol.ReadI64();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public String ReadStringField()
        {
            if (!CheckReadState(TType.String))
            {
                return null;
            }
            currentField = null;
            String fieldValue = Protocol.ReadString();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public T ReadStructField<T>(IThriftCodec<T> codec)
        {
            if (!CheckReadState(TType.Struct))
            {
                return default(T);
            }
            currentField = null;
            T fieldValue = codec.Read(this.Protocol);
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public bool[] ReadBoolArrayField()
        {
            if (!CheckReadState(TType.List))
            {
                return null;
            }
            currentField = null;
            bool[]
            fieldValue = ReadBoolArray();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public short[] ReadI16ArrayField()
        {
            if (!CheckReadState(TType.List))
            {
                return null;
            }
            currentField = null;
            short[]
            fieldValue = ReadI16Array();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public int[] ReadI32ArrayField()
        {
            if (!CheckReadState(TType.List))
            {
                return null;
            }
            currentField = null;
            int[]
            fieldValue = ReadI32Array();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public long[] ReadI64ArrayField()
        {
            if (!CheckReadState(TType.List))
            {
                return null;
            }
            currentField = null;
            long[]
            fieldValue = ReadI64Array();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public double[] ReadDoubleArrayField()
        {
            if (!CheckReadState(TType.List))
            {
                return null;
            }
            currentField = null;
            double[]
            fieldValue = ReadDoubleArray();
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public IEnumerable<E> ReadSetField<E>(IThriftCodec<HashSet<E>> setCodec)
        {
            if (!CheckReadState(TType.Set))
            {
                return null;
            }
            currentField = null;
            IEnumerable<E> fieldValue = setCodec.Read(this.Protocol);
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public List<E> ReadListField<E>(IThriftCodec<List<E>> listCodec)
        {
            if (!CheckReadState(TType.List))
            {
                return null;
            }
            currentField = null;
            List<E> read = listCodec.Read(this.Protocol);
            Protocol.ReadFieldEnd();
            return read;
        }

        public IDictionary<K, V> ReadMapField<K, V>(IThriftCodec<Dictionary<K, V>> mapCodec)
        {
            if (!CheckReadState(TType.Map))
            {
                return null;
            }
            currentField = null;
            IDictionary<K, V> fieldValue = mapCodec.Read(this.Protocol);
            this.Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public T ReadEnumField<T>(IThriftCodec<T> enumCodec)
                where T : struct
        {
            if (!CheckReadState(TType.I32))
            {
                return default(T);
            }
            currentField = null;
            T fieldValue = default(T);
            try
            {
                fieldValue = enumCodec.Read(Protocol);
            }
            catch (ArgumentException)
            {
                // return null
            }
            Protocol.ReadFieldEnd();
            return fieldValue;
        }

        public byte[] ReadBinary()
        {
            return Protocol.ReadBinary();
        }

        public bool ReadBool()
        {
            return this.Protocol.ReadBool();
        }

        public byte ReadByte()
        {
            return (byte)Protocol.ReadByte();
        }

        public short ReadI16()
        {
            return Protocol.ReadI16();
        }

        public int ReadI32()
        {
            return Protocol.ReadI32();
        }

        public long ReadI64()
        {
            return Protocol.ReadI64();
        }

        public double ReadDouble()
        {
            return Protocol.ReadDouble();
        }

        public String ReadString()
        {
            return Protocol.ReadString();
        }

        public bool[] ReadBoolArray()
        {
            TList list = Protocol.ReadListBegin();
            bool[]
            array = new bool[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = ReadBool();
            }
            Protocol.ReadListEnd();
            return array;
        }

        public short[] ReadI16Array()
        {
            TList list = Protocol.ReadListBegin();
            short[] array = new short[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = ReadI16();
            }
            Protocol.ReadListEnd();
            return array;
        }

        public int[] ReadI32Array()
        {
            TList list = Protocol.ReadListBegin();
            int[] array = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = ReadI32();
            }
            Protocol.ReadListEnd();
            return array;
        }

        public long[] ReadI64Array()
        {
            TList list = this.Protocol.ReadListBegin();
            long[] array = new long[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = ReadI64();
            }
            Protocol.ReadListEnd();
            return array;
        }

        public double[] ReadDoubleArray()
        {
            TList list = Protocol.ReadListBegin();
            double[] array = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = ReadDouble();
            }
            Protocol.ReadListEnd();
            return array;
        }

        public ISet<E> ReadSet<E>(IThriftCodec<E> elementCodec)
        {
            TSet tSet = Protocol.ReadSetBegin();
            ISet<E> set = new HashSet<E>();
            for (int i = 0; i < tSet.Count; i++)
            {
                try
                {
                    E element = elementCodec.Read(Protocol);
                    set.Add(element);
                }
                catch (ArgumentException e)
                {
                    // continue
                    e.ThrowIfNecessary();
                }
            }
            Protocol.ReadSetEnd();
            return set;
        }

        public IList<E> ReadList<E>(IThriftCodec<E> elementCodec)
        {
            TList tList = Protocol.ReadListBegin();
            IList<E> list = new List<E>();
            for (int i = 0; i < tList.Count; i++)
            {
                try
                {
                    E element = elementCodec.Read(Protocol);
                    list.Add(element);
                }
                catch (Exception e)
                {
                    // continue
                    e.ThrowIfNecessary();
                }
            }
            Protocol.ReadListEnd();
            return list;
        }


        public IDictionary<K, V> ReadMap<K, V>(IThriftCodec<K> keyCodec, IThriftCodec<V> valueCodec)
        {

            TMap tMap = Protocol.ReadMapBegin();
            Dictionary<K, V> map = new Dictionary<K, V>();
            for (int i = 0; i < tMap.Count; i++)
            {
                try
                {
                    K key = keyCodec.Read(this.Protocol);
                    V value = valueCodec.Read(this.Protocol);
                    map[key] = value;
                }
                catch (Exception e)
                {
                    // continue
                    e.ThrowIfNecessary();
                }
            }
            Protocol.ReadMapEnd();
            return map;
        }

        private bool CheckReadState(TType expectedType)
        {
            if (currentField == null)
            {
                throw new ThriftyException("No current field");
            }

            if (currentField.Value.Type != expectedType)
            {
                TProtocolUtil.Skip(Protocol, currentField.Value.Type);
                Protocol.ReadFieldEnd();
                currentField = null;
                return false;
            }

            return true;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TProtocolReader");
            sb.Append("{currentField=").Append(currentField);
            sb.Append('}');
            return sb.ToString();
        }

        public void Dispose()
        {
            this.Protocol?.Dispose();
            this.Protocol = null;
        }
    }
}
