using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Thrift;
using static Thrifty.ThriftFieldAttribute;

namespace Thrifty.Services.Metadata
{
    public class ThriftMethodMetadata
    {
        internal ThriftMethodMetadata(String serviceName, MethodInfo method, ThriftCatalog catalog)
        {
            Guard.ArgumentNullOrWhiteSpaceString(serviceName, nameof(serviceName));
            Guard.ArgumentNotNull(method, nameof(method));
            Guard.ArgumentNotNull(catalog, nameof(catalog));

            this.Order = (ThriftCatalog.GetMethodOrder(method) ?? int.MaxValue);

            ThriftMethodAttribute thriftMethod = method.GetCustomAttribute<ThriftMethodAttribute>();
            if (thriftMethod == null)
            {
                throw new ArgumentException($"Method '{method.DeclaringType.Name}.{method.Name}' is not annotated with {nameof(ThriftMethodAttribute)}.", nameof(method));
            }
            if (method.IsStatic)
            {
                throw new ArgumentException($"Method '{method.DeclaringType.Name}.{method.Name} is a static method.", nameof(method));
            }

            this.Name = String.IsNullOrWhiteSpace(thriftMethod.Name) ? method.Name : thriftMethod.Name.Trim();

            this.QualifiedName = GetQualifiedName(serviceName, Name);

            //this.QualifiedName = $"{serviceName}.{this.Name}";
            this.ReturnType = catalog.GetThriftType(method.ReturnType);

            var builder = ImmutableList.CreateBuilder<ThriftFieldMetadata>();
            var parameters = method.GetParameters();
            int index = 0;
            foreach (var p in parameters)
            {
                ThriftFieldMetadata fieldMetadata = CreateFieldMetadata(catalog, index, p);
                builder.Add(fieldMetadata);
                index++;
            }
            this.Parameters = builder.ToImmutableList();
            this.IsOneWay = thriftMethod.OneWay;
            this.Method = method;
            this.Exceptions = BuildExceptions(catalog, method);
        }

        public static string GetQualifiedName(string serviceName, String methodName)
        {
            return $"{serviceName}:{methodName}";
        }

        private IDictionary<short, ThriftType> BuildExceptions(ThriftCatalog catalog, MethodInfo method)
        {
            var exceptions = ImmutableDictionary.CreateBuilder<short, ThriftType>();
            HashSet<Type> exceptionTypes = new HashSet<Type>();

            var exceptionAttributes = method.GetCustomAttributes<ThriftExceptionAttribute>(true);

            foreach (var thriftException in exceptionAttributes)
            {
                if (!exceptionTypes.Add(thriftException.ExceptionType))
                {
                    throw new ThriftyException($"ThriftExceptionAttribute on method {method.DeclaringType}.{method.Name} contains more than one value for {thriftException.ExceptionType} .");
                }
                if (exceptions.ContainsKey(thriftException.Id))
                {
                    throw new ThriftyException($"ThriftExceptionAttribute on method {method.DeclaringType}.{method.Name} has duplicate id: {thriftException.Id} .");
                }
                exceptions.Add(thriftException.Id, catalog.GetThriftType(thriftException.ExceptionType));
            }

            foreach (var exceptionType in exceptionAttributes.Select(a => a.ExceptionType))
            {
                if (exceptionType.GetTypeInfo().IsAssignableFrom(typeof(TException)))
                {
                    // the built-in exception types don't need special treatment
                    continue;
                }
                ThriftStructAttribute attribute = exceptionType.GetTypeInfo().GetCustomAttribute<ThriftStructAttribute>();
                if (attribute == null)
                {
                    throw new ThriftyException($"ThriftExceptionAttribute on method {method.DeclaringType}.{method.Name} with {exceptionType.FullName} need {nameof(ThriftStructAttribute)} .");
                }
            }

            return exceptions.ToImmutableDictionary();
        }

        private static ThriftFieldMetadata CreateFieldMetadata(ThriftCatalog catalog, int index, ParameterInfo parameterInfo)
        {
            ThriftFieldAttribute thriftField = parameterInfo.GetCustomAttribute<ThriftFieldAttribute>();
            short parameterId = short.MinValue;
            String parameterName = parameterInfo.Name;
            Requiredness parameterRequiredness = Requiredness.Unspecified;
            if (thriftField != null)
            {
                parameterId = thriftField.Id;
                parameterRequiredness = thriftField.Required;
                if (!String.IsNullOrWhiteSpace(thriftField.Name))
                {
                    parameterName = thriftField.Name.Trim();
                }
            }
            if (parameterId == short.MinValue)
            {
                parameterId = (short)(index + 1);
            }
            ThriftType thriftType = catalog.GetThriftType(parameterInfo.ParameterType);
            var parameterInjection = new ThriftParameterInjection(parameterId, parameterName, index, parameterInfo.ParameterType);
            if (parameterRequiredness == Requiredness.Unspecified)
            {
                // There is only one field injection used to build metadata for method parameters, and if a
                // single injection point has UNSPECIFIED requiredness, that resolves to NONE.
                parameterRequiredness = Requiredness.None;
            }
            ThriftFieldMetadata fieldMetadata = new ThriftFieldMetadata(
                parameterId,
                false /* recursiveness */,
                parameterRequiredness,
                new DefaultThriftTypeReference(thriftType),
                parameterName,
                FieldKind.ThriftField,
                new IThriftInjection[] { parameterInjection });
            return fieldMetadata;
        }

        public IDictionary<short, ThriftType> Exceptions { get; }

        public int Order { get; }

        public String Name { get; }

        public String QualifiedName { get; }

        public MethodInfo Method { get; }

        public ThriftType ReturnType { get; }

        public bool IsOneWay { get; }

        public IEnumerable<ThriftFieldMetadata> Parameters { get; }

        public bool IsAsync
        {
            get
            {
                Type returnType = this.Method.ReturnType;
                return typeof(Task).GetTypeInfo().IsAssignableFrom(returnType);
            }
        }

        public override int GetHashCode()
        {
            return ThriftyUtilities.Hash(this.QualifiedName, this.ReturnType, this.Parameters, this.Method, this.Exceptions, this.IsOneWay);
        }

        public override bool Equals(object o)
        {
            if (o == null || !this.GetType().Equals(o.GetType()))
            {
                return false;
            }

            if (Object.ReferenceEquals(this,o))
            {
                return true;
            }

            ThriftMethodMetadata that = (ThriftMethodMetadata)o;

            return this.QualifiedName.Equals(that.QualifiedName) && this.ReturnType.Equals(that.ReturnType) &&
                    Enumerable.SequenceEqual(this.Parameters, that.Parameters) && this.Method.Equals(that.Method) &&
                    Enumerable.Equals(this.Exceptions, that.Exceptions) && this.IsOneWay.Equals(that.IsOneWay);
        }
    }
}
