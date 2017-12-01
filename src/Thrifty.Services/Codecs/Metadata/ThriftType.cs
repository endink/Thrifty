using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftType
    {
        public static readonly ThriftType Bool = new ThriftType(ThriftProtocolType.Bool, typeof(bool));
        public static readonly ThriftType Byte = new ThriftType(ThriftProtocolType.Byte, typeof(byte));
        public static readonly ThriftType Float = new ThriftType(ThriftProtocolType.Double, typeof(float));
        public static readonly ThriftType Double = new ThriftType(ThriftProtocolType.Double, typeof(double));
        public static readonly ThriftType I16 = new ThriftType(ThriftProtocolType.I16, typeof(short));
        public static readonly ThriftType I32 = new ThriftType(ThriftProtocolType.I32, typeof(int));
        public static readonly ThriftType I64 = new ThriftType(ThriftProtocolType.I64, typeof(long));
        public static readonly ThriftType DateTime = new ThriftType(ThriftProtocolType.I64, typeof(DateTime));
        public static readonly ThriftType Guid = new ThriftType(ThriftProtocolType.String, typeof(Guid));
        public static readonly ThriftType Decimal = new ThriftType(ThriftProtocolType.String, typeof(Decimal));

        //public static readonly ThriftType NullableBool = new ThriftType(ThriftProtocolType.Bool, typeof(bool?));
        //public static readonly ThriftType NullableByte = new ThriftType(ThriftProtocolType.Byte, typeof(byte?));
        //public static readonly ThriftType NullableDouble = new ThriftType(ThriftProtocolType.Double, typeof(double?));
        //public static readonly ThriftType NullableI16 = new ThriftType(ThriftProtocolType.I16, typeof(short?));
        //public static readonly ThriftType NullableI32 = new ThriftType(ThriftProtocolType.I32, typeof(int?));
        //public static readonly ThriftType NullableI64 = new ThriftType(ThriftProtocolType.I64, typeof(long?));

        public static readonly ThriftType Binary = new ThriftType(ThriftProtocolType.Binary, typeof(byte[]));
        public static readonly ThriftType Void = new ThriftType(ThriftProtocolType.Struct, typeof(void));

        public static readonly ThriftType String = new ThriftType(ThriftProtocolType.String, typeof(String));



        private IThriftTypeReference keyTypeReference;
        private IThriftTypeReference valueTypeReference;
        private ThriftStructMetadata structMetadata;
        private ThriftEnumMetadata enumMetadata;

        public static ThriftType Struct(ThriftStructMetadata structMetadata)
        {
            return new ThriftType(structMetadata);
        }

        public static ThriftType Dictionary(ThriftType keyType, ThriftType valueType)
        {
            Guard.ArgumentNotNull(keyType, nameof(keyType));
            Guard.ArgumentNotNull(valueType, nameof(valueType));

            return Dictionary(new DefaultThriftTypeReference(keyType), new DefaultThriftTypeReference(valueType));
        }

        public static ThriftType Dictionary(IThriftTypeReference keyTypeReference,
                                            IThriftTypeReference valueTypeReference)
        {
            Guard.ArgumentNotNull(keyTypeReference, nameof(keyTypeReference));
            Guard.ArgumentNotNull(valueTypeReference, nameof(valueTypeReference));


            var mapType = typeof(Dictionary<,>).MakeGenericType(keyTypeReference.CSharpType, valueTypeReference.CSharpType);

            return new ThriftType(ThriftProtocolType.Map, mapType, keyTypeReference, valueTypeReference);
        }

        public static ThriftType Set(ThriftType valueType)
        {
            Guard.ArgumentNotNull(valueType, nameof(valueType));

            return Set(new DefaultThriftTypeReference(valueType));
        }

        public static ThriftType Set(IThriftTypeReference valueTypeReference)
        {
            Guard.ArgumentNotNull(valueTypeReference, nameof(valueTypeReference));

            var setType = typeof(HashSet<>).MakeGenericType(valueTypeReference.CSharpType);

            return new ThriftType(ThriftProtocolType.Set, setType, null, valueTypeReference);
        }

        public static ThriftType List(ThriftType valueType)
        {
            Guard.ArgumentNotNull(valueType, nameof(valueType));

            return List(new DefaultThriftTypeReference(valueType));
        }

        public static ThriftType List(IThriftTypeReference valueTypeReference)
        {
            Guard.ArgumentNotNull(valueTypeReference, nameof(valueTypeReference));

            var listType = typeof(List<>).MakeGenericType(valueTypeReference.CSharpType);
            return new ThriftType(ThriftProtocolType.List, listType, null, valueTypeReference);
        }

        public static ThriftType Array(ThriftType valueType)
        {
            Guard.ArgumentNotNull(valueType, nameof(valueType));

            return Array(new DefaultThriftTypeReference(valueType));
        }

        public static ThriftType Array(IThriftTypeReference valueTypeReference)
        {
            Guard.ArgumentNotNull(valueTypeReference, nameof(valueTypeReference));

            return new ThriftType(ThriftProtocolType.List, valueTypeReference.CSharpType.MakeArrayType(),
                null, valueTypeReference);
        }

        public static ThriftType Enum(ThriftEnumMetadata enumMetadata, bool isNullable = false)
        {
            Guard.ArgumentNotNull(enumMetadata, nameof(enumMetadata));
            var tt = new ThriftType(enumMetadata);
            return isNullable ? tt.CoerceTo(typeof(Nullable<>).MakeGenericType(enumMetadata.EnumType)) : tt;
        }

        private ThriftType(ThriftEnumMetadata enumMetadata)
        {
            Guard.ArgumentNotNull(enumMetadata, "enumMetadata");

            this.ProtocolType = ThriftProtocolType.Enum;
            this.CSharpType = enumMetadata.EnumType;
            keyTypeReference = null;
            valueTypeReference = null;
            structMetadata = null;
            this.enumMetadata = enumMetadata;
            this.UncoercedType = null;
        }

        private ThriftType(ThriftProtocolType protocolType, Type csharpType)
        {
            Guard.ArgumentNotNull(csharpType, "csharpType");

            this.ProtocolType = protocolType;
            this.CSharpType = csharpType;
            keyTypeReference = null;
            valueTypeReference = null;
            structMetadata = null;
            this.UncoercedType = null;
        }

        private ThriftType(ThriftProtocolType protocolType,
                           Type javaType,
                           IThriftTypeReference keyTypeReference,
                           IThriftTypeReference valueTypeReference)
        {
            Guard.ArgumentNotNull(javaType, nameof(javaType));
            Guard.ArgumentNotNull(valueTypeReference, nameof(valueTypeReference));


            this.ProtocolType = protocolType;
            this.CSharpType = javaType;
            this.keyTypeReference = keyTypeReference;
            this.valueTypeReference = valueTypeReference;
            this.structMetadata = null;
            this.UncoercedType = null;
        }

        private ThriftType(ThriftStructMetadata structMetadata)
        {
            Guard.ArgumentNotNull(structMetadata, nameof(structMetadata));

            this.ProtocolType = ThriftProtocolType.Struct;
            this.CSharpType = structMetadata.StructType;
            keyTypeReference = null;
            valueTypeReference = null;
            this.structMetadata = structMetadata;
            this.UncoercedType = null;
        }

        private ThriftType(ThriftType underlyingType, Type nullableType)
        {
            this.CSharpType = nullableType;
            this.UncoercedType = underlyingType;

            this.ProtocolType = underlyingType.ProtocolType;
            keyTypeReference = null;
            valueTypeReference = null;
            structMetadata = null;
            enumMetadata = underlyingType.enumMetadata;
        }

        public Type CSharpType { get; }

        public ThriftProtocolType ProtocolType { get; }

        public IThriftTypeReference KeyTypeReference
        {
            get
            {
                if (this.keyTypeReference == null)
                {
                    throw new ArgumentException($"{this.ProtocolType} does not have a key.");
                }
                return this.keyTypeReference;
            }
        }

        public IThriftTypeReference ValueTypeReference
        {
            get
            {
                if (this.valueTypeReference == null)
                {
                    throw new ArgumentException($"{this.ProtocolType} does not have a value.");
                }
                return this.valueTypeReference;
            }
        }

        public ThriftStructMetadata StructMetadata
        {
            get
            {
                if (this.structMetadata == null)
                {
                    throw new ArgumentException($"{this.ProtocolType} does not have struct metadata.");
                }
                return this.structMetadata;
            }
        }

        public ThriftEnumMetadata EnumMetadata
        {
            get
            {
                if (this.enumMetadata == null)
                {
                    throw new ArgumentException($"{this.ProtocolType} does not have enum metadata.");
                }
                return this.enumMetadata;
            }
        }


        public bool IsCoerced
        {
            get { return this.UncoercedType != null; }
        }


        public ThriftType CoerceTo(Type csharpType)
        {
            if (csharpType == this.CSharpType)
            {
                return this;
            }

            if (!(ProtocolType != ThriftProtocolType.Struct &&
                    ProtocolType != ThriftProtocolType.Set &&
                    ProtocolType != ThriftProtocolType.List &&
                    ProtocolType != ThriftProtocolType.Map))
            {
                throw new ArgumentException($"Coercion is not supported for {this.ProtocolType}", nameof(csharpType));
            }
            return new ThriftType(this, csharpType);
        }

        public ThriftType UncoercedType { get; }
        

        public override int GetHashCode()
        {
            int result = ProtocolType.GetHashCode();
            result = 31 * result + (this.CSharpType != null ? CSharpType.GetHashCode() : 0);
            return result;
        }

        public override bool Equals(object o)
        {
            if (this != null && o != null && Object.ReferenceEquals(this,o))
            {
                return true;
            }
            if (o == null || !this.GetType().Equals(o.GetType()))
            {
                return false;
            }

            ThriftType that = (ThriftType)o;

            if (this.CSharpType != null ? !this.CSharpType.Equals(that.CSharpType) : that.CSharpType != null)
            {
                return false;
            }
            if (!this.ProtocolType.Equals(that.ProtocolType))
            {
                return false;
            }

            return true;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftType");
            sb.Append("{");
            sb.Append(this.ProtocolType).Append(" ").Append(this.CSharpType.Name);
            if (structMetadata != null)
            {
                sb.Append(" ").Append(this.StructMetadata.StructType.Name);
            }
            else if (keyTypeReference != null)
            {
                sb.Append(" keyTypeReference=").Append(keyTypeReference);
                sb.Append(", valueTypeReference=").Append(valueTypeReference);
            }
            else if (valueTypeReference != null)
            {
                sb.Append(" valueTypeReference=").Append(valueTypeReference);
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
