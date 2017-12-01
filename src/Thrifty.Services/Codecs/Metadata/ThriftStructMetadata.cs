using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;

namespace Thrifty.Codecs.Metadata
{

    public enum MetadataType
    {
        Struct, Union
    }

    public class ThriftStructMetadata
    {
        private IDictionary<String, String> _idlAnnotations;
        private ThriftMethodInjection _builderMethod;
        //private IReadOnlyList<String> documentation;

        private SortedDictionary<short, ThriftFieldMetadata> _fields;

        private ThriftConstructorInjection _constructorInjection;

        private IEnumerable<ThriftMethodInjection> _methodInjections;

        public ThriftStructMetadata(
                String structName,
                IDictionary<String, String> idlAnnotations,
                Type structType,
                Type builderType,
                MetadataType metadataType,
                //List<String> documentation,
                IEnumerable<ThriftFieldMetadata> fields = null,
                IEnumerable<ThriftMethodInjection> methodInjections = null,
                ThriftMethodInjection builderMethod = null,
                ThriftConstructorInjection constructorInjection = null)
        {
            //Guard.ArgumentNotNull(builderType, nameof(builderType));
            Guard.ArgumentNullOrWhiteSpaceString(structName, nameof(structName));
            Guard.ArgumentNotNull(idlAnnotations, nameof(idlAnnotations));
            Guard.ArgumentNotNull(metadataType, nameof(metadataType));
            Guard.ArgumentNotNull(structType, nameof(structType));
            //Guard.ArgumentNotNull(documentation, nameof(documentation));
            Guard.ArgumentNotNull(fields, nameof(fields));

            this.BuilderType = builderType;
            this.StructName = structName;
            this._idlAnnotations = idlAnnotations;
            this.MetadataType = metadataType;
            this.StructType = structType;

            this._builderMethod = builderMethod;
            this._methodInjections = methodInjections ?? Enumerable.Empty<ThriftMethodInjection>();
            //this.documentation = documentation.AsReadOnly();
            var dic = (fields ?? Enumerable.Empty<ThriftFieldMetadata>()).ToDictionary(m => m.Id);
            this._fields = new SortedDictionary<short, ThriftFieldMetadata>(dic);
            //        this.fields = ImmutableSortedMap.copyOf(uniqueIndex(checkNotNull(fields, "fields is null"), new Function<ThriftFieldMetadata, Short>()
            //    {
            //        @Override
            //        public Short apply(ThriftFieldMetadata input)
            //    {
            //        return input.getId();
            //    }
            //}));
            //this.methodInjections = ImmutableList.copyOf(checkNotNull(methodInjections, "methodInjections is null"));

            this._constructorInjection = constructorInjection;
            this._methodInjections = methodInjections ?? Enumerable.Empty<ThriftMethodInjection>();
        }

        public String StructName { get; }

        public Type StructType { get; }

        public Type BuilderType { get; }

        public MetadataType MetadataType { get; }

        public IEnumerable<KeyValuePair<String, String>> IdlAnnotations { get; }

        public ThriftFieldMetadata GetField(short id)
        {
            ThriftFieldMetadata meta = null;
            _fields.TryGetValue((short)id, out meta);
            return meta;
        }

        public bool TryGetBuilderMethod(out ThriftMethodInjection builderMethod)
        {
            builderMethod = this._builderMethod;
            return builderMethod != null;
        }

        public bool TryGetThriftConstructorInjection(out ThriftConstructorInjection injection)
        {
            injection = this._constructorInjection;
            return injection != null;
        }

        //public ImmutableList<String> getDocumentation()
        //{
        //    return documentation;
        //}

        public IEnumerable<ThriftFieldMetadata> GetFields(FieldKind type)
        {
            return this.Fields.Where(f => f.Type == type);
        }

        public IEnumerable<ThriftFieldMetadata> Fields
        {
            get { return _fields.Values; }
        }

        public IEnumerable<ThriftMethodInjection> MethodInjections
        {
            get { return _methodInjections; }
        }

        public bool IsException
        {

            get { return typeof(Exception).GetTypeInfo().IsAssignableFrom(this.StructType); }
        }

        public bool IsUnion
        {
            get { return false; }
        }

        public bool IsStruct
        {
            get { return !this.IsException && this.MetadataType == MetadataType.Struct; }
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ThriftStructMetadata");
            sb.Append("{structName='").Append(this.StructName).Append('\'');
            sb.Append(", structType=").Append(this.StructType);
            sb.Append(", builderType=").Append(this.BuilderType);
            sb.Append(", builderMethod=").Append(_builderMethod);
            sb.Append(", fields=").Append(_fields);
            if (_constructorInjection != null)
            {
                sb.Append(", constructorInjection=").Append(_constructorInjection);
            }
            if (_methodInjections.Any())
            {
                sb.Append(", methodInjections=").Append(String.Join(", ", _methodInjections));
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
