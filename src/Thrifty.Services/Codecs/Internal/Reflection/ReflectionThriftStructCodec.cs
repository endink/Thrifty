using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.Codecs.Metadata;
using Thrift.Protocol;
using System.Reflection;
using System.Linq.Expressions;

namespace Thrifty.Codecs.Internal.Reflection
{
    public class ReflectionThriftStructCodec<T> : AbstractReflectionThriftCodec<T>
    {
        public ReflectionThriftStructCodec(ThriftCodecManager manager, ThriftStructMetadata metadata) : base(manager, metadata)
        {
        }

        public override T Read(TProtocol protocol)
        {
            TProtocolReader reader = new TProtocolReader(protocol);
            reader.ReadStructBegin();

            IDictionary<short, Object> data = new Dictionary<short, Object>(_metadata.Fields.Count());
            while (reader.NextField())
            {
                short fieldId = reader.GetFieldId();

                // do we have a codec for this field
                IThriftCodec codec;
                if (!_fields.TryGetValue(fieldId, out codec))
                {
                    reader.SkipFieldData();
                    continue;
                }

                // is this field readable
                ThriftFieldMetadata field = _metadata.GetField(fieldId);
                if (field.ReadOnly || field.Type != FieldKind.ThriftField)
                {
                    reader.SkipFieldData();
                    continue;
                }

                // read the value
                Object value = reader.ReadField(codec);
                if (value == null)
                {
                    if (field.Required == ThriftFieldAttribute.Requiredness.Required)
                    {
                        throw new TProtocolException($"'{field.Name}（id: {fieldId}）' is a required field, but it was not set, thrift type: '{field.ThriftType.ToString()}', codec: '{codec.GetType().Name}'");
                    }
                    else
                    {
                        continue;
                    }
                }
                data[fieldId] = value;
            }
            reader.ReadStructEnd();

            // build the struct
            return ConstructStruct(data);
        }

        private T ConstructStruct(IDictionary<short, Object> data)
        {
            // construct instance
            Object instance;
            {
                ThriftConstructorInjection constructor;
                 if (!_metadata.TryGetThriftConstructorInjection(out constructor))
                {
                    throw new ThriftyException($"ReflectionThriftStructCodec need a constructor injection, for type {_metadata.StructType.FullName}");
                }
                Object[] parametersValues = new Object[constructor.Parameters.Count()];
                foreach (ThriftParameterInjection parameter in constructor.Parameters)
                {
                    Object value = null;
                    if (data.TryGetValue(parameter.Id, out value))
                    {
                        parametersValues[parameter.ParameterIndex] = value;
                    }
                    else
                    {
                        parametersValues[parameter.ParameterIndex] = ThriftyUtilities.GetDefaultValue(parameter.CSharpType);
                    }
                }

                try
                {
                    instance = constructor.Constructor.Invoke(parametersValues);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            // inject fields
            foreach (ThriftFieldMetadata fieldMetadata in _metadata.GetFields(FieldKind.ThriftField))
            {
                foreach (var fieldInjection in fieldMetadata.Injections.OfType<ThriftFieldInjection>())
                {

                    Object value;
                    if (data.TryGetValue(fieldInjection.Id, out value))
                    {
                        fieldInjection.Field.SetValue(instance, value);
                    }

                }
            }

            // inject methods
            foreach (ThriftMethodInjection methodInjection in _metadata.MethodInjections)
            {
                bool shouldInvoke = false;
                Object[] parametersValues = new Object[methodInjection.Parameters.Count()];
                foreach (ThriftParameterInjection parameter in methodInjection.Parameters)
                {
                    Object value;
                    if (data.TryGetValue(parameter.Id, out value))
                    {
                        parametersValues[parameter.ParameterIndex] = value;
                        shouldInvoke = true;
                    }
                }

                if (shouldInvoke)
                {
                    methodInjection.Method.Invoke(instance, parametersValues);

                }
            }

            ThriftMethodInjection builderMethod;
            // builder method
            if (_metadata.TryGetBuilderMethod(out builderMethod))
            {
                Object[] parametersValues = new Object[builderMethod.Parameters.Count()];
                foreach (ThriftParameterInjection parameter in builderMethod.Parameters)
                {
                    Object value = data[parameter.Id];
                    parametersValues[parameter.ParameterIndex] = value;
                }

                instance = builderMethod.Method.Invoke(instance, parametersValues);
                if (instance == null)
                {
                    throw new ThriftyException("Builder method returned a null instance");

                }
                if (!_metadata.StructType.GetTypeInfo().IsInstanceOfType(instance))
                {
                    throw new ThriftyException(
                        $"Builder method returned instance of type {instance.GetType().FullName}, but an instance of {_metadata.StructType.FullName} is required.");
                }
            }

            return (T)instance;
        }

        public override void Write(T value, TProtocol protocol)
        {
            TProtocolWriter writer = new TProtocolWriter(protocol);
            writer.WriteStructBegin(_metadata.StructName);
            foreach (ThriftFieldMetadata fieldMetadata in _metadata.GetFields(FieldKind.ThriftField))
            {
                // is the field readable?
                if (fieldMetadata.WriteOnly)
                {
                    continue;
                }

                // get the field value
                Object fieldValue = this.GetFieldValue(value, fieldMetadata);

                // write the field
                if (fieldValue != null)
                {
                    IThriftCodec codec;
                    if (!_fields.TryGetValue(fieldMetadata.Id, out codec))
                    {
                        throw new ThriftyException("IThriftCodec was not found.");
                    }
                    writer.WriteField(fieldMetadata.Name, fieldMetadata.Id, codec, fieldValue);
                }
            }
            writer.WriteStructEnd();
        }
    }
}
