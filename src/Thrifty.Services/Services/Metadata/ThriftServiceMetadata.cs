using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty.Services.Metadata
{
    public class ThriftServiceMetadata
    {
        //private readonly IEnumerable<String> documentation;

        public ThriftServiceMetadata(Type serviceClass, ThriftCatalog catalog)
        {
            this.Name = ParseServiceName(serviceClass); 

            var builder = ImmutableDictionary.CreateBuilder<String, ThriftMethodMetadata>();

            foreach (var method in serviceClass.FindAttributedMethods(typeof(ThriftMethodAttribute)))
            {
                ThriftMethodMetadata methodMetadata = new ThriftMethodMetadata(this.Name, method, catalog);
                if (builder.ContainsKey(methodMetadata.QualifiedName))
                {
                    throw new ThriftyException($"duplicate thrift method : {method.DeclaringType.FullName}.{method.Name} .");
                }
                builder[methodMetadata.QualifiedName] = methodMetadata;
            }
            this.Methods = builder.ToImmutable();

            //A multimap from order to method name. Sorted by key (order), with nulls (i.e. no order) last.
            //Within each key, values(ThriftMethodMetadata) are sorted by method name.

            this.DeclaredMethods = builder
                .OrderBy(kp => kp.Value.Order).ThenBy(kp => kp.Key)
                .ToArray()
                .ToImmutableDictionary(kp => kp.Key, kp => kp.Value);

            List<ThriftServiceMetadata> parentServices = new List<ThriftServiceMetadata>();
            foreach (var parent in serviceClass.GetTypeInfo().GetInterfaces())
            {
                var attributes = parent.GetEffectiveClassAnnotations<ThriftServiceAttribute>();
                if (attributes.Any())
                {
                    parentServices.Add(new ThriftServiceMetadata(parent, catalog));
                }
            }
            this.ParentServices = parentServices.ToImmutableList();
        }

        public static string ParseServiceName(Type serviceClass)
        {
            var thriftService = GetThriftServiceAttribute(serviceClass);
            String serviceName = String.IsNullOrWhiteSpace(thriftService.Value.Name) ? thriftService.Key.Name : thriftService.Value.Name.Trim();
            return serviceName;
        }

        public ThriftServiceMetadata(String name, params ThriftMethodMetadata[] methods)
        {
            Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
            this.Name = name;

            Dictionary<String, ThriftMethodMetadata> builder = new Dictionary<String, ThriftMethodMetadata>();
            foreach (ThriftMethodMetadata method in methods)
            {
                if (builder.ContainsKey(method.QualifiedName))
                {
                    throw new ThriftyException($"duplicate thrift method : {method.QualifiedName} .");
                }
                builder.Add(method.QualifiedName, method);
            }
            this.Methods = builder;
            this.DeclaredMethods = this.Methods;
            this.ParentServices = Enumerable.Empty<ThriftServiceMetadata>();
        }

        public static KeyValuePair<Type, ThriftServiceAttribute> GetThriftServiceAttribute(Type serviceClass)
        {
            var typeInfo = serviceClass.GetTypeInfo();
            var attributes = serviceClass.GetEffectiveClassAnnotations<ThriftServiceAttribute>();
            if (!attributes.Any())
            {
                throw new ThriftyException($"Service class {serviceClass.FullName} is not annotated with ThriftServiceAttribute.");
            }
            if (attributes.Count() > 1)
            {
                throw new ThriftyException($"Service class {serviceClass.FullName} has multiple conflicting ThriftServiceAttribute : {attributes.Count()}.");
            }

            return attributes.Single();
        }

        public String Name { get; }
        
        public IDictionary<String, ThriftMethodMetadata> Methods { get; }
        public IDictionary<String, ThriftMethodMetadata> DeclaredMethods { get; }
        public IEnumerable<ThriftServiceMetadata> ParentServices { get; }

        public ThriftMethodMetadata GetMethod(String name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            ThriftMethodMetadata value;

            this.Methods.TryGetValue(name, out value);
            return value;
        }

        public ThriftServiceMetadata GetParentService()
        {
            // Assert that we have 0 or 1 parent.
            // Having multiple @ThriftService parents is generally supported by swift,
            // but this is a restriction that applies to swift2thrift generator (because the Thrift IDL doesn't)
            if (this.ParentServices.Count() > 1)
            {
                throw new ThriftyException("multiple parent service was found.");
            }
            if (!this.ParentServices.Any())
            {
                return null;
            }
            else
            {
                return this.ParentServices.Single();
            }
        }
    }
}
