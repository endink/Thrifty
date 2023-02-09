using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs.Internal
{
    public class EnumThriftCodec<T> : IThriftCodec<T>
        where T : struct
    {
        private ThriftEnumMetadata _metadata;

        public EnumThriftCodec(ThriftType type)
        {
            this.Type = type;
            _metadata = type.EnumMetadata;
        }

        public ThriftType Type { get; }

        public T Read(TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));

            int enumValue = protocol.ReadI32();
            //var definedValue = TypeUnderlyingType(enumValue);
            if (enumValue >= 0)
            {
                if (_metadata.HasExplicitThriftValue)
                {
                    T enumConstant = (T)Enum.ToObject(_metadata.EnumType, enumValue);
                    return enumConstant;
                }
                else
                {
                    return (T)Enum.ToObject(_metadata.EnumType, enumValue);
                }
            }
            // unknown, throw unknown value exception
            throw new ThriftyException($"Enum {_metadata.EnumType.FullName} does not have a value for {enumValue}.");
        }

        public object ReadObject(TProtocol protocol)
        {
            return this.Read(protocol);
        }

        public void Write(T enumConstant, TProtocol protocol)
        {
            Guard.ArgumentNotNull(protocol, nameof(protocol));

            int enumValue;
            if (_metadata.HasExplicitThriftValue)
            {
                enumValue = _metadata.CastToNumber(enumConstant);
            }
            else
            {
                enumValue = Convert.ToInt32(enumConstant);
            }
            if (enumValue < 0)
            {
                //see https://thrift.apache.org/docs/idl
                //An enum creates an enumerated type, with named values. If no constant value is supplied, the value is either 0 for the first element, or one greater than the preceding value for any subsequent element. Any constant value that is supplied must be non-negative.
                throw new ThriftyException($"Enum {_metadata.EnumType.FullName} cannot be negative {enumValue} for {Enum.GetName(typeof(T), enumConstant)}, it must be non-negative.");
            }
            protocol.WriteI32(enumValue);
        }

        public void WriteObject(object value, TProtocol protocol)
        {
            this.Write((T)value, protocol);
        }
    }
}
