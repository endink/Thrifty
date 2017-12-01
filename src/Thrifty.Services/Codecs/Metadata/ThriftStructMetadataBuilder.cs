using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static Thrifty.ThriftFieldAttribute;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftStructMetadataBuilder : AbstractThriftMetadataBuilder
    {
        public ThriftStructMetadataBuilder(ThriftCatalog catalog, Type structType) : base(catalog, structType)
        {
            this.VerifyType<ThriftStructAttribute>();
            this.NormalizeThriftFields(catalog);
        }

        public override ThriftStructMetadata Build()
        {
            // this code assumes that metadata is clean
            this.MetadataErrors.ThrowIfHasErrors();

            // builder constructor injection
            ThriftMethodInjection builderMethodInjection = BuildBuilderConstructorInjections();

            // constructor injection (or factory method for builder)
            ThriftConstructorInjection constructorInjections = BuildConstructorInjection();

            // fields injections
            IEnumerable<ThriftFieldMetadata> fieldsMetadata = this.BuildFieldInjections();

            // methods injections
            IEnumerable<ThriftMethodInjection> methodInjections = BuildMethodInjections();

            return new ThriftStructMetadata(
                    this.StructName,
                    this.ExtractStructIdlAnnotations(),
                    this.StructType,
                    this.BuilderType,
                    MetadataType.Struct,
                    fieldsMetadata,
                    methodInjections,
                    builderMethodInjection,
                    //documentation,
                    constructorInjections
            );
        }

        private ThriftConstructorInjection BuildConstructorInjection()
        {
            return this.ConstructorInjections.Select(injection =>
            {
                return new ThriftConstructorInjection(injection.Constructor, BuildParameterInjections(injection.Parameters));
            }).First();
        }

        protected override ThriftFieldMetadata BuildField(IEnumerable<FieldMetadata> input)
        {
            short id = -1;
            //IDictionary<String, String> idlAnnotations = null;
            String name = null;
            Requiredness requiredness = Requiredness.Unspecified;
            bool recursive = false;
            IThriftTypeReference thriftTypeReference = null;

            // process field injections and extractions
            List<IThriftInjection> injections = new List<IThriftInjection>();
            IThriftExtraction extraction = null;
            foreach (FieldMetadata fieldMetadata in input)
            {
                id = fieldMetadata.Id;
                name = fieldMetadata.Name;
                recursive = fieldMetadata.IsRecursiveReference ?? false;
                requiredness = fieldMetadata.Requiredness;
                //idlAnnotations = fieldMetadata.getIdlAnnotations();
                thriftTypeReference = this.Catalog.GetFieldThriftTypeReference(fieldMetadata);

                FieldInjection fieldInjection = fieldMetadata as FieldInjection;
                if (fieldInjection != null)
                {
                    injections.Add(new ThriftFieldInjection(fieldInjection.Id,
                        fieldInjection.Name,
                        fieldInjection.Field,
                        fieldInjection.Type));
                }
                else if (fieldMetadata is ParameterInjection)
                {
                    ParameterInjection parameterInjection = (ParameterInjection)fieldMetadata;
                    injections.Add(new ThriftParameterInjection(
                            parameterInjection.Id,
                            parameterInjection.Name,
                            parameterInjection.ParameterIndex,
                            fieldMetadata.CSharpType
                    ));
                }
                else if (fieldMetadata is FieldExtractor)
                {
                    FieldExtractor fieldExtractor = (FieldExtractor)fieldMetadata;
                    extraction = new ThriftFieldExtractor(
                        fieldExtractor.Id, fieldExtractor.Name,
                        fieldExtractor.Type,
                        fieldExtractor.Field,
                        fieldExtractor.CSharpType);
                }
                else if (fieldMetadata is MethodExtractor)
                {
                    MethodExtractor methodExtractor = (MethodExtractor)fieldMetadata;
                    extraction = new ThriftMethodExtractor(
                        methodExtractor.Id,
                        methodExtractor.Name,
                        methodExtractor.Type,
                        methodExtractor.Method,
                        methodExtractor.CSharpType);
                }
            }

            // add type coercion
            TypeCoercion coercion = null;
            if (!thriftTypeReference.Recursive && thriftTypeReference.Get().IsCoerced)
            {
                coercion = this.Catalog.GetDefaultCoercion(thriftTypeReference.Get().CSharpType);
            }

            if (recursive && requiredness != Requiredness.Optional)
            {
                this.MetadataErrors.AddError($"Struct '{this.StructName}' field '{name}' is recursive but not marked optional");
            }

            ThriftFieldMetadata thriftFieldMetadata = new ThriftFieldMetadata(
                    id,
                    recursive,
                    requiredness,
                    //idlAnnotations,
                    thriftTypeReference,
                    name,
                    FieldKind.ThriftField,
                    injections,
                    extraction:extraction,
                    coercion:coercion
            );
            return thriftFieldMetadata;
        }

        protected override Type ExtractBuilderClass()
        {
            //暂时不支持自定义 Builder
            //var annotation = this.StructType.GetTypeInfo().GetCustomAttribute<ThriftStructAttribute>();
            //if (annotation != null && !annotation.Builder.GetType().equals(void.class)) {
            //    return annotation.builder();
            //}
            //else {
            //    return null;
            //}
            return null;
        }

        protected override string ExtractName()
        {
            var annotation = this.StructType.GetTypeInfo().GetCustomAttribute<ThriftStructAttribute>();
            return String.IsNullOrWhiteSpace(annotation?.Name) ? this.StructType.Name : annotation.Name;
        }
    }
}
